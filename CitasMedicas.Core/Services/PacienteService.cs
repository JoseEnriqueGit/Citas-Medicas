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
    public const int MaximoTexto = 100;

    private static readonly DateTime FechaNacimientoMinima = new(1900, 1, 1);

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
        var existente = _repositorio.ObtenerPaciente(id)
            ?? throw new KeyNotFoundException($"No existe el paciente {id}.");

        var (datos, errores) = Validar(registro, id);
        if (errores.Count > 0)
        {
            return new ResultadoRegistro { Errores = errores };
        }

        datos!.Id = id;
        datos.Activo = existente.Activo;
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
        else if (nombre.Length > MaximoTexto)
        {
            errores[CampoPaciente.NombreCompleto] = $"El nombre no puede pasar de {MaximoTexto} caracteres.";
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

        var telefonoCrudo = registro.Telefono ?? "";
        var digitos = new string(telefonoCrudo.Where(char.IsAsciiDigit).ToArray());
        if (digitos.Length != 10 || !telefonoCrudo.All(EsCaracterDeTelefono))
        {
            errores[CampoPaciente.Telefono] = "Escribe 10 dígitos: 809-555-0142.";
        }

        var correo = (registro.Correo ?? "").Trim();
        if (correo.Length > 0 && (correo.Length > MaximoTexto || !CorreoRegex().IsMatch(correo)))
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
        else if (registro.FechaNacimiento.Value.Date < FechaNacimientoMinima)
        {
            errores[CampoPaciente.FechaNacimiento] = "La fecha de nacimiento no es válida.";
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

    private static bool EsCaracterDeTelefono(char c) =>
        char.IsAsciiDigit(c) || c is ' ' or '-' or '(' or ')' or '.';

    [GeneratedRegex(@"^[0-9]{3}-[0-9]{7}-[0-9]$")]
    private static partial Regex CedulaRegex();

    [GeneratedRegex(@"^[^@\s.]+(\.[^@\s.]+)*@[^@\s.]+(\.[^@\s.]+)+$")]
    private static partial Regex CorreoRegex();
}
