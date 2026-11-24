using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class SesionTests : IDisposable
{
    private const string Clave = "clave123";

    private readonly CitasRepository _repo = new(":memory:");
    private readonly PacienteService _servicio;

    public SesionTests()
    {
        _servicio = new PacienteService(_repo, new FixedTimeProvider(new DateTime(2026, 11, 24, 10, 15, 0)));
    }

    public void Dispose() => _repo.Dispose();

    private static RegistroPaciente Datos(string cedula = "001-1234567-8", string? clave = Clave) => new(
        "María Fernández Peña",
        cedula,
        "809-555-0142",
        "maria.fernandez@correo.com",
        new DateTime(1991, 3, 14),
        clave);

    [Fact]
    public void RegistrationStoresAHashAndNeverThePlainPassword()
    {
        var paciente = _servicio.Registrar(Datos()).Paciente!;

        var guardado = _repo.ObtenerPaciente(paciente.Id)!;
        Assert.False(string.IsNullOrEmpty(guardado.ContrasenaHash));
        Assert.DoesNotContain(Clave, guardado.ContrasenaHash);
    }

    [Fact]
    public void TheSamePasswordProducesDifferentHashesForDifferentPatients()
    {
        var a = _servicio.Registrar(Datos("001-1234567-8")).Paciente!;
        var b = _servicio.Registrar(Datos("002-7654321-0")).Paciente!;

        Assert.NotEqual(_repo.ObtenerPaciente(a.Id)!.ContrasenaHash, _repo.ObtenerPaciente(b.Id)!.ContrasenaHash);
    }

    [Theory]
    [InlineData(null, "Escribe una contraseña.")]
    [InlineData("", "Escribe una contraseña.")]
    [InlineData("   ", "Escribe una contraseña.")]
    [InlineData("12345", "Usa al menos 6 caracteres.")]
    public void RegistrationWithoutAUsablePasswordIsRejectedAndNothingIsStored(string? clave, string mensaje)
    {
        var resultado = _servicio.Registrar(Datos(clave: clave));

        Assert.False(resultado.Exitoso);
        Assert.Equal(mensaje, resultado.Errores[CampoPaciente.Contrasena]);
        Assert.Equal(0, _repo.ContarPacientes());
    }

    [Fact]
    public void AnOverlongPasswordIsRejected()
    {
        var resultado = _servicio.Registrar(Datos(clave: new string('a', 101)));

        Assert.Equal("La contraseña no puede pasar de 100 caracteres.", resultado.Errores[CampoPaciente.Contrasena]);
    }

    [Fact]
    public void PasswordErrorIsReportedTogetherWithTheOtherErrors()
    {
        var resultado = _servicio.Registrar(new RegistroPaciente("", "mal", "123", null, null, "1"));

        Assert.Equal(
            new[]
            {
                CampoPaciente.NombreCompleto, CampoPaciente.Cedula, CampoPaciente.Telefono,
                CampoPaciente.FechaNacimiento, CampoPaciente.Contrasena
            }.OrderBy(k => k),
            resultado.Errores.Keys.OrderBy(k => k));
    }

    [Fact]
    public void LoginWithTheRightPasswordActivatesThatPatient()
    {
        var paciente = _servicio.Registrar(Datos()).Paciente!;
        _servicio.CerrarSesion();

        var resultado = _servicio.IniciarSesion("001-1234567-8", Clave);

        Assert.True(resultado.Exitoso);
        Assert.Equal(paciente.Id, resultado.Paciente!.Id);
        Assert.Equal(paciente.Id, _servicio.ObtenerActivo()!.Id);
    }

    [Fact]
    public void LoginTrimsTheCedula()
    {
        _servicio.Registrar(Datos());
        _servicio.CerrarSesion();

        Assert.True(_servicio.IniciarSesion("  001-1234567-8  ", Clave).Exitoso);
    }

    [Fact]
    public void LoginWithAWrongPasswordFailsAndLeavesNobodyActive()
    {
        _servicio.Registrar(Datos());
        _servicio.CerrarSesion();

        var resultado = _servicio.IniciarSesion("001-1234567-8", "otraClave");

        Assert.False(resultado.Exitoso);
        Assert.Equal("Cédula o contraseña incorrectos.", resultado.Error);
        Assert.Null(_servicio.ObtenerActivo());
    }

    [Fact]
    public void LoginWithAnUnknownCedulaGivesTheSameMessageAsAWrongPassword()
    {
        _servicio.Registrar(Datos());
        _servicio.CerrarSesion();

        var resultado = _servicio.IniciarSesion("009-9999999-9", Clave);

        Assert.False(resultado.Exitoso);
        Assert.Equal("Cédula o contraseña incorrectos.", resultado.Error);
        Assert.Null(_servicio.ObtenerActivo());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", Clave)]
    [InlineData("001-1234567-8", "")]
    [InlineData("   ", "   ")]
    public void LoginWithABlankFieldAsksForBoth(string? cedula, string? clave)
    {
        _servicio.Registrar(Datos());
        _servicio.CerrarSesion();

        var resultado = _servicio.IniciarSesion(cedula, clave);

        Assert.False(resultado.Exitoso);
        Assert.Equal("Escribe tu cédula y tu contraseña.", resultado.Error);
        Assert.Null(_servicio.ObtenerActivo());
    }

    [Fact]
    public void PasswordsAreCaseSensitive()
    {
        _servicio.Registrar(Datos());
        _servicio.CerrarSesion();

        Assert.False(_servicio.IniciarSesion("001-1234567-8", "CLAVE123").Exitoso);
    }

    [Fact]
    public void LoggingOutLeavesNobodyActiveAndNotifies()
    {
        _servicio.Registrar(Datos());
        var avisos = 0;
        _servicio.PacientesCambiaron += () => avisos++;

        _servicio.CerrarSesion();

        Assert.Null(_servicio.ObtenerActivo());
        Assert.Equal(1, avisos);
        Assert.Equal(1, _repo.ContarPacientes());
    }

    [Fact]
    public void LoggingInAsAnotherPatientNeedsThatPatientsOwnPassword()
    {
        _servicio.Registrar(Datos("001-1234567-8", "claveDeMaria"));
        var segundo = _servicio.Registrar(Datos("002-7654321-0", "claveDeJose")).Paciente!;

        Assert.False(_servicio.IniciarSesion("001-1234567-8", "claveDeJose").Exitoso);
        Assert.Equal(segundo.Id, _servicio.ObtenerActivo()!.Id);

        var resultado = _servicio.IniciarSesion("001-1234567-8", "claveDeMaria");

        Assert.True(resultado.Exitoso);
        Assert.Equal(resultado.Paciente!.Id, _servicio.ObtenerActivo()!.Id);
    }

    [Fact]
    public void UpdatingProfileKeepsThePassword()
    {
        var paciente = _servicio.Registrar(Datos()).Paciente!;

        var actualizado = _servicio.Actualizar(paciente.Id, Datos() with { NombreCompleto = "María F. Peña", Contrasena = null });
        _servicio.CerrarSesion();

        Assert.True(actualizado.Exitoso);
        Assert.True(_servicio.IniciarSesion("001-1234567-8", Clave).Exitoso);
    }

    [Fact]
    public void ALegacyPatientWithoutPasswordSetsOneOnFirstLogin()
    {
        var legado = _repo.InsertarPaciente(new Paciente
        {
            NombreCompleto = "Carlos Jiménez",
            Cedula = "003-1111111-1",
            Telefono = "809-555-0100",
            FechaNacimiento = new DateTime(1985, 1, 1)
        });
        _servicio.CerrarSesion();

        var primera = _servicio.IniciarSesion("003-1111111-1", "nuevaClave");
        _servicio.CerrarSesion();
        var conOtra = _servicio.IniciarSesion("003-1111111-1", "otraClave");
        var conLaMisma = _servicio.IniciarSesion("003-1111111-1", "nuevaClave");

        Assert.True(primera.Exitoso);
        Assert.Equal(legado.Id, primera.Paciente!.Id);
        Assert.False(conOtra.Exitoso);
        Assert.True(conLaMisma.Exitoso);
    }

    [Fact]
    public void ALegacyPatientCannotSetAnUnusablePassword()
    {
        _repo.InsertarPaciente(new Paciente
        {
            NombreCompleto = "Carlos Jiménez",
            Cedula = "003-1111111-1",
            Telefono = "809-555-0100",
            FechaNacimiento = new DateTime(1985, 1, 1)
        });
        _servicio.CerrarSesion();

        var resultado = _servicio.IniciarSesion("003-1111111-1", "123");

        Assert.False(resultado.Exitoso);
        Assert.Equal("Cédula o contraseña incorrectos.", resultado.Error);
        Assert.Null(_servicio.ObtenerActivo());
        Assert.Null(_repo.ObtenerPacientePorCedula("003-1111111-1")!.ContrasenaHash);
    }

    [Fact]
    public void AShortPasswordGivesTheSameMessageWhetherOrNotTheCedulaExists()
    {
        _servicio.Registrar(Datos());
        _servicio.CerrarSesion();

        var existente = _servicio.IniciarSesion("001-1234567-8", "123");
        var inexistente = _servicio.IniciarSesion("009-9999999-9", "123");

        Assert.Equal(inexistente.Error, existente.Error);
    }

    [Fact]
    public void RejectedLoginAndProfileUpdateLeaveTheStoredHashByteIdentical()
    {
        var paciente = _servicio.Registrar(Datos()).Paciente!;
        var antes = _repo.ObtenerPaciente(paciente.Id)!.ContrasenaHash;
        _servicio.CerrarSesion();

        Assert.Equal("Cédula o contraseña incorrectos.", _servicio.IniciarSesion("001-1234567-8", "otraClave9").Error);
        _servicio.Actualizar(paciente.Id, Datos(clave: "nuevaClave1") with { Telefono = "809-555-0199" });

        Assert.Equal(antes, _repo.ObtenerPaciente(paciente.Id)!.ContrasenaHash);
        Assert.True(_servicio.IniciarSesion("001-1234567-8", Clave).Exitoso);
    }

    [Fact]
    public void ACorruptedStoredHashNeverLogsAnyoneIn()
    {
        var paciente = _servicio.Registrar(Datos()).Paciente!;
        paciente.ContrasenaHash = "basura";
        _repo.ActualizarPaciente(paciente);
        _servicio.CerrarSesion();

        var resultado = _servicio.IniciarSesion("001-1234567-8", "basura");

        Assert.False(resultado.Exitoso);
        Assert.Null(_servicio.ObtenerActivo());
    }
}
