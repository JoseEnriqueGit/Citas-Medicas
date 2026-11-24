using System.Globalization;
using System.Text;
using System.Windows.Input;
using CitasMedicas.App.Services;
using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed class MedicosViewModel : ObservableObject
{
    private const int MilisegundosAviso = 3000;

    private readonly CitasRepository _repositorio;
    private readonly HorarioService _horarios;
    private readonly MedicoService _medicosServicio;
    private readonly AppState _estado;
    private bool _abriendo;
    private bool _avisoVisible;
    private string _textoAviso = "";
    private int _versionAviso;
    private List<MedicoItem> _todos = new();
    private IReadOnlyList<MedicoItem> _medicos = Array.Empty<MedicoItem>();
    private IReadOnlyList<EspecialidadItem> _especialidades = Array.Empty<EspecialidadItem>();
    private string _busqueda = "";
    private string? _especialidad;

    public MedicosViewModel(CitasRepository repositorio, HorarioService horarios, MedicoService medicosServicio, AppState estado)
    {
        _repositorio = repositorio;
        _horarios = horarios;
        _medicosServicio = medicosServicio;
        _estado = estado;
        _estado.MedicosCambiaron += Cargar;
        _estado.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.Paciente))
            {
                OnPropertyChanged(nameof(SinPaciente));
            }
        };
        LimpiarCommand = new Command(Limpiar);
        RegistrarseCommand = new Command(async () => await Shell.Current.GoToAsync("//paciente"));
        AgregarCommand = new Command(() => _ = ErroresUi.EjecutarAsync(AgregarAsync));
    }

    public event Action<string>? AvisoMostrado;

    public Func<RegistroMedicoViewModel, Task<Medico?>>? RegistrarAsync { get; set; }

    public ICommand AgregarCommand { get; }

    public bool AvisoVisible
    {
        get => _avisoVisible;
        private set => SetProperty(ref _avisoVisible, value);
    }

    public string TextoAviso
    {
        get => _textoAviso;
        private set => SetProperty(ref _textoAviso, value);
    }

    public AppState Estado => _estado;

    public ICommand LimpiarCommand { get; }

    public ICommand RegistrarseCommand { get; }

    public bool SinPaciente => _estado.Paciente is null;

    public IReadOnlyList<MedicoItem> Medicos
    {
        get => _medicos;
        private set => SetProperty(ref _medicos, value);
    }

    public IReadOnlyList<EspecialidadItem> Especialidades
    {
        get => _especialidades;
        private set => SetProperty(ref _especialidades, value);
    }

    public string Busqueda
    {
        get => _busqueda;
        set
        {
            if (SetProperty(ref _busqueda, value ?? ""))
            {
                Filtrar();
            }
        }
    }

    public bool HayFiltros => _busqueda.Trim().Length > 0 || _especialidad is not null;

    public bool MostrarResumen => HayFiltros && _medicos.Count > 0;

    public string ResumenCantidad => _medicos.Count == 1 ? "1 médico" : $"{_medicos.Count} médicos";

    public string ResumenDetalle
    {
        get
        {
            var consulta = _busqueda.Trim();
            var partes = new List<string>();
            if (_especialidad is not null)
            {
                partes.Add($" en {_especialidad}");
            }

            if (consulta.Length > 0)
            {
                partes.Add($" para \"{consulta}\"");
            }

            return string.Concat(partes);
        }
    }

    public string SinResultadosTexto
    {
        get
        {
            var consulta = _busqueda.Trim();
            if (consulta.Length > 0 && _especialidad is null)
            {
                return $"No hay médicos para \"{consulta}\". Prueba con otra especialidad o con el nombre del médico.";
            }

            if (consulta.Length > 0)
            {
                return $"No hay médicos para \"{consulta}\" en {_especialidad}. Prueba con otra especialidad o con el nombre del médico.";
            }

            return "No hay médicos para mostrar.";
        }
    }

    public void Cargar()
    {
        _todos = _repositorio.ListarMedicos()
            .Select(m => new MedicoItem(m, _horarios.ProximoHorarioLibre(m.Id), Elegir))
            .ToList();

        var nombres = _todos.Select(m => m.Especialidad).Distinct().ToList();
        if (_especialidad is not null && !nombres.Contains(_especialidad))
        {
            _especialidad = null;
        }

        var chips = new List<EspecialidadItem> { new("Todas", null, ElegirEspecialidad) };
        chips.AddRange(nombres.Select(n => new EspecialidadItem(n, n, ElegirEspecialidad)));
        Especialidades = chips;
        Filtrar();
    }

    private async Task AgregarAsync()
    {
        if (_abriendo || RegistrarAsync is null)
        {
            return;
        }

        _abriendo = true;
        Medico? medico;
        try
        {
            medico = await RegistrarAsync(new RegistroMedicoViewModel(_medicosServicio));
        }
        finally
        {
            _abriendo = false;
        }

        if (medico is not null)
        {
            await MostrarAvisoAsync(medico.Nombre);
        }
    }

    private async Task MostrarAvisoAsync(string nombre)
    {
        var version = ++_versionAviso;
        TextoAviso = $"Médico registrado: {nombre}.";
        AvisoVisible = true;
        AvisoMostrado?.Invoke(TextoAviso);
        await Task.Delay(MilisegundosAviso);
        if (version == _versionAviso)
        {
            AvisoVisible = false;
        }
    }

    private void Elegir(MedicoItem item)
    {
        _estado.Medico = item.Medico;
        _estado.Fecha = null;
        _estado.Horario = null;
        _ = Shell.Current.GoToAsync("//horarios");
    }

    private void ElegirEspecialidad(EspecialidadItem item)
    {
        _especialidad = item.Valor;
        Filtrar();
    }

    private void Limpiar()
    {
        _especialidad = null;
        Busqueda = "";
        Filtrar();
    }

    private void Filtrar()
    {
        var consulta = Normalizar(_busqueda.Trim());
        Medicos = _todos
            .Where(m => _especialidad is null || m.Especialidad == _especialidad)
            .Where(m => consulta.Length == 0
                || Normalizar(m.Nombre).Contains(consulta, StringComparison.Ordinal)
                || Normalizar(m.Especialidad).Contains(consulta, StringComparison.Ordinal))
            .ToList();

        foreach (var chip in Especialidades)
        {
            chip.Seleccionada = chip.Valor == _especialidad;
        }

        OnPropertyChanged(nameof(HayFiltros));
        OnPropertyChanged(nameof(MostrarResumen));
        OnPropertyChanged(nameof(ResumenCantidad));
        OnPropertyChanged(nameof(ResumenDetalle));
        OnPropertyChanged(nameof(SinResultadosTexto));
    }

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var limpio = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                limpio.Append(char.ToLowerInvariant(c));
            }
        }

        return limpio.ToString();
    }
}
