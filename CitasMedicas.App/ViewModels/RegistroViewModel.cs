using System.Globalization;
using System.Windows.Input;
using CitasMedicas.App.Services;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed class RegistroViewModel : ObservableObject
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    private readonly PacienteService _pacientes;
    private readonly AppState _estado;
    private string _nombre = "";
    private string _cedula = "";
    private string _telefono = "";
    private string _correo = "";
    private DateTime? _fecha;
    private bool _editando;
    private bool _otro;
    private IReadOnlyList<PacienteItem> _lista = Array.Empty<PacienteItem>();
    private IReadOnlyDictionary<CampoPaciente, string> _errores = new Dictionary<CampoPaciente, string>();

    public RegistroViewModel(PacienteService pacientes, AppState estado)
    {
        _pacientes = pacientes;
        _estado = estado;
        _estado.PropertyChanged += (_, _) => NotificarEstado();
        _estado.DatosCambiaron += Refrescar;
        _lista = ConstruirLista();
        RegistrarCommand = new Command(() => ErroresUi.Ejecutar(Registrar));
        EditarCommand = new Command(Editar);
        CancelarEdicionCommand = new Command(CancelarEdicion);
        OtroCommand = new Command(() => ErroresUi.Ejecutar(EntrarOtro));
        SolicitarCommand = new Command(() => _ = ErroresUi.EjecutarAsync(() => Shell.Current.GoToAsync("//solicitar")));
    }

    public event Action<CampoPaciente>? FocoSolicitado;

    public ICommand RegistrarCommand { get; }

    public ICommand EditarCommand { get; }

    public ICommand CancelarEdicionCommand { get; }

    public ICommand SolicitarCommand { get; }

    public ICommand OtroCommand { get; }

    public IReadOnlyList<PacienteItem> Pacientes
    {
        get => _lista;
        private set => SetProperty(ref _lista, value);
    }

    public bool MostrarLista => Completado && _lista.Count >= 2;

    public bool Otro => _otro;

    public bool MostrarCancelar => _editando || _otro;

    public AppState Estado => _estado;

    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    public string Cedula
    {
        get => _cedula;
        set => SetProperty(ref _cedula, value);
    }

    public string Telefono
    {
        get => _telefono;
        set => SetProperty(ref _telefono, value);
    }

    public string Correo
    {
        get => _correo;
        set => SetProperty(ref _correo, value);
    }

    public DateTime? FechaNacimiento
    {
        get => _fecha;
        set
        {
            if (SetProperty(ref _fecha, value))
            {
                OnPropertyChanged(nameof(FechaTexto));
                OnPropertyChanged(nameof(TieneFecha));
            }
        }
    }

    public bool TieneFecha => _fecha is not null;

    public string FechaTexto => _fecha is null ? "dd/mm/aaaa" : _fecha.Value.ToString("dd/MM/yyyy", Cultura);

    public string ErrorNombre => Error(CampoPaciente.NombreCompleto);

    public string ErrorCedula => Error(CampoPaciente.Cedula);

    public string ErrorTelefono => Error(CampoPaciente.Telefono);

    public string ErrorCorreo => Error(CampoPaciente.Correo);

    public string ErrorFecha => Error(CampoPaciente.FechaNacimiento);

    public bool HayErrorFecha => ErrorFecha.Length > 0;

    public bool HayErrores => _errores.Count > 0;

    public bool MostrarInfo => MostrarFormulario && !HayErrores && !_editando && !_otro;

    public string ResumenErrores => _errores.Count == 1 ? "Revisa 1 campo" : $"Revisa {_errores.Count} campos";

    public bool Completado => _estado.Paciente is not null && !_editando && !_otro;

    public bool MostrarFormulario => !Completado;

    public bool Editando => _editando;

    public string Titulo => Completado ? "Mi registro" : _editando ? "Editar mis datos" : _otro ? "Registrar otro paciente" : "Registro de paciente";

    public string TextoBoton => _editando ? "Guardar cambios" : _otro ? "Registrar paciente" : "Registrarme";

    public string DatoNombre => _estado.Paciente?.NombreCompleto ?? "";

    public string DatoCedula => _estado.Paciente?.Cedula ?? "";

    public string DatoTelefono => _estado.Paciente?.Telefono ?? "";

    public string DatoCorreo => string.IsNullOrEmpty(_estado.Paciente?.Correo) ? "No indicado" : _estado.Paciente!.Correo!;

    public string DatoNacimiento => _estado.Paciente?.FechaNacimiento.ToString("dd/MM/yyyy", Cultura) ?? "";

    public void Registrar()
    {
        if (!MostrarFormulario)
        {
            return;
        }

        var datos = new RegistroPaciente(_nombre, _cedula, _telefono, _correo, _fecha);
        var resultado = _editando && _estado.Paciente is { } actual
            ? _pacientes.Actualizar(actual.Id, datos)
            : _pacientes.Registrar(datos);

        _errores = resultado.Errores;
        NotificarErrores();

        if (!resultado.Exitoso)
        {
            var primero = _errores.Keys.OrderBy(k => k).First();
            FocoSolicitado?.Invoke(primero);
            return;
        }

        _editando = false;
        _otro = false;
        _estado.Paciente = resultado.Paciente;
        LimpiarFormulario();
        Refrescar();
    }

    private void Editar()
    {
        if (_estado.Paciente is not { } paciente)
        {
            return;
        }

        _editando = true;
        _errores = new Dictionary<CampoPaciente, string>();
        Nombre = paciente.NombreCompleto;
        Cedula = paciente.Cedula;
        Telefono = paciente.Telefono;
        Correo = paciente.Correo ?? "";
        FechaNacimiento = paciente.FechaNacimiento;
        NotificarErrores();
        NotificarEstado();
    }

    private void EntrarOtro()
    {
        _editando = false;
        _otro = true;
        _errores = new Dictionary<CampoPaciente, string>();
        LimpiarFormulario();
        NotificarErrores();
        NotificarEstado();
    }

    private void Refrescar()
    {
        Pacientes = ConstruirLista();
        NotificarEstado();
    }

    private IReadOnlyList<PacienteItem> ConstruirLista()
    {
        var activo = _estado.Paciente?.Id;
        return _pacientes.Listar().Select(p => new PacienteItem(p, p.Id == activo, Cambiar)).ToList();
    }

    private void Cambiar(PacienteItem item) => ErroresUi.Ejecutar(() => _pacientes.Activar(item.Paciente.Id));

    private void CancelarEdicion()
    {
        _editando = false;
        _otro = false;
        _errores = new Dictionary<CampoPaciente, string>();
        LimpiarFormulario();
        NotificarErrores();
        NotificarEstado();
    }

    private void LimpiarFormulario()
    {
        Nombre = "";
        Cedula = "";
        Telefono = "";
        Correo = "";
        FechaNacimiento = null;
    }

    private string Error(CampoPaciente campo) => _errores.TryGetValue(campo, out var mensaje) ? mensaje : "";

    private void NotificarErrores()
    {
        OnPropertyChanged(nameof(ErrorNombre));
        OnPropertyChanged(nameof(ErrorCedula));
        OnPropertyChanged(nameof(ErrorTelefono));
        OnPropertyChanged(nameof(ErrorCorreo));
        OnPropertyChanged(nameof(ErrorFecha));
        OnPropertyChanged(nameof(HayErrorFecha));
        OnPropertyChanged(nameof(HayErrores));
        OnPropertyChanged(nameof(ResumenErrores));
        OnPropertyChanged(nameof(MostrarInfo));
    }

    private void NotificarEstado()
    {
        OnPropertyChanged(nameof(Completado));
        OnPropertyChanged(nameof(MostrarFormulario));
        OnPropertyChanged(nameof(Editando));
        OnPropertyChanged(nameof(Otro));
        OnPropertyChanged(nameof(MostrarCancelar));
        OnPropertyChanged(nameof(MostrarLista));
        OnPropertyChanged(nameof(Titulo));
        OnPropertyChanged(nameof(TextoBoton));
        OnPropertyChanged(nameof(MostrarInfo));
        OnPropertyChanged(nameof(DatoNombre));
        OnPropertyChanged(nameof(DatoCedula));
        OnPropertyChanged(nameof(DatoTelefono));
        OnPropertyChanged(nameof(DatoCorreo));
        OnPropertyChanged(nameof(DatoNacimiento));
    }
}
