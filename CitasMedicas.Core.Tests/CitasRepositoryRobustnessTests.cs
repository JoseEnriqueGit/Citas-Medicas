using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using SQLite;

namespace CitasMedicas.Core.Tests;

public class CitasRepositoryRobustnessTests
{
    private static string RutaTemporal() => Path.Combine(Path.GetTempPath(), $"citas-rob-{Guid.NewGuid():N}.db3");

    private static void Borrar(string ruta)
    {
        try
        {
            File.Delete(ruta);
        }
        catch (IOException)
        {
        }
    }

    private static Paciente NuevoPaciente() => new()
    {
        NombreCompleto = "María Fernández Peña",
        Cedula = "001-1234567-8",
        Telefono = "809-555-0142",
        FechaNacimiento = new DateTime(1991, 3, 14)
    };

    private static Cita NuevaCita(int pacienteId, int medicoId, DateTime inicio, EstadoCita estado = EstadoCita.Programada) => new()
    {
        PacienteId = pacienteId,
        MedicoId = medicoId,
        Inicio = inicio,
        Motivo = "Consulta",
        Estado = estado,
        CreadaEn = new DateTime(2026, 11, 20, 9, 0, 0)
    };

    [Fact]
    public void TheDatabaseItselfRejectsADuplicateSlotEvenFromAnotherConnection()
    {
        var ruta = RutaTemporal();
        try
        {
            using var primero = new CitasRepository(ruta);
            using var segundo = new CitasRepository(ruta);
            var paciente = primero.InsertarPaciente(NuevoPaciente());
            var medico = primero.ListarMedicos()[0];
            var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
            primero.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio));

            Assert.Throws<HorarioOcupadoException>(() => segundo.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio)));
            Assert.Single(segundo.CitasDePaciente(paciente.Id));
        }
        finally
        {
            Borrar(ruta);
        }
    }

    [Fact]
    public void TwentyConcurrentRequestsForTheSameSlotProduceExactlyOneWinner()
    {
        using var repo = new CitasRepository(":memory:");
        var paciente = repo.InsertarPaciente(NuevoPaciente());
        var medico = repo.ListarMedicos()[0];
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        var ganadoras = 0;
        var rechazadas = 0;

        Parallel.For(0, 20, _ =>
        {
            try
            {
                repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio));
                Interlocked.Increment(ref ganadoras);
            }
            catch (HorarioOcupadoException)
            {
                Interlocked.Increment(ref rechazadas);
            }
        });

        Assert.Equal(1, ganadoras);
        Assert.Equal(19, rechazadas);
        Assert.Single(repo.CitasDePaciente(paciente.Id));
    }

    [Fact]
    public void ReactivatingACancelledAppointmentIntoATakenSlotIsRejectedAndStaysCancelled()
    {
        using var repo = new CitasRepository(":memory:");
        var paciente = repo.InsertarPaciente(NuevoPaciente());
        var medico = repo.ListarMedicos()[0];
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        var cancelada = repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio, EstadoCita.Cancelada));
        repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio));

        cancelada.Estado = EstadoCita.Programada;

        Assert.Throws<HorarioOcupadoException>(() => repo.ActualizarCita(cancelada));
        Assert.Equal(EstadoCita.Cancelada, repo.ObtenerCita(cancelada.Id)!.Estado);
    }

    [Fact]
    public void ManyCancelledAppointmentsInTheSameSlotAreAllowed()
    {
        using var repo = new CitasRepository(":memory:");
        var paciente = repo.InsertarPaciente(NuevoPaciente());
        var medico = repo.ListarMedicos()[0];
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);

        for (var i = 0; i < 5; i++)
        {
            repo.InsertarCita(NuevaCita(paciente.Id, medico.Id, inicio, EstadoCita.Cancelada));
        }

        Assert.Equal(5, repo.CitasDePaciente(paciente.Id).Count);
    }

    [Fact]
    public void DuplicateCedulaFromAnotherConnectionIsRejectedAndKeepsTheActivePatient()
    {
        var ruta = RutaTemporal();
        try
        {
            using var primero = new CitasRepository(ruta);
            using var segundo = new CitasRepository(ruta);
            var original = primero.InsertarPaciente(NuevoPaciente());

            Assert.Throws<CedulaDuplicadaException>(() => segundo.InsertarPaciente(NuevoPaciente()));

            Assert.Equal(1, segundo.ContarPacientes());
            Assert.Equal(original.Id, segundo.ObtenerPacienteActivo()!.Id);
        }
        finally
        {
            Borrar(ruta);
        }
    }

    [Fact]
    public void AFileThatIsNotADatabaseMakesTheConstructorFailLoudly()
    {
        var ruta = RutaTemporal();
        try
        {
            File.WriteAllBytes(ruta, Enumerable.Range(0, 2048).Select(i => (byte)(i * 31 % 251)).ToArray());

            Assert.ThrowsAny<SQLiteException>(() => new CitasRepository(ruta));
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Borrar(ruta);
        }
    }

    [Fact]
    public void DataSurvivesClosingAndReopeningTheDatabase()
    {
        var ruta = RutaTemporal();
        try
        {
            int pacienteId;
            var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
            using (var repo = new CitasRepository(ruta))
            {
                pacienteId = repo.InsertarPaciente(NuevoPaciente()).Id;
                repo.InsertarCita(NuevaCita(pacienteId, repo.ListarMedicos()[0].Id, inicio));
            }

            using var reabierto = new CitasRepository(ruta);

            Assert.Equal(pacienteId, reabierto.ObtenerPacienteActivo()!.Id);
            Assert.Equal(inicio, reabierto.CitasDePaciente(pacienteId).Single().Inicio);
            Assert.Equal(9, reabierto.ListarMedicos().Count);
        }
        finally
        {
            Borrar(ruta);
        }
    }
}
