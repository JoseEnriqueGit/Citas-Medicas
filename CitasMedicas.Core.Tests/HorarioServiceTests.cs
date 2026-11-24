using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class HorarioServiceTests : IDisposable
{
    private static readonly DateTime Martes = new(2026, 11, 24, 10, 15, 0);

    private readonly CitasRepository _repo = new(":memory:");
    private readonly FixedTimeProvider _clock = new(Martes);
    private readonly HorarioService _servicio;
    private readonly Paciente _paciente;
    private readonly Medico _medico;
    private readonly Medico _otro;

    public HorarioServiceTests()
    {
        _servicio = new HorarioService(_repo, _clock);
        _paciente = _repo.InsertarPaciente(new Paciente
        {
            NombreCompleto = "María Fernández Peña",
            Cedula = "001-1234567-8",
            Telefono = "809-555-0142",
            FechaNacimiento = new DateTime(1991, 3, 14)
        });
        var medicos = _repo.ListarMedicos();
        _medico = medicos[0];
        _otro = medicos[1];
    }

    public void Dispose() => _repo.Dispose();

    private void Reservar(Medico medico, DateTime inicio, EstadoCita estado = EstadoCita.Programada) =>
        _repo.InsertarCita(new Cita
        {
            PacienteId = _paciente.Id,
            MedicoId = medico.Id,
            Inicio = inicio,
            Motivo = "Consulta",
            Estado = estado,
            CreadaEn = Martes
        });

    private void LlenarDia(Medico medico, DateTime dia)
    {
        for (var inicio = dia.Date.AddHours(8); inicio < dia.Date.AddHours(16); inicio = inicio.AddMinutes(30))
        {
            Reservar(medico, inicio);
        }
    }

    [Fact]
    public void FutureWeekdayHasSixteenBlocksFrom0800To1530()
    {
        var bloques = _servicio.Bloques(_medico.Id, new DateTime(2026, 11, 25));

        Assert.Equal(16, bloques.Count);
        Assert.Equal(new DateTime(2026, 11, 25, 8, 0, 0), bloques[0].Inicio);
        Assert.Equal(new DateTime(2026, 11, 25, 15, 30, 0), bloques[^1].Inicio);
        Assert.All(bloques, b => Assert.Equal(TimeSpan.FromMinutes(30), b.Fin - b.Inicio));
        Assert.All(bloques, b => Assert.Equal(EstadoBloque.Libre, b.Estado));
    }

    [Theory]
    [InlineData(2026, 11, 28)]
    [InlineData(2026, 11, 29)]
    public void WeekendsHaveNoBlocks(int anio, int mes, int dia)
    {
        var fecha = new DateTime(anio, mes, dia);

        Assert.False(_servicio.EsDiaHabil(fecha));
        Assert.Empty(_servicio.Bloques(_medico.Id, fecha));
    }

    [Fact]
    public void PastDatesHaveNoBlocks()
    {
        Assert.Empty(_servicio.Bloques(_medico.Id, new DateTime(2026, 11, 23)));
    }

    [Fact]
    public void TimeOfDayInTheRequestedDateIsIgnored()
    {
        var bloques = _servicio.Bloques(_medico.Id, new DateTime(2026, 11, 25, 14, 40, 0));

        Assert.Equal(16, bloques.Count);
        Assert.Equal(new DateTime(2026, 11, 25, 8, 0, 0), bloques[0].Inicio);
    }

    [Fact]
    public void ScheduledAppointmentsMarkOnlyThatDoctorsBlocksAsBusy()
    {
        Reservar(_medico, new DateTime(2026, 11, 25, 9, 0, 0));
        Reservar(_otro, new DateTime(2026, 11, 25, 10, 0, 0));

        var bloques = _servicio.Bloques(_medico.Id, new DateTime(2026, 11, 25));

        Assert.Equal(EstadoBloque.Ocupado, bloques.Single(b => b.Inicio.Hour == 9 && b.Inicio.Minute == 0).Estado);
        Assert.Equal(1, bloques.Count(b => b.Estado == EstadoBloque.Ocupado));
    }

    [Fact]
    public void CancelledAndCompletedAppointmentsDoNotOccupyBlocks()
    {
        Reservar(_medico, new DateTime(2026, 11, 25, 9, 0, 0), EstadoCita.Cancelada);
        Reservar(_medico, new DateTime(2026, 11, 25, 9, 30, 0), EstadoCita.Completada);

        var bloques = _servicio.Bloques(_medico.Id, new DateTime(2026, 11, 25));

        Assert.All(bloques, b => Assert.Equal(EstadoBloque.Libre, b.Estado));
    }

    [Fact]
    public void BlocksAlreadyStartedTodayAreMarkedAsPast()
    {
        var bloques = _servicio.Bloques(_medico.Id, Martes.Date);

        Assert.Equal(16, bloques.Count);
        Assert.All(bloques.Where(b => b.Inicio <= Martes), b => Assert.Equal(EstadoBloque.Pasado, b.Estado));
        Assert.All(bloques.Where(b => b.Inicio > Martes), b => Assert.Equal(EstadoBloque.Libre, b.Estado));
        Assert.Equal(new DateTime(2026, 11, 24, 10, 30, 0), bloques.First(b => b.Estado == EstadoBloque.Libre).Inicio);
    }

    [Fact]
    public void FullDayHasNoFreeSlot()
    {
        var viernes = new DateTime(2026, 11, 27);
        LlenarDia(_medico, viernes);

        Assert.False(_servicio.TieneHueco(_medico.Id, viernes));
        Assert.True(_servicio.TieneHueco(_medico.Id, viernes.AddDays(-1)));
    }

    [Fact]
    public void NextDayWithAvailabilitySkipsFullDaysAndWeekends()
    {
        var viernes = new DateTime(2026, 11, 27);
        LlenarDia(_medico, viernes);

        var siguiente = _servicio.ProximoDiaConHueco(_medico.Id, viernes);

        Assert.Equal(new DateTime(2026, 11, 30), siguiente);
    }

    [Fact]
    public void NextDayWithAvailabilityAfterTheGivenDateNeverReturnsThatDate()
    {
        var siguiente = _servicio.ProximoDiaConHueco(_medico.Id, new DateTime(2026, 11, 25));

        Assert.Equal(new DateTime(2026, 11, 26), siguiente);
    }

    [Fact]
    public void NextFreeSlotIsTheFirstBlockAfterNowToday()
    {
        Assert.Equal(new DateTime(2026, 11, 24, 10, 30, 0), _servicio.ProximoHorarioLibre(_medico.Id));
    }

    [Fact]
    public void NextFreeSlotSkipsBusyBlocks()
    {
        Reservar(_medico, new DateTime(2026, 11, 24, 10, 30, 0));

        Assert.Equal(new DateTime(2026, 11, 24, 11, 0, 0), _servicio.ProximoHorarioLibre(_medico.Id));
    }

    [Fact]
    public void NextFreeSlotMovesToTomorrowWhenTodayIsOver()
    {
        _clock.Set(new DateTime(2026, 11, 24, 15, 45, 0));

        Assert.Equal(new DateTime(2026, 11, 25, 8, 0, 0), _servicio.ProximoHorarioLibre(_medico.Id));
    }

    [Fact]
    public void NextFreeSlotOnFridayEveningIsMondayMorning()
    {
        _clock.Set(new DateTime(2026, 11, 27, 16, 30, 0));

        Assert.Equal(new DateTime(2026, 11, 30, 8, 0, 0), _servicio.ProximoHorarioLibre(_medico.Id));
    }

    [Fact]
    public void NextFreeSlotIsNullWhenNothingIsFreeInTheHorizon()
    {
        for (var dia = Martes.Date; dia <= Martes.Date.AddDays(HorarioService.DiasDeBusqueda); dia = dia.AddDays(1))
        {
            if (_servicio.EsDiaHabil(dia))
            {
                LlenarDia(_medico, dia);
            }
        }

        Assert.Null(_servicio.ProximoHorarioLibre(_medico.Id));
    }
}
