using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class HorarioServiceRobustnessTests : IDisposable
{
    private static readonly DateTime Martes = new(2026, 11, 24, 10, 15, 0);

    private readonly CitasRepository _repo = new(":memory:");
    private readonly FixedTimeProvider _clock = new(Martes);
    private readonly HorarioService _servicio;
    private readonly Medico _medico;
    private readonly Paciente _paciente;

    public HorarioServiceRobustnessTests()
    {
        _servicio = new HorarioService(_repo, _clock);
        _medico = _repo.ListarMedicos()[0];
        _paciente = _repo.InsertarPaciente(new Paciente
        {
            NombreCompleto = "María Fernández Peña",
            Cedula = "001-1234567-8",
            Telefono = "809-555-0142",
            FechaNacimiento = new DateTime(1991, 3, 14)
        });
    }

    public void Dispose() => _repo.Dispose();

    private void Reservar(DateTime inicio) =>
        _repo.InsertarCita(new Cita
        {
            PacienteId = _paciente.Id,
            MedicoId = _medico.Id,
            Inicio = inicio,
            Motivo = "Consulta",
            Estado = EstadoCita.Programada,
            CreadaEn = Martes
        });

    [Fact]
    public void UnknownDoctorHasNoBlocksNoAvailabilityAndNoNextSlot()
    {
        Assert.Empty(_servicio.Bloques(9999, new DateTime(2026, 11, 25)));
        Assert.False(_servicio.TieneHueco(9999, new DateTime(2026, 11, 25)));
        Assert.Null(_servicio.ProximoDiaConHueco(9999, Martes));
        Assert.Null(_servicio.ProximoHorarioLibre(9999));
    }

    [Fact]
    public void BlockThatStartsExactlyNowIsPastAndTheNextOneIsFree()
    {
        _clock.Set(new DateTime(2026, 11, 24, 10, 30, 0));

        var bloques = _servicio.Bloques(_medico.Id, new DateTime(2026, 11, 24));

        Assert.Equal(EstadoBloque.Pasado, bloques.Single(b => b.Inicio == new DateTime(2026, 11, 24, 10, 30, 0)).Estado);
        Assert.Equal(EstadoBloque.Libre, bloques.Single(b => b.Inicio == new DateTime(2026, 11, 24, 11, 0, 0)).Estado);
    }

    [Fact]
    public void TodayAfterClosingHasOnlyPastBlocksAndNoAvailability()
    {
        _clock.Set(new DateTime(2026, 11, 24, 23, 59, 59));

        var bloques = _servicio.Bloques(_medico.Id, new DateTime(2026, 11, 24));

        Assert.Equal(16, bloques.Count);
        Assert.All(bloques, b => Assert.Equal(EstadoBloque.Pasado, b.Estado));
        Assert.False(_servicio.TieneHueco(_medico.Id, new DateTime(2026, 11, 24)));
    }

    [Fact]
    public void NextFreeSlotOnASaturdayNoonIsMondayMorning()
    {
        _clock.Set(new DateTime(2026, 11, 28, 12, 0, 0));

        Assert.Equal(new DateTime(2026, 11, 30, 8, 0, 0), _servicio.ProximoHorarioLibre(_medico.Id));
    }

    [Fact]
    public void NextDayWithAvailabilityFromASaturdayIsMonday()
    {
        Assert.Equal(new DateTime(2026, 11, 30), _servicio.ProximoDiaConHueco(_medico.Id, new DateTime(2026, 11, 28)));
    }

    [Fact]
    public void LastBlockOfTheDayCanBeBusyWithoutAffectingTheOthers()
    {
        var dia = new DateTime(2026, 11, 25);
        Reservar(dia.AddHours(15).AddMinutes(30));

        var bloques = _servicio.Bloques(_medico.Id, dia);

        Assert.Equal(EstadoBloque.Ocupado, bloques[^1].Estado);
        Assert.Equal(15, bloques.Count(b => b.Libre));
    }

    [Fact]
    public void AppointmentsBookedOnOtherDaysDoNotLeakIntoTheRequestedDay()
    {
        Reservar(new DateTime(2026, 11, 26, 8, 0, 0));

        Assert.All(_servicio.Bloques(_medico.Id, new DateTime(2026, 11, 25)), b => Assert.Equal(EstadoBloque.Libre, b.Estado));
    }
}
