using System.Globalization;
using System.Text.RegularExpressions;
using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;

namespace CitasMedicas.Core.Services;

public enum CampoMedico
{
    Nombre,
    Especialidad,
    Centro
}

public sealed record RegistroMedico(string? Nombre, string? Especialidad, string? Centro);

public sealed class ResultadoMedico
{
    public Medico? Medico { get; init; }

    public IReadOnlyDictionary<CampoMedico, string> Errores { get; init; } =
        new Dictionary<CampoMedico, string>();

    public bool Exitoso => Medico is not null && Errores.Count == 0;
}

public sealed partial class MedicoService
{
    public const int MaximoTexto = 100;

    private const string MensajeDuplicado = "Ya hay un médico con ese nombre, especialidad y centro.";

    private static readonly StringComparer Comparador = StringComparer.OrdinalIgnoreCase;

    private readonly CitasRepository _repositorio;
    private readonly object _gate = new();

    public MedicoService(CitasRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public event Action<Medico>? MedicoRegistrado;

    public IReadOnlyList<Medico> Listar() => _repositorio.ListarMedicos();

    public IReadOnlyList<string> Especialidades() =>
        _repositorio.ListarMedicos()
            .Select(m => m.Especialidad)
            .Distinct(Comparador)
            .Order(StringComparer.Create(CultureInfo.GetCultureInfo("es-DO"), true))
            .ToList();

    public ResultadoMedico Registrar(RegistroMedico registro)
    {
        var nombre = Normalizar(registro.Nombre);
        var especialidad = Normalizar(registro.Especialidad);
        var centro = Normalizar(registro.Centro);
        var errores = new Dictionary<CampoMedico, string>();

        Validar(errores, CampoMedico.Nombre, nombre, "Escribe el nombre del médico.", "El nombre");
        Validar(errores, CampoMedico.Especialidad, especialidad, "Escribe la especialidad.", "La especialidad");
        Validar(errores, CampoMedico.Centro, centro, "Escribe el centro médico.", "El centro");

        if (errores.Count > 0)
        {
            return new ResultadoMedico { Errores = errores };
        }

        var resultado = RegistrarSincronizado(nombre, especialidad, centro);
        if (resultado.Exitoso)
        {
            Notificar(resultado.Medico!);
        }

        return resultado;
    }

    private ResultadoMedico RegistrarSincronizado(string nombre, string especialidad, string centro)
    {
        lock (_gate)
        {
            var existentes = _repositorio.ListarMedicos();
            especialidad = Existente(existentes.Select(m => m.Especialidad), especialidad);
            centro = Existente(existentes.Select(m => m.Centro), centro);

            if (existentes.Any(m =>
                    Comparador.Equals(m.Nombre, nombre) &&
                    Comparador.Equals(m.Especialidad, especialidad) &&
                    Comparador.Equals(m.Centro, centro)))
            {
                return Duplicado();
            }

            try
            {
                var medico = _repositorio.InsertarMedico(new Medico
                {
                    Nombre = nombre,
                    Especialidad = especialidad,
                    Centro = centro
                });
                return new ResultadoMedico { Medico = medico };
            }
            catch (MedicoDuplicadoException)
            {
                return Duplicado();
            }
        }
    }

    private void Notificar(Medico medico)
    {
        foreach (var suscriptor in MedicoRegistrado?.GetInvocationList().Cast<Action<Medico>>() ?? [])
        {
            try
            {
                suscriptor(medico);
            }
            catch (Exception)
            {
            }
        }
    }

    private static ResultadoMedico Duplicado() =>
        new() { Errores = new Dictionary<CampoMedico, string> { [CampoMedico.Nombre] = MensajeDuplicado } };

    private static void Validar(Dictionary<CampoMedico, string> errores, CampoMedico campo, string valor, string requerido, string sujeto)
    {
        if (valor.Length == 0)
        {
            errores[campo] = requerido;
        }
        else if (valor.Length > MaximoTexto)
        {
            errores[campo] = $"{sujeto} no puede pasar de {MaximoTexto} caracteres.";
        }
    }

    private static string Existente(IEnumerable<string> valores, string candidato) =>
        valores.FirstOrDefault(v => Comparador.Equals(v, candidato)) ?? candidato;

    private static string Normalizar(string? texto) =>
        EspaciosRegex().Replace((texto ?? "").Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRegex();
}
