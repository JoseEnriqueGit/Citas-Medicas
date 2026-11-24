using CitasMedicas.Core.Data;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class PacienteServiceTests : IDisposable
{
    private readonly CitasRepository _repo = new(":memory:");
    private readonly PacienteService _servicio;

    public PacienteServiceTests()
    {
        _servicio = new PacienteService(_repo, new FixedTimeProvider(new DateTime(2026, 11, 24, 10, 15, 0)));
    }

    public void Dispose() => _repo.Dispose();

    private static RegistroPaciente Valido(string cedula = "001-1234567-8") => new(
        "María Fernández Peña",
        cedula,
        "809-555-0142",
        "maria.fernandez@correo.com",
        new DateTime(1991, 3, 14),
        "clave123");

    [Fact]
    public void ValidRegistrationStoresAnActivePatient()
    {
        var resultado = _servicio.Registrar(Valido());

        Assert.True(resultado.Exitoso);
        Assert.Empty(resultado.Errores);
        Assert.NotNull(resultado.Paciente);
        Assert.True(resultado.Paciente!.Activo);
        Assert.Equal("María Fernández Peña", resultado.Paciente.NombreCompleto);
        Assert.Equal(resultado.Paciente.Id, _servicio.ObtenerActivo()!.Id);
    }

    [Fact]
    public void EmptyFormReportsEveryRequiredFieldAtOnceAndStoresNothing()
    {
        var resultado = _servicio.Registrar(new RegistroPaciente(null, null, null, null, null));

        Assert.False(resultado.Exitoso);
        Assert.Equal(
            new[] { CampoPaciente.NombreCompleto, CampoPaciente.Cedula, CampoPaciente.Telefono, CampoPaciente.FechaNacimiento, CampoPaciente.Contrasena },
            resultado.Errores.Keys.OrderBy(k => k));
        Assert.Equal("Escribe tu nombre completo.", resultado.Errores[CampoPaciente.NombreCompleto]);
        Assert.Equal("Escribe la cédula con el formato 000-0000000-0.", resultado.Errores[CampoPaciente.Cedula]);
        Assert.Equal("Escribe 10 dígitos: 809-555-0142.", resultado.Errores[CampoPaciente.Telefono]);
        Assert.Equal("Selecciona tu fecha de nacimiento.", resultado.Errores[CampoPaciente.FechaNacimiento]);
        Assert.Equal(0, _repo.ContarPacientes());
    }

    [Fact]
    public void DuplicateCedulaPlusOtherErrorsAreAllReportedTogetherAndNothingIsSaved()
    {
        _servicio.Registrar(Valido());

        var resultado = _servicio.Registrar(new RegistroPaciente(
            "Carlos Jiménez", "001-1234567-8", "809-555", "carlos@correo", new DateTime(1988, 9, 2), "clave123"));

        Assert.False(resultado.Exitoso);
        Assert.Equal(3, resultado.Errores.Count);
        Assert.Equal("Esta cédula ya está registrada.", resultado.Errores[CampoPaciente.Cedula]);
        Assert.Equal("Escribe 10 dígitos: 809-555-0142.", resultado.Errores[CampoPaciente.Telefono]);
        Assert.Equal("El correo no es válido.", resultado.Errores[CampoPaciente.Correo]);
        Assert.Equal(1, _repo.ContarPacientes());
        Assert.Equal("María Fernández Peña", _servicio.ObtenerActivo()!.NombreCompleto);
    }

    [Theory]
    [InlineData("00112345678")]
    [InlineData("001-123456-78")]
    [InlineData("abc-1234567-8")]
    [InlineData("001-1234567-89")]
    public void CedulaMustMatchTheDashedFormat(string cedula)
    {
        var resultado = _servicio.Registrar(Valido(cedula));

        Assert.Equal("Escribe la cédula con el formato 000-0000000-0.", resultado.Errores[CampoPaciente.Cedula]);
    }

    [Theory]
    [InlineData("8095550142")]
    [InlineData("809 555 0142")]
    [InlineData("(809) 555-0142")]
    public void PhoneWithTenDigitsIsAcceptedAndNormalized(string telefono)
    {
        var resultado = _servicio.Registrar(Valido() with { Telefono = telefono });

        Assert.True(resultado.Exitoso);
        Assert.Equal("809-555-0142", resultado.Paciente!.Telefono);
    }

    [Theory]
    [InlineData("809-555")]
    [InlineData("809-555-01422")]
    public void PhoneWithoutTenDigitsIsRejected(string telefono)
    {
        var resultado = _servicio.Registrar(Valido() with { Telefono = telefono });

        Assert.Equal("Escribe 10 dígitos: 809-555-0142.", resultado.Errores[CampoPaciente.Telefono]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmailIsOptional(string? correo)
    {
        var resultado = _servicio.Registrar(Valido() with { Correo = correo });

        Assert.True(resultado.Exitoso);
        Assert.Null(resultado.Paciente!.Correo);
    }

    [Theory]
    [InlineData("carlos@correo")]
    [InlineData("carlos")]
    [InlineData("@correo.com")]
    [InlineData("car los@correo.com")]
    public void WrittenEmailMustBeValid(string correo)
    {
        var resultado = _servicio.Registrar(Valido() with { Correo = correo });

        Assert.Equal("El correo no es válido.", resultado.Errores[CampoPaciente.Correo]);
    }

    [Fact]
    public void BirthDateInTheFutureIsRejected()
    {
        var resultado = _servicio.Registrar(Valido() with { FechaNacimiento = new DateTime(2026, 11, 25) });

        Assert.Equal("La fecha de nacimiento no puede ser futura.", resultado.Errores[CampoPaciente.FechaNacimiento]);
    }

    [Fact]
    public void BirthDateTodayIsAccepted()
    {
        var resultado = _servicio.Registrar(Valido() with { FechaNacimiento = new DateTime(2026, 11, 24) });

        Assert.True(resultado.Exitoso);
    }

    [Fact]
    public void FieldsAreTrimmedBeforeStoring()
    {
        var resultado = _servicio.Registrar(Valido() with { NombreCompleto = "  María Fernández Peña  ", Cedula = " 001-1234567-8 " });

        Assert.True(resultado.Exitoso);
        Assert.Equal("María Fernández Peña", resultado.Paciente!.NombreCompleto);
        Assert.Equal("001-1234567-8", resultado.Paciente.Cedula);
    }

    [Fact]
    public void WithoutRegistrationThereIsNoActivePatient()
    {
        Assert.Null(_servicio.ObtenerActivo());
    }

    [Fact]
    public void UpdatingKeepingTheSameCedulaSavesTheChanges()
    {
        var paciente = _servicio.Registrar(Valido()).Paciente!;

        var resultado = _servicio.Actualizar(paciente.Id, Valido() with { Telefono = "829-555-0199" });

        Assert.True(resultado.Exitoso);
        Assert.Equal("829-555-0199", _servicio.ObtenerActivo()!.Telefono);
    }

    [Fact]
    public void UpdatingToAnotherPatientsCedulaIsRejectedAndKeepsOldData()
    {
        _servicio.Registrar(Valido("001-1111111-1"));
        var segundo = _servicio.Registrar(Valido("001-2222222-2")).Paciente!;

        var resultado = _servicio.Actualizar(segundo.Id, Valido("001-1111111-1") with { NombreCompleto = "Otro Nombre" });

        Assert.Equal("Esta cédula ya está registrada.", resultado.Errores[CampoPaciente.Cedula]);
        Assert.Equal("001-2222222-2", _servicio.ObtenerActivo()!.Cedula);
        Assert.Equal("María Fernández Peña", _servicio.ObtenerActivo()!.NombreCompleto);
    }
}
