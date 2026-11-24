using System.Globalization;
using System.Text;
using System.Windows.Input;
using CitasMedicas.App.Services;
using CitasMedicas.Core.Data;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed class MedicosViewModel : ObservableObject
{
    private readonly CitasRepository _repositorio;
    private readonly HorarioService _horarios;
    private readonly AppState _estado;
    private List<MedicoItem> _todos = new();
    private IReadOnlyList<MedicoItem> _medicos = Array.Empty<MedicoItem>();
    private IReadOnlyList<EspecialidadItem> _especialidades = Array.Empty<EspecialidadItem>();
    private string _busqueda = "";
    private string? _especialidad;

    public MedicosViewModel(CitasRepository repositorio, HorarioService horarios, AppState estado)
    {
        _repositorio = repositorio;
        _horarios = horarios;
        _estado = estado;
        _estado.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.Paciente))
            {
                OnPropertyChanged(nameof(SinPaciente));
            }
        };
        LimpiarCommand = new Command(Limpiar);
        RegistrarseCommand = new Command(async () => await Shell.Current.GoToAsync("//paciente"));
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
