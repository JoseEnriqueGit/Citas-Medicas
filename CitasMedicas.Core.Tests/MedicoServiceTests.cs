using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class MedicoServiceTests : IDisposable
{
    private static readonly DateTime Martes = new(2026, 11, 24, 10, 15, 0);

    private readonly CitasRepository _repo = new(":memory:");
    private readonly FixedTimeProvider _clock = new(Martes);
    private readonly MedicoService _servicio;

    public MedicoServiceTests()
    {
        _servicio = new MedicoService(_repo);
    }

    public void Dispose() => _repo.Dispose();

    private static RegistroMedico Valido() => new("Dra. Marta Peña", "Neurología", "Hospital San Rafael");

    [Fact]
    public void ValidDoctorIsStoredAndListedAfterTheSeededOnes()
    {
        var resultado = _servicio.Registrar(Valido());

        Assert.True(resultado.Exitoso);
        Assert.Empty(resultado.Errores);
        Assert.True(resultado.Medico!.Id > 0);
        Assert.Equal(10, _servicio.Listar().Count);
        Assert.Equal("Dra. Marta Peña", _servicio.Listar()[^1].Nombre);
    }

    [Fact]
    public void FieldsAreTrimmedAndInnerWhitespaceIsCollapsed()
    {
        var resultado = _servicio.Registrar(new RegistroMedico("  Dra.   Marta    Peña ", "  Neurología ", "Hospital   San  Rafael  "));

        Assert.True(resultado.Exitoso);
        Assert.Equal("Dra. Marta Peña", resultado.Medico!.Nombre);
        Assert.Equal("Neurología", resultado.Medico.Especialidad);
        Assert.Equal("Hospital San Rafael", resultado.Medico.Centro);
    }

    [Fact]
    public void EmptyFormReportsTheThreeRequiredFieldsAtOnceAndStoresNothing()
    {
        var resultado = _servicio.Registrar(new RegistroMedico(null, "   ", ""));

        Assert.False(resultado.Exitoso);
        Assert.Equal(
            new[] { CampoMedico.Nombre, CampoMedico.Especialidad, CampoMedico.Centro },
            resultado.Errores.Keys.OrderBy(k => k));
        Assert.Equal("Escribe el nombre del médico.", resultado.Errores[CampoMedico.Nombre]);
        Assert.Equal("Escribe la especialidad.", resultado.Errores[CampoMedico.Especialidad]);
        Assert.Equal("Escribe el centro médico.", resultado.Errores[CampoMedico.Centro]);
        Assert.Equal(9, _servicio.Listar().Count);
    }

    [Fact]
    public void EachFieldAcceptsExactly100CharactersAndRejects101()
    {
        var cien = new string('a', 100);
        var ciento1 = new string('a', 101);

        Assert.True(_servicio.Registrar(new RegistroMedico(cien, cien, cien)).Exitoso);

        var resultado = _servicio.Registrar(new RegistroMedico(ciento1, ciento1, ciento1));

        Assert.Equal("El nombre no puede pasar de 100 caracteres.", resultado.Errores[CampoMedico.Nombre]);
        Assert.Equal("La especialidad no puede pasar de 100 caracteres.", resultado.Errores[CampoMedico.Especialidad]);
        Assert.Equal("El centro no puede pasar de 100 caracteres.", resultado.Errores[CampoMedico.Centro]);
        Assert.Equal(10, _servicio.Listar().Count);
    }

    [Fact]
    public void SameNameSpecialtyAndCenterIsRejectedAsDuplicate()
    {
        _servicio.Registrar(Valido());

        var resultado = _servicio.Registrar(Valido());

        Assert.False(resultado.Exitoso);
        Assert.Equal("Ya hay un médico con ese nombre, especialidad y centro.", resultado.Errores[CampoMedico.Nombre]);
        Assert.Equal(10, _servicio.Listar().Count);
    }

    [Theory]
    [InlineData("DRA. MARTA PEÑA", "NEUROLOGÍA", "HOSPITAL SAN RAFAEL")]
    [InlineData("dra. marta peña", "neurología", "hospital san rafael")]
    [InlineData("Dra.  Marta Peña", "Neurología", "Hospital San  Rafael")]
    public void DuplicateDetectionIgnoresCaseAndExtraSpaces(string nombre, string especialidad, string centro)
    {
        _servicio.Registrar(Valido());

        var resultado = _servicio.Registrar(new RegistroMedico(nombre, especialidad, centro));

        Assert.Equal("Ya hay un médico con ese nombre, especialidad y centro.", resultado.Errores[CampoMedico.Nombre]);
        Assert.Equal(10, _servicio.Listar().Count);
    }

    [Fact]
    public void SameNameInAnotherCenterOrSpecialtyIsAllowed()
    {
        _servicio.Registrar(Valido());

        Assert.True(_servicio.Registrar(Valido() with { Centro = "Clínica Santa Lucía" }).Exitoso);
        Assert.True(_servicio.Registrar(Valido() with { Especialidad = "Medicina general" }).Exitoso);
        Assert.Equal(12, _servicio.Listar().Count);
    }

    [Fact]
    public void SpecialtyAndCenterReuseTheExistingSpellingSoFiltersDoNotSplit()
    {
        var resultado = _servicio.Registrar(new RegistroMedico("Dr. Pablo Rojas", "cardiología", "CLÍNICA SANTA LUCÍA"));

        Assert.Equal("Cardiología", resultado.Medico!.Especialidad);
        Assert.Equal("Clínica Santa Lucía", resultado.Medico.Centro);
        Assert.Equal(5, _servicio.Especialidades().Count);
    }

    [Fact]
    public void SpecialtiesAreDistinctAndSortedAndIncludeNewOnes()
    {
        Assert.Equal(
            new[] { "Cardiología", "Dermatología", "Ginecología", "Medicina general", "Pediatría" },
            _servicio.Especialidades());

        _servicio.Registrar(Valido());

        Assert.Equal(6, _servicio.Especialidades().Count);
        Assert.Contains("Neurología", _servicio.Especialidades());
    }

    [Fact]
    public void NewDoctorHasASixteenBlockScheduleAndANextFreeSlot()
    {
        var medico = _servicio.Registrar(Valido()).Medico!;
        var horarios = new HorarioService(_repo, _clock);

        var bloques = horarios.Bloques(medico.Id, new DateTime(2026, 11, 25));

        Assert.Equal(16, bloques.Count);
        Assert.All(bloques, b => Assert.True(b.Libre));
        Assert.Equal(new DateTime(2026, 11, 24, 10, 30, 0), horarios.ProximoHorarioLibre(medico.Id));
    }

    [Fact]
    public void AppointmentCanBeRequestedWithANewDoctorAndAppearsInHistory()
    {
        var medico = _servicio.Registrar(Valido()).Medico!;
        var paciente = _repo.InsertarPaciente(new Paciente
        {
            NombreCompleto = "María Fernández Peña",
            Cedula = "001-1234567-8",
            Telefono = "809-555-0142",
            FechaNacimiento = new DateTime(1991, 3, 14)
        });
        var citas = new CitaService(_repo, _clock);

        var resultado = citas.Solicitar(paciente.Id, new SolicitudCita(medico.Id, new DateTime(2026, 11, 25, 9, 0, 0), "Dolor de cabeza"));

        Assert.True(resultado.Exitoso);
        Assert.Equal("Dra. Marta Peña", citas.Historial(paciente.Id).Single().Medico.Nombre);
    }

    [Fact]
    public void RegisteredDoctorSurvivesClosingAndReopeningTheDatabase()
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"citas-med-{Guid.NewGuid():N}.db3");
        try
        {
            using (var repo = new CitasRepository(ruta))
            {
                Assert.True(new MedicoService(repo).Registrar(Valido()).Exitoso);
            }

            using var reabierto = new CitasRepository(ruta);

            Assert.Equal(10, reabierto.ListarMedicos().Count);
            Assert.Equal("Neurología", reabierto.ListarMedicos()[^1].Especialidad);
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

    [Fact]
    public void SuccessfulRegistrationNotifiesOnceWithTheDoctorAlreadyListed()
    {
        var avisos = new List<(Medico Medico, int Total)>();
        _servicio.MedicoRegistrado += m => avisos.Add((m, _servicio.Listar().Count));

        _servicio.Registrar(Valido());

        var aviso = Assert.Single(avisos);
        Assert.Equal("Dra. Marta Peña", aviso.Medico.Nombre);
        Assert.Equal(10, aviso.Total);
    }

    [Fact]
    public void FailedOrDuplicateRegistrationsNeverNotify()
    {
        _servicio.Registrar(Valido());
        var avisos = 0;
        _servicio.MedicoRegistrado += _ => avisos++;

        _servicio.Registrar(Valido());
        _servicio.Registrar(new RegistroMedico("", "", ""));

        Assert.Equal(0, avisos);
    }

    [Fact]
    public void AFailingSubscriberDoesNotUndoTheRegistration()
    {
        _servicio.MedicoRegistrado += _ => throw new InvalidOperationException("suscriptor roto");

        var resultado = _servicio.Registrar(Valido());

        Assert.True(resultado.Exitoso);
        Assert.Equal(10, _servicio.Listar().Count);
    }

    [Fact]
    public void TwentyConcurrentRegistrationsOfTheSameDoctorProduceExactlyOne()
    {
        var exitosas = 0;

        Parallel.For(0, 20, _ =>
        {
            if (_servicio.Registrar(Valido()).Exitoso)
            {
                Interlocked.Increment(ref exitosas);
            }
        });

        Assert.Equal(1, exitosas);
        Assert.Equal(10, _servicio.Listar().Count);
    }
}
