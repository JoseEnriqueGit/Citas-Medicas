using System.Text.RegularExpressions;
using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;

namespace CitasMedicas.Core.Services;

public enum CampoPaciente
{
    NombreCompleto,
    Cedula,
    Telefono,
    Correo,
    FechaNacimiento
}

public sealed record RegistroPaciente(
    string? NombreCompleto,
    string? Cedula,
    string? Telefono,
    string? Correo,
    DateTime? FechaNacimiento);

public sealed class ResultadoRegistro
{
    public Paciente? Paciente { get; init; }

    public IReadOnlyDictionary<CampoPaciente, string> Errores { get; init; } =
        new Dictionary<CampoPaciente, string>();

    public bool Exitoso => Paciente is not null && Errores.Count == 0;
}

public sealed partial class PacienteService
{
    private readonly CitasRepository _repositorio;
    private readonly TimeProvider _reloj;

    public PacienteService(CitasRepository repositorio, TimeProvider reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    public Paciente? ObtenerActivo() => _repositorio.ObtenerPacienteActivo();

    public ResultadoRegistro Registrar(RegistroPaciente registro)
    {
        var (datos, errores) = Validar(registro, null);
        if (errores.Count > 0)
        {
            return new ResultadoRegistro { Errores = errores };
        }

        try
        {
            return new ResultadoRegistro { Paciente = _repositorio.InsertarPaciente(datos!) };
        }
        catch (CedulaDuplicadaException ex)
        {
            return new ResultadoRegistro { Errores = new Dictionary<CampoPaciente, string> { [CampoPaciente.Cedula] = ex.Message } };
        }
    }

    public ResultadoRegistro Actualizar(int id, RegistroPaciente registro)
    {
        var (datos, errores) = Validar(registro, id);
        if (errores.Count > 0)
        {
            return new ResultadoRegistro { Errores = errores };
        }

        datos!.Id = id;
        datos.Activo = true;
        try
        {
            _repositorio.ActualizarPaciente(datos);
            return new ResultadoRegistro { Paciente = datos };
        }
        catch (CedulaDuplicadaException ex)
        {
            return new ResultadoRegistro { Errores = new Dictionary<CampoPaciente, string> { [CampoPaciente.Cedula] = ex.Message } };
        }
    }

    private (Paciente? Datos, Dictionary<CampoPaciente, string> Errores) Validar(RegistroPaciente registro, int? idActual)
    {
        var errores = new Dictionary<CampoPaciente, string>();

        var nombre = (registro.NombreCompleto ?? "").Trim();
        if (nombre.Length == 0)
        {
            errores[CampoPaciente.NombreCompleto] = "Escribe tu nombre completo.";
        }

        var cedula = (registro.Cedula ?? "").Trim();
        if (!CedulaRegex().IsMatch(cedula))
        {
            errores[CampoPaciente.Cedula] = "Escribe la cédula con el formato 000-0000000-0.";
        }
        else if (CedulaEnUsoPorOtro(cedula, idActual))
        {
            errores[CampoPaciente.Cedula] = "Esta cédula ya está registrada.";
        }

        var digitos = new string((registro.Telefono ?? "").Where(char.IsAsciiDigit).ToArray());
        if (digitos.Length != 10)
        {
            errores[CampoPaciente.Telefono] = "Escribe 10 dígitos: 809-555-0142.";
        }

        var correo = (registro.Correo ?? "").Trim();
        if (correo.Length > 0 && !CorreoRegex().IsMatch(correo))
        {
            errores[CampoPaciente.Correo] = "El correo no es válido.";
        }

        if (registro.FechaNacimiento is null)
        {
            errores[CampoPaciente.FechaNacimiento] = "Selecciona tu fecha de nacimiento.";
        }
        else if (registro.FechaNacimiento.Value.Date > _reloj.GetLocalNow().Date)
        {
            errores[CampoPaciente.FechaNacimiento] = "La fecha de nacimiento no puede ser futura.";
        }

        if (errores.Count > 0)
        {
            return (null, errores);
        }

        var datos = new Paciente
        {
            NombreCompleto = nombre,
            Cedula = cedula,
            Telefono = $"{digitos[..3]}-{digitos[3..6]}-{digitos[6..]}",
            Correo = correo.Length == 0 ? null : correo,
            FechaNacimiento = registro.FechaNacimiento!.Value.Date
        };
        return (datos, errores);
    }

    private bool CedulaEnUsoPorOtro(string cedula, int? idActual)
    {
        if (!_repositorio.ExisteCedula(cedula))
        {
            return false;
        }

        if (idActual is null)
        {
            return true;
        }

        var actual = _repositorio.ObtenerPaciente(idActual.Value);
        return actual?.Cedula != cedula;
    }

    [GeneratedRegex(@"^\d{3}-\d{7}-\d$")]
    private static partial Regex CedulaRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex CorreoRegex();
}
