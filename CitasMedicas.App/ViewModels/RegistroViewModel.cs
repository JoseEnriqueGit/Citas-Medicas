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
    private string _contrasena = "";
    private DateTime? _fecha;
    private bool _editando;
    private bool _iniciando;
    private string _cedulaInicio = "";
    private string _contrasenaInicio = "";
    private string _errorInicio = "";
    private IReadOnlyDictionary<CampoPaciente, string> _errores = new Dictionary<CampoPaciente, string>();

    public RegistroViewModel(PacienteService pacientes, AppState estado)
    {
        _pacientes = pacientes;
        _estado = estado;
        _iniciando = estado.Paciente is null && pacientes.Listar().Count > 0;
        _estado.PropertyChanged += (_, _) => NotificarEstado();
        _estado.DatosCambiaron += NotificarEstado;
        RegistrarCommand = new Command(() => ErroresUi.Ejecutar(Registrar));
        EditarCommand = new Command(Editar);
        CancelarEdicionCommand = new Command(CancelarEdicion);
        IniciarCommand = new Command(() => ErroresUi.Ejecutar(Iniciar));
        CerrarSesionCommand = new Command(() => ErroresUi.Ejecutar(CerrarSesion));
        IrAInicioCommand = new Command(() => CambiarModo(true));
        IrARegistroCommand = new Command(() => CambiarModo(false));
        SolicitarCommand = new Command(() => _ = ErroresUi.EjecutarAsync(() => Shell.Current.GoToAsync("//solicitar")));
    }

    public event Action<CampoPaciente>? FocoSolicitado;

    public ICommand RegistrarCommand { get; }

    public ICommand EditarCommand { get; }

    public ICommand CancelarEdicionCommand { get; }

    public ICommand SolicitarCommand { get; }

    public ICommand IniciarCommand { get; }

    public ICommand CerrarSesionCommand { get; }

    public ICommand IrAInicioCommand { get; }

    public ICommand IrARegistroCommand { get; }

    public bool MostrarCancelar => _editando;

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

    public string Contrasena
    {
        get => _contrasena;
        set => SetProperty(ref _contrasena, value);
    }

    public string CedulaInicio
    {
        get => _cedulaInicio;
        set => SetProperty(ref _cedulaInicio, value);
    }

    public string ContrasenaInicio
    {
        get => _contrasenaInicio;
        set => SetProperty(ref _contrasenaInicio, value);
    }

    public string ErrorInicio
    {
        get => _errorInicio;
        private set
        {
            if (SetProperty(ref _errorInicio, value))
            {
                OnPropertyChanged(nameof(HayErrorInicio));
            }
        }
    }

    public bool HayErrorInicio => _errorInicio.Length > 0;

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

    public string ErrorContrasena => Error(CampoPaciente.Contrasena);

    public string ErrorFecha => Error(CampoPaciente.FechaNacimiento);

    public bool HayErrorFecha => ErrorFecha.Length > 0;

    public bool HayErrores => _errores.Count > 0;

    public bool MostrarInfo => MostrarFormulario && !HayErrores && !_editando;

    public string ResumenErrores => _errores.Count == 1 ? "Revisa 1 campo" : $"Revisa {_errores.Count} campos";

    public bool Completado => _estado.Paciente is not null && !_editando;

    public bool MostrarInicio => _estado.Paciente is null && _iniciando;

    public bool MostrarFormulario => !Completado && !MostrarInicio;

    public bool MostrarContrasena => !_editando;

    public bool MostrarIrAInicio => _estado.Paciente is null && !_iniciando;

    public bool Editando => _editando;

    public string Titulo => Completado ? "Mi cuenta" : _editando ? "Editar mis datos" : MostrarInicio ? "Iniciar sesión" : "Registro de paciente";

    public string TextoBoton => _editando ? "Guardar cambios" : "Registrarme";

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

        var datos = new RegistroPaciente(_nombre, _cedula, _telefono, _correo, _fecha, _contrasena);
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
        _estado.Paciente = resultado.Paciente;
        LimpiarFormulario();
        NotificarEstado();
    }

    private void Iniciar()
    {
        if (!MostrarInicio)
        {
            return;
        }

        var resultado = _pacientes.IniciarSesion(_cedulaInicio, _contrasenaInicio);
        ContrasenaInicio = "";
        if (!resultado.Exitoso)
        {
            ErrorInicio = resultado.Error ?? "No se pudo iniciar sesión.";
            return;
        }

        ErrorInicio = "";
        CedulaInicio = "";
        _estado.Paciente = resultado.Paciente;
        NotificarEstado();
    }

    private void CerrarSesion()
    {
        _editando = false;
        _iniciando = true;
        ErrorInicio = "";
        LimpiarFormulario();
        _pacientes.CerrarSesion();
        NotificarEstado();
    }

    private void CambiarModo(bool iniciando)
    {
        _iniciando = iniciando;
        _errores = new Dictionary<CampoPaciente, string>();
        ErrorInicio = "";
        Contrasena = "";
        ContrasenaInicio = "";
        NotificarErrores();
        NotificarEstado();
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

    private void CancelarEdicion()
    {
        _editando = false;
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
        Contrasena = "";
        FechaNacimiento = null;
    }

    private string Error(CampoPaciente campo) => _errores.TryGetValue(campo, out var mensaje) ? mensaje : "";

    private void NotificarErrores()
    {
        OnPropertyChanged(nameof(ErrorNombre));
        OnPropertyChanged(nameof(ErrorCedula));
        OnPropertyChanged(nameof(ErrorTelefono));
        OnPropertyChanged(nameof(ErrorCorreo));
        OnPropertyChanged(nameof(ErrorContrasena));
        OnPropertyChanged(nameof(ErrorFecha));
        OnPropertyChanged(nameof(HayErrorFecha));
        OnPropertyChanged(nameof(HayErrores));
        OnPropertyChanged(nameof(ResumenErrores));
        OnPropertyChanged(nameof(MostrarInfo));
    }

    private void NotificarEstado()
    {
        OnPropertyChanged(nameof(Completado));
        OnPropertyChanged(nameof(MostrarInicio));
        OnPropertyChanged(nameof(MostrarFormulario));
        OnPropertyChanged(nameof(MostrarContrasena));
        OnPropertyChanged(nameof(MostrarIrAInicio));
        OnPropertyChanged(nameof(Editando));
        OnPropertyChanged(nameof(MostrarCancelar));
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
