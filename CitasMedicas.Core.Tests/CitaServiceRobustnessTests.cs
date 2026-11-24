using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class CitaServiceRobustnessTests : IDisposable
{
    private static readonly DateTime Martes = new(2026, 11, 24, 10, 15, 0);
    private const string ErrorGrilla = "Elige un horario de lunes a viernes entre 08:00 y 16:00.";

    private readonly CitasRepository _repo = new(":memory:");
    private readonly FixedTimeProvider _clock = new(Martes);
    private readonly CitaService _servicio;
    private readonly Paciente _paciente;
    private readonly Medico _medico;
    private readonly Medico _otro;

    public CitaServiceRobustnessTests()
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

    [Theory]
    [InlineData(1L)]
    [InlineData(9999L)]
    public void SlotWithSubMillisecondTicksIsRejectedSoItCannotBypassTheDuplicateRule(long ticksExtra)
    {
        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud(new DateTime(2026, 11, 25, 8, 0, 0).AddTicks(ticksExtra)));

        Assert.Equal(ErrorGrilla, resultado.Errores[CampoSolicitud.Horario]);
        Assert.Empty(_repo.CitasDePaciente(_paciente.Id));
    }

    [Fact]
    public void SlotWithSecondsIsRejected()
    {
        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud(new DateTime(2026, 11, 25, 8, 0, 30)));

        Assert.Equal(ErrorGrilla, resultado.Errores[CampoSolicitud.Horario]);
    }

    [Fact]
    public void InactivePatientCannotRequestAnAppointment()
    {
        var antiguo = _paciente;
        _repo.InsertarPaciente(new Paciente
        {
            NombreCompleto = "Carlos Jiménez",
            Cedula = "001-7654321-0",
            Telefono = "809-555-0143",
            FechaNacimiento = new DateTime(1988, 9, 2)
        });

        var resultado = _servicio.Solicitar(antiguo.Id, Solicitud());

        Assert.False(resultado.Exitoso);
        Assert.Equal("Regístrate para solicitar una cita.", resultado.Errores[CampoSolicitud.Paciente]);
        Assert.Empty(_repo.CitasDePaciente(antiguo.Id));
    }

    [Fact]
    public void UnknownDoctorIsRejectedWithoutStoringAnything()
    {
        var resultado = _servicio.Solicitar(_paciente.Id, new SolicitudCita(9999, new DateTime(2026, 11, 25, 8, 0, 0), "Consulta"));

        Assert.Equal("Elige un médico.", resultado.Errores[CampoSolicitud.Medico]);
        Assert.Empty(_repo.CitasDePaciente(_paciente.Id));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\t \r\n")]
    public void ReasonMadeOfWhitespaceControlCharactersIsRejected(string motivo)
    {
        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud(motivo: motivo));

        Assert.Equal("Escribe el motivo de la consulta.", resultado.Errores[CampoSolicitud.Motivo]);
    }

    [Fact]
    public void ReasonLengthIsMeasuredAfterTrimming()
    {
        var resultado = _servicio.Solicitar(_paciente.Id, Solicitud(motivo: "   " + new string('a', 200) + "   "));

        Assert.True(resultado.Exitoso);
        Assert.Equal(200, resultado.Cita!.Motivo.Length);
    }

    [Fact]
    public void BookingTheSlotThatStartsExactlyNowIsRejectedAndTheNextOneAccepted()
    {
        _clock.Set(new DateTime(2026, 11, 24, 10, 30, 0));

        var ahora = _servicio.Solicitar(_paciente.Id, Solicitud(new DateTime(2026, 11, 24, 10, 30, 0)));
        var siguiente = _servicio.Solicitar(_paciente.Id, Solicitud(new DateTime(2026, 11, 24, 11, 0, 0)));

        Assert.Equal("No puedes reservar un horario que ya pasó.", ahora.Errores[CampoSolicitud.Horario]);
        Assert.True(siguiente.Exitoso);
    }

    [Fact]
    public void BookingOneSecondBeforeTheSlotStartsIsAccepted()
    {
        _clock.Set(new DateTime(2026, 11, 24, 10, 29, 59));

        Assert.True(_servicio.Solicitar(_paciente.Id, Solicitud(new DateTime(2026, 11, 24, 10, 30, 0))).Exitoso);
    }

    [Fact]
    public void FailedRequestNeverTouchesTheExistingAppointmentInThatSlot()
    {
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        var original = _servicio.Solicitar(_paciente.Id, Solicitud(inicio, "Motivo original")).Cita!;

        var intento = _servicio.Solicitar(_paciente.Id, Solicitud(inicio, "Intento duplicado"));

        Assert.True(intento.HorarioTomado);
        var guardada = _repo.ObtenerCita(original.Id)!;
        Assert.Equal("Motivo original", guardada.Motivo);
        Assert.Equal(EstadoCita.Programada, guardada.Estado);
        Assert.Single(_repo.CitasDePaciente(_paciente.Id));
    }

    [Fact]
    public void CancelledAppointmentCannotBeMarkedCompleted()
    {
        var cita = _servicio.Solicitar(_paciente.Id, Solicitud()).Cita!;
        _servicio.Cancelar(cita.Id);

        Assert.False(_servicio.MarcarCompletada(cita.Id));
        Assert.Equal(EstadoCita.Cancelada, _repo.ObtenerCita(cita.Id)!.Estado);
    }

    [Fact]
    public void HistoryIgnoresAppointmentsWhoseDoctorNoLongerExists()
    {
        var valida = _servicio.Solicitar(_paciente.Id, Solicitud()).Cita!;
        _repo.InsertarCita(new Cita
        {
            PacienteId = _paciente.Id,
            MedicoId = 9999,
            Inicio = new DateTime(2026, 11, 26, 9, 0, 0),
            Motivo = "Huérfana",
            Estado = EstadoCita.Programada,
            CreadaEn = Martes
        });

        Assert.Equal(new[] { valida.Id }, _servicio.Historial(_paciente.Id).Select(i => i.Cita.Id));
    }

    [Fact]
    public void HistoryOrderIsDeterministicWhenTwoAppointmentsShareTheStartTime()
    {
        var inicio = new DateTime(2026, 11, 25, 9, 0, 0);
        var a = _servicio.Solicitar(_paciente.Id, Solicitud(inicio)).Cita!;
        var b = _servicio.Solicitar(_paciente.Id, new SolicitudCita(_otro.Id, inicio, "Consulta")).Cita!;

        Assert.Equal(new[] { b.Id, a.Id }, _servicio.Historial(_paciente.Id).Select(i => i.Cita.Id));
    }

    [Fact]
    public void HistoryOfAnUnknownPatientIsEmpty()
    {
        Assert.Empty(_servicio.Historial(9999));
    }
}
