using System.Globalization;
using System.Text;
using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;

namespace CitasMedicas.Core.Services;

public sealed record ResumenMedico(Medico Medico, int Total, int Programadas, int Completadas, int Canceladas);

public sealed record ResumenEspecialidad(string Especialidad, int Total);

public sealed record Reporte(
    DateTime GeneradoEn,
    DateTime? Desde,
    DateTime? Hasta,
    int Total,
    int Programadas,
    int Completadas,
    int Canceladas,
    IReadOnlyList<ResumenMedico> PorMedico,
    IReadOnlyList<ResumenEspecialidad> PorEspecialidad,
    int TotalPacientes,
    int TotalMedicos)
{
    public double TasaCancelacion => Total == 0 ? 0 : (double)Canceladas / Total;
}

public sealed class ReporteService
{
    private const string Encabezado = "Fecha,Hora,Paciente,Cédula,Médico,Especialidad,Centro,Estado,Motivo";

    private static readonly StringComparer Comparador = StringComparer.Create(CultureInfo.GetCultureInfo("es-DO"), true);

    private readonly CitasRepository _repositorio;
    private readonly TimeProvider _reloj;

    public ReporteService(CitasRepository repositorio, TimeProvider reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    public Reporte Generar(DateTime? desde, DateTime? hasta)
    {
        var citas = Consultar(desde, hasta);
        var medicos = _repositorio.ListarMedicos();
        var porId = medicos.ToDictionary(m => m.Id);

        var porMedico = medicos
            .Select(m =>
            {
                var propias = citas.Where(c => c.MedicoId == m.Id).ToList();
                return new ResumenMedico(
                    m,
                    propias.Count,
                    propias.Count(c => c.Estado == EstadoCita.Programada),
                    propias.Count(c => c.Estado == EstadoCita.Completada),
                    propias.Count(c => c.Estado == EstadoCita.Cancelada));
            })
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.Medico.Nombre, Comparador)
            .ToList();

        var porEspecialidad = citas
            .Where(c => porId.ContainsKey(c.MedicoId))
            .GroupBy(c => porId[c.MedicoId].Especialidad, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ResumenEspecialidad(g.Key, g.Count()))
            .OrderByDescending(e => e.Total)
            .ThenBy(e => e.Especialidad, Comparador)
            .ToList();

        return new Reporte(
            _reloj.GetLocalNow().DateTime,
            desde?.Date,
            hasta?.Date,
            citas.Count,
            citas.Count(c => c.Estado == EstadoCita.Programada),
            citas.Count(c => c.Estado == EstadoCita.Completada),
            citas.Count(c => c.Estado == EstadoCita.Cancelada),
            porMedico,
            porEspecialidad,
            _repositorio.ContarPacientes(),
            medicos.Count);
    }

    public string ExportarCsv(DateTime? desde, DateTime? hasta)
    {
        var citas = Consultar(desde, hasta);
        var medicos = _repositorio.ListarMedicos().ToDictionary(m => m.Id);
        var pacientes = _repositorio.ListarPacientes().ToDictionary(p => p.Id);
        var csv = new StringBuilder();
        csv.Append(Encabezado).Append("\r\n");

        foreach (var cita in citas)
        {
            medicos.TryGetValue(cita.MedicoId, out var medico);
            pacientes.TryGetValue(cita.PacienteId, out var paciente);
            var celdas = new[]
            {
                cita.Inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cita.Inicio.ToString("HH:mm", CultureInfo.InvariantCulture),
                Texto(paciente?.NombreCompleto ?? "Paciente desconocido"),
                Texto(paciente?.Cedula ?? ""),
                Texto(medico?.Nombre ?? "Médico desconocido"),
                Texto(medico?.Especialidad ?? ""),
                Texto(medico?.Centro ?? ""),
                cita.Estado.ToString(),
                Texto(cita.Motivo)
            };
            csv.Append(string.Join(",", celdas.Select(Celda))).Append("\r\n");
        }

        return csv.ToString();
    }

    private IReadOnlyList<Cita> Consultar(DateTime? desde, DateTime? hasta)
    {
        if (desde is { } d && hasta is { } h && d.Date > h.Date)
        {
            throw new ArgumentException("La fecha inicial no puede ser posterior a la final.");
        }

        return _repositorio.CitasEnRango(desde?.Date, hasta?.Date.AddDays(1));
    }

    private static string Texto(string valor) =>
        valor.Length > 0 && valor[0] is '=' or '+' or '-' or '@' ? "'" + valor : valor;

    private static string Celda(string valor) =>
        valor.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + valor.Replace("\"", "\"\"") + "\"" : valor;
}
