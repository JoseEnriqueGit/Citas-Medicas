using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;

namespace CitasMedicas.Core.Tests;

public class CitasRepositoryTests : IDisposable
{
    private readonly CitasRepository _repo = new(":memory:");

    public void Dispose() => _repo.Dispose();

    private static Paciente NuevoPaciente(string cedula = "001-1234567-8") => new()
    {
        NombreCompleto = "María Fernández Peña",
        Cedula = cedula,
        Telefono = "809-555-0142",
        Correo = "maria@correo.com",
        FechaNacimiento = new DateTime(1991, 3, 14)
    };

    private static Cita NuevaCita(int pacienteId, int medicoId, DateTime inicio) => new()
    {
        PacienteId = pacienteId,
        MedicoId = medicoId,
        Inicio = inicio,
        Motivo = "Control general",
        Estado = EstadoCita.Programada,
        CreadaEn = new DateTime(2026, 11, 20, 9, 0, 0)
    };

    [Fact]
    public void FirstStartSeedsNineDoctorsWithSpecialtyAndCenter()
    {
        var medicos = _repo.ListarMedicos();

        Assert.Equal(9, medicos.Count);
        Assert.All(medicos, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Nombre));
            Assert.False(string.IsNullOrWhiteSpace(m.Especialidad));
            Assert.False(string.IsNullOrWhiteSpace(m.Centro));
        });
        Assert.Equal(5, medicos.Select(m => m.Especialidad).Distinct().Count());
    }

    [Fact]
    public void ReopeningTheSameDatabaseDoesNotDuplicateDoctors()
    {
        var path = Path.Combine(Path.GetTempPath(), $"citas-{Guid.NewGuid():N}.db3");
        try
        {
            using (new CitasRepository(path)) { }
            using var segundo = new CitasRepository(path);

            Assert.Equal(9, segundo.ListarMedicos().Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void InsertedPatientBecomesTheActivePatient()
    {
        var paciente = _repo.InsertarPaciente(NuevoPaciente());

        Assert.True(paciente.Id > 0);
        var activo = _repo.ObtenerPacienteActivo();
        Assert.NotNull(activo);
        Assert.Equal(paciente.Id, activo!.Id);
        Assert.True(activo.Activo);
        Assert.Equal("001-1234567-8", activo.Cedula);
    }

    [Fact]
    public void WithoutPatientsThereIsNoActivePatient()
    {
        Assert.Null(_repo.ObtenerPacienteActivo());
    }

    [Fact]
    public void ExistsCedulaDetectsStoredCedula()
    {
        _repo.InsertarPaciente(NuevoPaciente());

        Assert.True(_repo.ExisteCedula("001-1234567-8"));
        Assert.False(_repo.ExisteCedula("001-7654321-0"));
    }

    [Fact]
    public void StoringTheSameCedulaTwiceFailsAndKeepsOnePatient()
    {
        _repo.InsertarPaciente(NuevoPaciente());

        Assert.Throws<CedulaDuplicadaException>(() => _repo.InsertarPaciente(NuevoPaciente()));
        Assert.Equal(1, _repo.ContarPacientes());
    }

    [Fact]
    public void InsertingScheduledAppointmentPersistsIt()
    {
        var paciente = _repo.InsertarPaciente(NuevoPaciente());
        var medico = _repo.ListarMedicos()[0];

        var cita = _repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, new DateTime(2026, 11, 25, 8, 0, 0)));

        var leida = _repo.ObtenerCita(cita.Id);
        Assert.NotNull(leida);
        Assert.Equal(new DateTime(2026, 11, 25, 8, 0, 0), leida!.Inicio);
        Assert.Equal(EstadoCita.Programada, leida.Estado);
        Assert.Equal("Control general", leida.Motivo);
    }

    [Fact]
    public void TwoScheduledAppointmentsInTheSameSlotAreRejected()
    {
        var paciente = _repo.InsertarPaciente(NuevoPaciente());
        var medico = _repo.ListarMedicos()[0];
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        _repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio));

        Assert.Throws<HorarioOcupadoException>(() => _repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio)));
        Assert.Single(_repo.CitasDePaciente(paciente.Id));
    }

    [Fact]
    public void SameSlotWithAnotherDoctorIsAllowed()
    {
        var paciente = _repo.InsertarPaciente(NuevoPaciente());
        var medicos = _repo.ListarMedicos();
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        _repo.InsertarCita(NuevaCita(paciente.Id, medicos[0].Id, inicio));

        _repo.InsertarCita(NuevaCita(paciente.Id, medicos[1].Id, inicio));

        Assert.Equal(2, _repo.CitasDePaciente(paciente.Id).Count);
    }

    [Fact]
    public void CancelledAppointmentFreesTheSlotForANewOne()
    {
        var paciente = _repo.InsertarPaciente(NuevoPaciente());
        var medico = _repo.ListarMedicos()[0];
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        var cita = _repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio));
        cita.Estado = EstadoCita.Cancelada;
        _repo.ActualizarCita(cita);

        _repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio));

        Assert.Equal(2, _repo.CitasDePaciente(paciente.Id).Count);
    }

    [Fact]
    public void ScheduledAppointmentsAreFilteredByDoctorAndDayAndStatus()
    {
        var paciente = _repo.InsertarPaciente(NuevoPaciente());
        var medicos = _repo.ListarMedicos();
        var dia = new DateTime(2026, 11, 25);
        _repo.InsertarCita(NuevaCita(paciente.Id, medicos[0].Id, dia.AddHours(8)));
        _repo.InsertarCita(NuevaCita(paciente.Id, medicos[0].Id, dia.AddHours(9)));
        _repo.InsertarCita(NuevaCita(paciente.Id, medicos[1].Id, dia.AddHours(8)));
        _repo.InsertarCita(NuevaCita(paciente.Id, medicos[0].Id, dia.AddDays(1).AddHours(8)));
        var cancelada = _repo.InsertarCita(NuevaCita(paciente.Id, medicos[0].Id, dia.AddHours(10)));
        cancelada.Estado = EstadoCita.Cancelada;
        _repo.ActualizarCita(cancelada);

        var citas = _repo.CitasProgramadas(medicos[0].Id, dia, dia.AddDays(1));

        Assert.Equal(new[] { dia.AddHours(8), dia.AddHours(9) }, citas.Select(c => c.Inicio).OrderBy(x => x));
    }

    [Fact]
    public void PatientHistoryIsReturnedMostRecentFirst()
    {
        var paciente = _repo.InsertarPaciente(NuevoPaciente());
        var medico = _repo.ListarMedicos()[0];
        var dia = new DateTime(2026, 11, 25);
        _repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, dia.AddHours(9)));
        _repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, dia.AddDays(2).AddHours(8)));
        _repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, dia.AddHours(8)));

        var historial = _repo.CitasDePaciente(paciente.Id);

        Assert.Equal(
            new[] { dia.AddDays(2).AddHours(8), dia.AddHours(9), dia.AddHours(8) },
            historial.Select(c => c.Inicio));
    }
}
