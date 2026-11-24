using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class CitaServiceTests : IDisposable
{
    private static readonly DateTime Martes = new(2026, 11, 24, 10, 15, 0);

    private readonly CitasRepository _repo = new(":memory:");
    private readonly FixedTimeProvider _clock = new(Martes);
    private readonly CitaService _servicio;
    private readonly Paciente _paciente;
    private readonly Medico _medico;
    private readonly Medico _otro;

    public CitaServiceTests()
    {
        _servicio = new CitaService(_repo, _clock);
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

    private SolicitudCita Solicitud(DateTime? inicio = null, string motivo = "Control general") =>
        new(_medico.Id, inicio ?? new DateTime(2026, 11, 25, 8, 0, 0), motivo);

    [Fact]
    public void ValidRequestCreatesAScheduledAppointment()
    {
        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud());

        Assert.True(resultado.Exitoso);
        var cita = resultado.Cita!;
        Assert.Equal(EstadoCita.Programada, cita.Estado);
        Assert.Equal(_paciente.Id, cita.PacienteId);
        Assert.Equal(_medico.Id, cita.MedicoId);
        Assert.Equal(new DateTime(2026, 11, 25, 8, 0, 0), cita.Inicio);
        Assert.Equal("Control general", cita.Motivo);
        Assert.Equal(Martes, cita.CreadaEn);
        Assert.Single(_repo.CitasDePaciente(_paciente.Id));
    }

    [Fact]
    public void RequestWithoutActivePatientIsRejected()
    {
        var resultado = _servicio.Solicitar(null, Solicitud());

        Assert.False(resultado.Exitoso);
        Assert.Equal("Regístrate para solicitar una cita.", resultado.Errores[CampoSolicitud.Paciente]);
        Assert.Empty(_repo.CitasDePaciente(_paciente.Id));
    }

    [Fact]
    public void EmptyRequestReportsDoctorSlotAndReasonTogether()
    {
        var resultado = _servicio.Solicitar(_paciente.Id, new SolicitudCita(null, null, null));

        Assert.Equal(
            new[] { CampoSolicitud.Medico, CampoSolicitud.Horario, CampoSolicitud.Motivo },
            resultado.Errores.Keys.OrderBy(k => k));
        Assert.Equal("Elige un médico.", resultado.Errores[CampoSolicitud.Medico]);
        Assert.Equal("Elige un horario.", resultado.Errores[CampoSolicitud.Horario]);
        Assert.Equal("Escribe el motivo de la consulta.", resultado.Errores[CampoSolicitud.Motivo]);
        Assert.Empty(_repo.CitasDePaciente(_paciente.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void BlankReasonIsRejected(string motivo)
    {
        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud(motivo: motivo));

        Assert.Equal("Escribe el motivo de la consulta.", resultado.Errores[CampoSolicitud.Motivo]);
        Assert.Empty(_repo.CitasDePaciente(_paciente.Id));
    }

    [Fact]
    public void ReasonOf200CharactersIsAcceptedAnd201IsRejected()
    {
        Assert.True(_servicio.Solicitar(_paciente.Id, Solicitud(motivo: new string('a', 200))).Exitoso);

        var resultado = _servicio.Solicitar(
            _paciente.Id, Solicitud(new DateTime(2026, 11, 25, 8, 30, 0), new string('a', 201)));

        Assert.Equal("El motivo no puede pasar de 200 caracteres.", resultado.Errores[CampoSolicitud.Motivo]);
        Assert.Single(_repo.CitasDePaciente(_paciente.Id));
    }

    [Fact]
    public void ReasonIsTrimmed()
    {
        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud(motivo: "  Dolor de cabeza  "));

        Assert.Equal("Dolor de cabeza", resultado.Cita!.Motivo);
    }

    [Fact]
    public void BookingInThePastIsRejected()
    {
        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud(new DateTime(2026, 11, 24, 10, 0, 0)));

        Assert.Equal("No puedes reservar un horario que ya pasó.", resultado.Errores[CampoSolicitud.Horario]);
        Assert.False(resultado.HorarioTomado);
        Assert.Empty(_repo.CitasDePaciente(_paciente.Id));
    }

    [Theory]
    [InlineData(2026, 11, 28, 9, 0)]
    [InlineData(2026, 11, 25, 7, 30)]
    [InlineData(2026, 11, 25, 16, 0)]
    [InlineData(2026, 11, 25, 9, 15)]
    public void SlotOutsideTheScheduleGridIsRejected(int anio, int mes, int dia, int hora, int minuto)
    {
        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud(new DateTime(anio, mes, dia, hora, minuto, 0)));

        Assert.Equal(
            "Elige un horario de lunes a viernes entre 08:00 y 16:00.",
            resultado.Errores[CampoSolicitud.Horario]);
        Assert.Empty(_repo.CitasDePaciente(_paciente.Id));
    }

    [Fact]
    public void TakenSlotIsNotSavedAndTheResultSaysSo()
    {
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        Assert.True(_servicio.Solicitar(_paciente.Id, Solicitud(inicio)).Exitoso);

        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud(inicio, "Otro motivo"));

        Assert.False(resultado.Exitoso);
        Assert.True(resultado.HorarioTomado);
        Assert.Equal("Ese horario ya fue tomado. Elige otro.", resultado.Errores[CampoSolicitud.Horario]);
        Assert.Single(_repo.CitasDePaciente(_paciente.Id));
    }

    [Fact]
    public void SameSlotWithAnotherDoctorIsAllowed()
    {
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        _servicio.Solicitar(_paciente.Id, Solicitud(inicio));

        var resultado = _servicio.Solicitar(_paciente.Id, new SolicitudCita(_otro.Id, inicio, "Consulta"));

        Assert.True(resultado.Exitoso);
    }

    [Fact]
    public void CancelingFreesTheSlotForANewRequest()
    {
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        var cita = _servicio.Solicitar(_paciente.Id, Solicitud(inicio)).Cita!;

        Assert.True(_servicio.Cancelar(cita.Id));

        Assert.Equal(EstadoCita.Cancelada, _repo.ObtenerCita(cita.Id)!.Estado);
        Assert.True(_servicio.Solicitar(_paciente.Id, Solicitud(inicio)).Exitoso);
    }

    [Fact]
    public void OnlyScheduledAppointmentsCanBeCanceled()
    {
        var cita = _servicio.Solicitar(_paciente.Id, Solicitud()).Cita!;
        _servicio.Cancelar(cita.Id);

        Assert.False(_servicio.Cancelar(cita.Id));
        Assert.False(_servicio.Cancelar(9999));
        Assert.Equal(EstadoCita.Cancelada, _repo.ObtenerCita(cita.Id)!.Estado);
    }

    [Fact]
    public void CompletedAppointmentsCannotBeCanceled()
    {
        var cita = _servicio.Solicitar(_paciente.Id, Solicitud()).Cita!;
        Assert.True(_servicio.MarcarCompletada(cita.Id));

        Assert.False(_servicio.Cancelar(cita.Id));
        Assert.Equal(EstadoCita.Completada, _repo.ObtenerCita(cita.Id)!.Estado);
    }

    [Fact]
    public void HistoryIsMostRecentFirstWithDoctorAndFilterByStatus()
    {
        var primera = _servicio.Solicitar(_paciente.Id, Solicitud(new DateTime(2026, 11, 25, 8, 0, 0))).Cita!;
        var segunda = _servicio.Solicitar(_paciente.Id, new SolicitudCita(_otro.Id, new DateTime(2026, 11, 26, 9, 0, 0), "Revisión")).Cita!;
        var tercera = _servicio.Solicitar(_paciente.Id, Solicitud(new DateTime(2026, 11, 27, 9, 0, 0))).Cita!;
        _servicio.Cancelar(segunda.Id);
        _servicio.MarcarCompletada(primera.Id);

        var todas = _servicio.Historial(_paciente.Id);

        Assert.Equal(new[] { tercera.Id, segunda.Id, primera.Id }, todas.Select(i => i.Cita.Id));
        Assert.Equal(_otro.Nombre, todas[1].Medico.Nombre);
        Assert.Equal(new[] { tercera.Id }, _servicio.Historial(_paciente.Id, EstadoCita.Programada).Select(i => i.Cita.Id));
        Assert.Equal(new[] { primera.Id }, _servicio.Historial(_paciente.Id, EstadoCita.Completada).Select(i => i.Cita.Id));
        Assert.Equal(new[] { segunda.Id }, _servicio.Historial(_paciente.Id, EstadoCita.Cancelada).Select(i => i.Cita.Id));
    }

    [Fact]
    public void HistoryOfAPatientWithoutAppointmentsIsEmpty()
    {
        Assert.Empty(_servicio.Historial(_paciente.Id));
    }
}
