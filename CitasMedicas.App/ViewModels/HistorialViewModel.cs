using System.Windows.Input;
using CitasMedicas.App.Services;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed class HistorialViewModel : ObservableObject
{
    private const int MilisegundosAviso = 3000;

    private readonly CitaService _citas;
    private readonly AppState _estado;
    private readonly IReadOnlyList<FiltroHistorialItem> _filtros;
    private IReadOnlyList<ItemHistorial> _todas = Array.Empty<ItemHistorial>();
    private IReadOnlyList<HistorialItem> _items = Array.Empty<HistorialItem>();
    private EstadoCita? _filtro;
    private bool _ocupado;
    private bool _avisoVisible;
    private int _versionAviso;

    public HistorialViewModel(CitaService citas, AppState estado)
    {
        _citas = citas;
        _estado = estado;
        _filtros =
        [
            new("Todas", null, Elegir),
            new("Programadas", EstadoCita.Programada, Elegir),
            new("Completadas", EstadoCita.Completada, Elegir),
            new("Canceladas", EstadoCita.Cancelada, Elegir)
        ];
        _estado.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.Paciente))
            {
                OnPropertyChanged(nameof(SinPaciente));
                OnPropertyChanged(nameof(HayPaciente));
            }
        };
        _estado.PacienteActivoCambio += () =>
        {
            _filtro = null;
            Cargar();
        };
        AccionVaciaCommand = new Command(AccionVacia);
        RegistrarseCommand = new Command(() => _ = ErroresUi.EjecutarAsync(() => Shell.Current.GoToAsync("//paciente")));
    }

    public event Action? AvisoMostrado;

    public Func<IReadOnlyList<DatoItem>, Task<bool>>? ConfirmarAsync { get; set; }

    public AppState Estado => _estado;

    public IReadOnlyList<FiltroHistorialItem> Filtros => _filtros;

    public ICommand AccionVaciaCommand { get; }

    public ICommand RegistrarseCommand { get; }

    public bool SinPaciente => _estado.Paciente is null;

    public bool HayPaciente => !SinPaciente;

    public IReadOnlyList<HistorialItem> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    public bool AvisoVisible
    {
        get => _avisoVisible;
        private set => SetProperty(ref _avisoVisible, value);
    }

    public bool MostrarResumen => _items.Count > 0;

    public string ResumenCantidad => _filtro is null
        ? Cantidad(_items.Count, "cita", "citas")
        : Cantidad(_items.Count, $"cita {Singular(_filtro.Value)}", $"citas {Plural(_filtro.Value)}");

    public string ResumenDetalle
    {
        get
        {
            if (_filtro is not null)
            {
                return "";
            }

            var programadas = _items.Count(i => i.EsProgramada);
            return $" · {Cantidad(programadas, "programada", "programadas")}";
        }
    }

    public string VacioTitulo => _filtro switch
    {
        null => "Aún no tienes citas",
        var estado => $"Sin citas {Plural(estado.Value)}"
    };

    public string VacioTexto => _filtro switch
    {
        null => "Cuando solicites una cita aparecerá aquí, con su estado.",
        var estado => $"No tienes citas {Plural(estado.Value)} por ahora."
    };

    public string VacioAccion => _filtro is null ? "Solicitar cita" : "Ver todas";

    public void Cargar()
    {
        _todas = _estado.Paciente is { } paciente ? _citas.Historial(paciente.Id) : Array.Empty<ItemHistorial>();
        Aplicar();
    }

    private void Elegir(FiltroHistorialItem item)
    {
        _filtro = item.Estado;
        Aplicar();
    }

    private void AccionVacia()
    {
        if (_filtro is null)
        {
            _ = Shell.Current.GoToAsync("//solicitar");
            return;
        }

        _filtro = null;
        Aplicar();
    }

    private void Aplicar()
    {
        Items = _todas
            .Where(i => _filtro is null || i.Cita.Estado == _filtro)
            .Select(i => new HistorialItem(i, Cancelar))
            .ToList();

        foreach (var filtro in _filtros)
        {
            filtro.Seleccionado = filtro.Estado == _filtro;
        }

        OnPropertyChanged(nameof(MostrarResumen));
        OnPropertyChanged(nameof(ResumenCantidad));
        OnPropertyChanged(nameof(ResumenDetalle));
        OnPropertyChanged(nameof(VacioTitulo));
        OnPropertyChanged(nameof(VacioTexto));
        OnPropertyChanged(nameof(VacioAccion));
    }

    private void Cancelar(HistorialItem item) => _ = ErroresUi.EjecutarAsync(() => CancelarAsync(item));

    private async Task CancelarAsync(HistorialItem item)
    {
        if (_ocupado)
        {
            return;
        }

        _ocupado = true;
        try
        {
            var datos = new List<DatoItem>
            {
                new("Médico", item.Nombre),
                new("Fecha", $"{FormatoFecha.DiaLargoMes(item.Cita.Inicio)}, {item.Hora}")
            };

            if (ConfirmarAsync is null || !await ConfirmarAsync(datos))
            {
                return;
            }

            var cancelada = _citas.Cancelar(item.Cita.Id);
            Cargar();
            if (cancelada)
            {
                await MostrarAvisoAsync();
            }
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task MostrarAvisoAsync()
    {
        var version = ++_versionAviso;
        AvisoVisible = true;
        AvisoMostrado?.Invoke();
        await Task.Delay(MilisegundosAviso);
        if (version == _versionAviso)
        {
            AvisoVisible = false;
        }
    }

    private static string Cantidad(int n, string singular, string plural) => n == 1 ? $"1 {singular}" : $"{n} {plural}";

    private static string Singular(EstadoCita estado) => estado switch
    {
        EstadoCita.Programada => "programada",
        EstadoCita.Completada => "completada",
        _ => "cancelada"
    };

    private static string Plural(EstadoCita estado) => Singular(estado) + "s";
}
