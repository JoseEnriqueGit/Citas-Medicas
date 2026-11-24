using System.Globalization;
using System.Text;
using System.Windows.Input;
using CitasMedicas.App.Services;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed record ReporteMedicoItem(string Nombre, string Detalle, int Total, string Resumen)
{
    public string Descripcion => $"{Nombre}, {Detalle}, {Total} citas. {Resumen}";
}

public sealed record ReporteEspecialidadItem(string Nombre, int Total)
{
    public string Descripcion => $"{Nombre}, {Total} citas";
}

public sealed class ReporteViewModel : ObservableObject
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    private readonly ReporteService _servicio;
    private readonly TimeProvider _reloj;
    private bool _exportando;
    private DateTime? _desde;
    private DateTime? _hasta;
    private string _rango = "";
    private string _generado = "";
    private int _total;
    private int _programadas;
    private int _completadas;
    private int _canceladas;
    private string _tasa = "";
    private int _pacientes;
    private int _medicos;
    private IReadOnlyList<ReporteMedicoItem> _porMedico = Array.Empty<ReporteMedicoItem>();
    private IReadOnlyList<ReporteEspecialidadItem> _porEspecialidad = Array.Empty<ReporteEspecialidadItem>();

    public ReporteViewModel(ReporteService servicio, TimeProvider reloj)
    {
        _servicio = servicio;
        _reloj = reloj;
        Periodos = new List<EspecialidadItem>
        {
            new("Todo", "todo", ElegirPeriodo),
            new("Hoy", "hoy", ElegirPeriodo),
            new("Próximos 7 días", "semana", ElegirPeriodo),
            new("Este mes", "mes", ElegirPeriodo)
        };
        ExportarCommand = new Command(() => _ = ErroresUi.EjecutarAsync(ExportarAsync));
        CerrarCommand = new Command(() => CierreSolicitado?.Invoke());
        Aplicar(Periodos[0]);
    }

    public event Action? CierreSolicitado;

    public IReadOnlyList<EspecialidadItem> Periodos { get; }

    public ICommand ExportarCommand { get; }

    public ICommand CerrarCommand { get; }

    public string Rango
    {
        get => _rango;
        private set => SetProperty(ref _rango, value);
    }

    public string Generado
    {
        get => _generado;
        private set => SetProperty(ref _generado, value);
    }

    public int Total
    {
        get => _total;
        private set
        {
            if (SetProperty(ref _total, value))
            {
                OnPropertyChanged(nameof(SinCitas));
            }
        }
    }

    public bool SinCitas => _total == 0;

    public int Programadas
    {
        get => _programadas;
        private set => SetProperty(ref _programadas, value);
    }

    public int Completadas
    {
        get => _completadas;
        private set => SetProperty(ref _completadas, value);
    }

    public int Canceladas
    {
        get => _canceladas;
        private set => SetProperty(ref _canceladas, value);
    }

    public string Tasa
    {
        get => _tasa;
        private set => SetProperty(ref _tasa, value);
    }

    public int Pacientes
    {
        get => _pacientes;
        private set => SetProperty(ref _pacientes, value);
    }

    public int Medicos
    {
        get => _medicos;
        private set => SetProperty(ref _medicos, value);
    }

    public IReadOnlyList<ReporteMedicoItem> PorMedico
    {
        get => _porMedico;
        private set => SetProperty(ref _porMedico, value);
    }

    public IReadOnlyList<ReporteEspecialidadItem> PorEspecialidad
    {
        get => _porEspecialidad;
        private set
        {
            if (SetProperty(ref _porEspecialidad, value))
            {
                OnPropertyChanged(nameof(SinEspecialidades));
            }
        }
    }

    public bool SinEspecialidades => _porEspecialidad.Count == 0;

    private void ElegirPeriodo(EspecialidadItem item) => Aplicar(item);

    private void Aplicar(EspecialidadItem item)
    {
        var hoy = _reloj.GetLocalNow().Date;
        (_desde, _hasta) = item.Valor switch
        {
            "hoy" => (hoy, hoy),
            "semana" => (hoy, hoy.AddDays(6)),
            "mes" => (new DateTime(hoy.Year, hoy.Month, 1), new DateTime(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1)),
            _ => ((DateTime?)null, (DateTime?)null)
        };

        foreach (var periodo in Periodos)
        {
            periodo.Seleccionada = periodo == item;
        }

        Generar();
    }

    private void Generar()
    {
        var r = _servicio.Generar(_desde, _hasta);
        Rango = FormatearRango(_desde, _hasta);
        Generado = $"Generado el {r.GeneradoEn:dd/MM/yyyy} a las {r.GeneradoEn:HH:mm}";
        Total = r.Total;
        Programadas = r.Programadas;
        Completadas = r.Completadas;
        Canceladas = r.Canceladas;
        Tasa = $"Tasa de cancelación: {Math.Round(r.TasaCancelacion * 100, MidpointRounding.AwayFromZero):0} %";
        Pacientes = r.TotalPacientes;
        Medicos = r.TotalMedicos;
        PorMedico = r.PorMedico
            .Select(m => new ReporteMedicoItem(
                m.Medico.Nombre,
                $"{m.Medico.Especialidad} · {m.Medico.Centro}",
                m.Total,
                $"Programadas {m.Programadas} · Completadas {m.Completadas} · Canceladas {m.Canceladas}"))
            .ToList();
        PorEspecialidad = r.PorEspecialidad.Select(e => new ReporteEspecialidadItem(e.Especialidad, e.Total)).ToList();
    }

    private static string FormatearRango(DateTime? desde, DateTime? hasta)
    {
        if (desde is null || hasta is null)
        {
            return "Todas las fechas";
        }

        var d = desde.Value;
        var h = hasta.Value;
        if (d == h)
        {
            return $"{d.ToString("d 'de' MMMM 'de' yyyy", Cultura).ToLower(Cultura)}";
        }

        if (d.Month == h.Month && d.Year == h.Year)
        {
            return $"Del {d.Day} al {h.ToString("d 'de' MMMM 'de' yyyy", Cultura).ToLower(Cultura)}";
        }

        return $"Del {d.ToString("d 'de' MMMM", Cultura).ToLower(Cultura)} al {h.ToString("d 'de' MMMM 'de' yyyy", Cultura).ToLower(Cultura)}";
    }

    private async Task ExportarAsync()
    {
        if (_exportando)
        {
            return;
        }

        _exportando = true;
        try
        {
            var csv = _servicio.ExportarCsv(_desde, _hasta);
            var ruta = Path.Combine(FileSystem.CacheDirectory, $"reporte-citas-{DateTime.Now:yyyyMMdd-HHmm}.csv");
            await File.WriteAllTextAsync(ruta, csv, new UTF8Encoding(true));
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Reporte de citas",
                File = new ShareFile(ruta)
            });
        }
        finally
        {
            _exportando = false;
        }
    }
}
