using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class PacienteServiceMultiplePatientsTests : IDisposable
{
    private static readonly DateTime Martes = new(2026, 11, 24, 10, 15, 0);

    private readonly CitasRepository _repo = new(":memory:");
    private readonly FixedTimeProvider _clock = new(Martes);
    private readonly PacienteService _servicio;
    private readonly CitaService _citas;

    public PacienteServiceMultiplePatientsTests()
    {
        _servicio = new PacienteService(_repo, _clock);
        _citas = new CitaService(_repo, _clock);
    }

    public void Dispose() => _repo.Dispose();

    private static RegistroPaciente Datos(string cedula, string nombre = "María Fernández Peña") =>
        new(nombre, cedula, "809-555-0142", "maria@correo.com", new DateTime(1991, 3, 14));

    private Paciente Registrar(string cedula, string nombre = "María Fernández Peña") =>
        _servicio.Registrar(Datos(cedula, nombre)).Paciente!;

    [Fact]
    public void WithoutRegistrationsTheListIsEmpty()
    {
        Assert.Empty(_servicio.Listar());
    }

    [Fact]
    public void RegisteringASecondPatientMakesItActiveAndKeepsBothListedInOrder()
    {
        var primero = Registrar("001-1111111-1");
        var segundo = Registrar("001-2222222-2", "Carlos Jiménez");

        Assert.Equal(new[] { primero.Id, segundo.Id }, _servicio.Listar().Select(p => p.Id));
        Assert.Equal(segundo.Id, _servicio.ObtenerActivo()!.Id);
        Assert.Single(_servicio.Listar(), p => p.Activo);
    }

    [Fact]
    public void ActivatingAnotherPatientSwitchesTheActiveOneAndNothingElseChanges()
    {
        var primero = Registrar("001-1111111-1");
        var segundo = Registrar("001-2222222-2", "Carlos Jiménez");

        var activado = _servicio.Activar(primero.Id);

        Assert.Equal(primero.Id, activado.Id);
        Assert.Equal(primero.Id, _servicio.ObtenerActivo()!.Id);
        Assert.Single(_servicio.Listar(), p => p.Activo);
        Assert.Equal("Carlos Jiménez", _servicio.Listar().Single(p => p.Id == segundo.Id).NombreCompleto);
        Assert.Equal(2, _servicio.Listar().Count);
    }

    [Fact]
    public void ActivatingTheAlreadyActivePatientChangesNothing()
    {
        Registrar("001-1111111-1");
        var segundo = Registrar("001-2222222-2", "Carlos Jiménez");

        _servicio.Activar(segundo.Id);
        _servicio.Activar(segundo.Id);

        Assert.Equal(segundo.Id, _servicio.ObtenerActivo()!.Id);
        Assert.Single(_servicio.Listar(), p => p.Activo);
    }

    [Fact]
    public void ActivatingAnUnknownPatientFailsAndKeepsTheCurrentOne()
    {
        var primero = Registrar("001-1111111-1");

        Assert.Throws<KeyNotFoundException>(() => _servicio.Activar(9999));

        Assert.Equal(primero.Id, _servicio.ObtenerActivo()!.Id);
    }

    [Fact]
    public void RegisteringAnotherPatientWithARepeatedCedulaFailsAndKeepsTheActiveOne()
    {
        var primero = Registrar("001-1111111-1");

        var resultado = _servicio.Registrar(Datos("001-1111111-1", "Otra Persona"));

        Assert.Equal("Esta cédula ya está registrada.", resultado.Errores[CampoPaciente.Cedula]);
        Assert.Single(_servicio.Listar());
        Assert.Equal(primero.Id, _servicio.ObtenerActivo()!.Id);
    }

    [Fact]
    public void RegisteringAnotherPatientWithInvalidDataKeepsTheActiveOneAndStoresNothing()
    {
        var primero = Registrar("001-1111111-1");

        var resultado = _servicio.Registrar(new RegistroPaciente("", "mal", "123", "x", null));

        Assert.False(resultado.Exitoso);
        Assert.Single(_servicio.Listar());
        Assert.Equal(primero.Id, _servicio.ObtenerActivo()!.Id);
    }

    [Fact]
    public void EachPatientSeesOnlyTheirOwnHistory()
    {
        var medico = _repo.ListarMedicos()[0];
        var primero = Registrar("001-1111111-1");
        _citas.Solicitar(primero.Id, new SolicitudCita(medico.Id, new DateTime(2026, 11, 25, 8, 0, 0), "Control"));
        var segundo = Registrar("001-2222222-2", "Carlos Jiménez");
        _citas.Solicitar(segundo.Id, new SolicitudCita(medico.Id, new DateTime(2026, 11, 25, 9, 0, 0), "Revisión"));

        Assert.Equal("Control", _citas.Historial(primero.Id).Single().Cita.Motivo);
        Assert.Equal("Revisión", _citas.Historial(segundo.Id).Single().Cita.Motivo);
    }

    [Fact]
    public void OnlyTheActivePatientCanRequestAndSwitchingBackRestoresThatAbility()
    {
        var medico = _repo.ListarMedicos()[0];
        var primero = Registrar("001-1111111-1");
        Registrar("001-2222222-2", "Carlos Jiménez");
        var solicitud = new SolicitudCita(medico.Id, new DateTime(2026, 11, 25, 8, 0, 0), "Control");

        var inactivo = _citas.Solicitar(primero.Id, solicitud);
        _servicio.Activar(primero.Id);
        var activo = _citas.Solicitar(primero.Id, solicitud);

        Assert.Equal("Regístrate para solicitar una cita.", inactivo.Errores[CampoSolicitud.Paciente]);
        Assert.True(activo.Exitoso);
    }

    [Fact]
    public void ASlotTakenByOnePatientIsBusyForTheOtherOneToo()
    {
        var medico = _repo.ListarMedicos()[0];
        var primero = Registrar("001-1111111-1");
        var inicio = new DateTime(2026, 11, 25, 8, 0, 0);
        _citas.Solicitar(primero.Id, new SolicitudCita(medico.Id, inicio, "Control"));
        var segundo = Registrar("001-2222222-2", "Carlos Jiménez");

        var resultado = _citas.Solicitar(segundo.Id, new SolicitudCita(medico.Id, inicio, "Revisión"));

        Assert.True(resultado.HorarioTomado);
        Assert.Empty(_citas.Historial(segundo.Id));
    }

    [Fact]
    public void UpdatingTheActivePatientAfterSwitchingDoesNotChangeWhoIsActive()
    {
        var primero = Registrar("001-1111111-1");
        var segundo = Registrar("001-2222222-2", "Carlos Jiménez");
        _servicio.Activar(primero.Id);

        var resultado = _servicio.Actualizar(segundo.Id, Datos("001-2222222-2", "Carlos Jiménez Rosario"));

        Assert.True(resultado.Exitoso);
        Assert.Equal(primero.Id, _servicio.ObtenerActivo()!.Id);
        Assert.False(_servicio.Listar().Single(p => p.Id == segundo.Id).Activo);
    }

    [Fact]
    public void RegisteringUpdatingAndSwitchingPatientsNotifiesOncePerChange()
    {
        var avisos = 0;
        _servicio.PacientesCambiaron += () => avisos++;

        var primero = Registrar("001-1111111-1");
        var segundo = Registrar("001-2222222-2", "Carlos Jiménez");
        _servicio.Activar(primero.Id);
        _servicio.Actualizar(primero.Id, Datos("001-1111111-1", "María Fernández"));

        Assert.Equal(4, avisos);
        Assert.NotEqual(primero.Id, segundo.Id);
    }

    [Fact]
    public void NotificationArrivesWhenTheNewActivePatientIsAlreadyVisible()
    {
        Registrar("001-1111111-1");
        string? nombreVisto = null;
        _servicio.PacientesCambiaron += () => nombreVisto = _servicio.ObtenerActivo()!.NombreCompleto;

        Registrar("001-2222222-2", "Carlos Jiménez");

        Assert.Equal("Carlos Jiménez", nombreVisto);
    }

    [Fact]
    public void FailedRegistrationsUpdatesAndSwitchesNeverNotify()
    {
        var primero = Registrar("001-1111111-1");
        var avisos = 0;
        _servicio.PacientesCambiaron += () => avisos++;

        _servicio.Registrar(Datos("001-1111111-1", "Repetida"));
        _servicio.Registrar(new RegistroPaciente("", "", "", "", null));
        _servicio.Actualizar(primero.Id, new RegistroPaciente("", "", "", "", null));
        Assert.Throws<KeyNotFoundException>(() => _servicio.Activar(9999));

        Assert.Equal(0, avisos);
    }

    [Fact]
    public void TheActivePatientSurvivesClosingAndReopeningTheDatabase()
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"citas-pac-{Guid.NewGuid():N}.db3");
        try
        {
            int primeroId;
            using (var repo = new CitasRepository(ruta))
            {
                var servicio = new PacienteService(repo, _clock);
                primeroId = servicio.Registrar(Datos("001-1111111-1")).Paciente!.Id;
                servicio.Registrar(Datos("001-2222222-2", "Carlos Jiménez"));
                servicio.Activar(primeroId);
            }

            using var reabierto = new CitasRepository(ruta);

            Assert.Equal(primeroId, reabierto.ObtenerPacienteActivo()!.Id);
            Assert.Equal(2, reabierto.ListarPacientes().Count);
        }
        finally
        {
            try
            {
                File.Delete(ruta);
            }
            catch (IOException)
            {
            }
        }
    }
}
