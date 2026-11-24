using CitasMedicas.Core.Data;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class PacienteServiceRobustnessTests : IDisposable
{
    private readonly CitasRepository _repo = new(":memory:");
    private readonly PacienteService _servicio;

    public PacienteServiceRobustnessTests()
    {
        _servicio = new PacienteService(_repo, new FixedTimeProvider(new DateTime(2026, 11, 24, 10, 15, 0)));
    }

    public void Dispose() => _repo.Dispose();

    private static RegistroPaciente Valido(string cedula = "001-1234567-8") => new(
        "María Fernández Peña", cedula, "809-555-0142", "maria@correo.com", new DateTime(1991, 3, 14), "clave123");

    [Theory]
    [InlineData("٠٠١-١٢٣٤٥٦٧-٨")]
    [InlineData("００１-１２３４５６７-８")]
    public void CedulaWithNonAsciiDigitsIsRejected(string cedula)
    {
        var resultado = _servicio.Registrar(Valido(cedula));

        Assert.Equal("Escribe la cédula con el formato 000-0000000-0.", resultado.Errores[CampoPaciente.Cedula]);
        Assert.Equal(0, _repo.ContarPacientes());
    }

    [Theory]
    [InlineData("809-abc-5550142")]
    [InlineData("tel 8095550142")]
    [InlineData("809-555-0142 ext 5")]
    [InlineData("809*555*0142")]
    public void PhoneWithLettersOrSymbolsIsRejectedEvenIfItHasTenDigits(string telefono)
    {
        var resultado = _servicio.Registrar(Valido() with { Telefono = telefono });

        Assert.Equal("Escribe 10 dígitos: 809-555-0142.", resultado.Errores[CampoPaciente.Telefono]);
        Assert.Equal(0, _repo.ContarPacientes());
    }

    [Fact]
    public void BirthDateBefore1900IsRejectedAndJan1st1900IsAccepted()
    {
        var antiguo = _servicio.Registrar(Valido() with { FechaNacimiento = new DateTime(1899, 12, 31) });
        var limite = _servicio.Registrar(Valido() with { FechaNacimiento = new DateTime(1900, 1, 1) });

        Assert.Equal("La fecha de nacimiento no es válida.", antiguo.Errores[CampoPaciente.FechaNacimiento]);
        Assert.True(limite.Exitoso);
    }

    [Theory]
    [InlineData("a@b..com")]
    [InlineData("a@.b.com")]
    [InlineData("a@b.com.")]
    [InlineData(".a@b.com")]
    public void EmailWithEmptyLabelsIsRejected(string correo)
    {
        var resultado = _servicio.Registrar(Valido() with { Correo = correo });

        Assert.Equal("El correo no es válido.", resultado.Errores[CampoPaciente.Correo]);
    }

    [Fact]
    public void EmailLongerThan100CharactersIsRejected()
    {
        var largo = new string('a', 95) + "@b.com";

        var resultado = _servicio.Registrar(Valido() with { Correo = largo });

        Assert.Equal("El correo no es válido.", resultado.Errores[CampoPaciente.Correo]);
    }

    [Fact]
    public void NameOf100CharactersIsAcceptedAnd101IsRejected()
    {
        Assert.True(_servicio.Registrar(Valido() with { NombreCompleto = new string('a', 100) }).Exitoso);

        var resultado = _servicio.Registrar(Valido("001-7654321-0") with { NombreCompleto = new string('a', 101) });

        Assert.Equal("El nombre no puede pasar de 100 caracteres.", resultado.Errores[CampoPaciente.NombreCompleto]);
        Assert.Equal(1, _repo.ContarPacientes());
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void BlankNameIsRejected(string nombre)
    {
        var resultado = _servicio.Registrar(Valido() with { NombreCompleto = nombre });

        Assert.Equal("Escribe tu nombre completo.", resultado.Errores[CampoPaciente.NombreCompleto]);
    }

    [Fact]
    public void UpdatingAnUnknownPatientFailsLoudlyInsteadOfReportingSuccess()
    {
        Assert.Throws<KeyNotFoundException>(() => _servicio.Actualizar(9999, Valido()));
        Assert.Equal(0, _repo.ContarPacientes());
    }

    [Fact]
    public void UpdatingAnInactivePatientDoesNotReactivateIt()
    {
        var primero = _servicio.Registrar(Valido("001-1111111-1")).Paciente!;
        var segundo = _servicio.Registrar(Valido("001-2222222-2")).Paciente!;

        var resultado = _servicio.Actualizar(primero.Id, Valido("001-1111111-1") with { Telefono = "829-555-0199" });

        Assert.True(resultado.Exitoso);
        Assert.False(_repo.ObtenerPaciente(primero.Id)!.Activo);
        Assert.Equal(segundo.Id, _servicio.ObtenerActivo()!.Id);
    }

    [Fact]
    public void InvalidUpdateLeavesStoredDataUntouched()
    {
        var paciente = _servicio.Registrar(Valido()).Paciente!;

        var resultado = _servicio.Actualizar(paciente.Id, Valido() with { Telefono = "123", Correo = "mal" });

        Assert.False(resultado.Exitoso);
        var guardado = _repo.ObtenerPaciente(paciente.Id)!;
        Assert.Equal("809-555-0142", guardado.Telefono);
        Assert.Equal("maria@correo.com", guardado.Correo);
    }
}
