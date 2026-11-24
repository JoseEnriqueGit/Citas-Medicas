using System.Windows.Input;
using CitasMedicas.App.Services;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed class RegistroMedicoViewModel : ObservableObject
{
    private readonly MedicoService _medicos;
    private string _nombre = "";
    private string _especialidad = "";
    private string _centro = "";
    private bool _cerrado;
    private IReadOnlyDictionary<CampoMedico, string> _errores = new Dictionary<CampoMedico, string>();

    public RegistroMedicoViewModel(MedicoService medicos)
    {
        _medicos = medicos;
        Especialidades = medicos.Especialidades()
            .Select(n => new EspecialidadItem(n, n, ElegirEspecialidad))
            .ToList();
        MarcarEspecialidades();
        GuardarCommand = new Command(() => ErroresUi.Ejecutar(Guardar));
        CancelarCommand = new Command(Cancelar);
    }

    public event Action<CampoMedico>? FocoSolicitado;

    public event Action<Medico?>? CierreSolicitado;

    public ICommand GuardarCommand { get; }

    public ICommand CancelarCommand { get; }

    public IReadOnlyList<EspecialidadItem> Especialidades { get; }

    public bool HayEspecialidades => Especialidades.Count > 0;

    public string Nombre
    {
        get => _nombre;
        set
        {
            if (SetProperty(ref _nombre, value ?? ""))
            {
                QuitarError(CampoMedico.Nombre);
            }
        }
    }

    public string Especialidad
    {
        get => _especialidad;
        set
        {
            if (SetProperty(ref _especialidad, value ?? ""))
            {
                MarcarEspecialidades();
                QuitarError(CampoMedico.Especialidad);
            }
        }
    }

    public string Centro
    {
        get => _centro;
        set
        {
            if (SetProperty(ref _centro, value ?? ""))
            {
                QuitarError(CampoMedico.Centro);
            }
        }
    }

    public string ErrorNombre => Error(CampoMedico.Nombre);

    public string ErrorEspecialidad => Error(CampoMedico.Especialidad);

    public string ErrorCentro => Error(CampoMedico.Centro);

    public bool HayErrores => _errores.Count > 0;

    public string ResumenErrores => _errores.Count == 1 ? "Revisa 1 campo" : $"Revisa {_errores.Count} campos";

    private IReadOnlyDictionary<CampoMedico, string> Errores
    {
        set
        {
            _errores = value;
            OnPropertyChanged(nameof(ErrorNombre));
            OnPropertyChanged(nameof(ErrorEspecialidad));
            OnPropertyChanged(nameof(ErrorCentro));
            OnPropertyChanged(nameof(HayErrores));
            OnPropertyChanged(nameof(ResumenErrores));
        }
    }

    private void Guardar()
    {
        if (_cerrado)
        {
            return;
        }

        var resultado = _medicos.Registrar(new RegistroMedico(_nombre, _especialidad, _centro));
        Errores = resultado.Errores;
        if (!resultado.Exitoso)
        {
            FocoSolicitado?.Invoke(_errores.Keys.OrderBy(campo => campo).First());
            return;
        }

        _cerrado = true;
        CierreSolicitado?.Invoke(resultado.Medico);
    }

    private void Cancelar()
    {
        if (_cerrado)
        {
            return;
        }

        _cerrado = true;
        CierreSolicitado?.Invoke(null);
    }

    private void ElegirEspecialidad(EspecialidadItem item) => Especialidad = item.Valor ?? "";

    private void MarcarEspecialidades()
    {
        var actual = _especialidad.Trim();
        foreach (var chip in Especialidades)
        {
            chip.Seleccionada = actual.Length > 0 && string.Equals(chip.Valor, actual, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void QuitarError(CampoMedico campo)
    {
        if (_errores.ContainsKey(campo))
        {
            Errores = _errores.Where(par => par.Key != campo).ToDictionary(par => par.Key, par => par.Value);
        }
    }

    private string Error(CampoMedico campo) => _errores.TryGetValue(campo, out var mensaje) ? mensaje : "";
}
