using System.Collections.ObjectModel;
using System.Windows.Input;
using CitasMedicas.App.Services;
using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed class SolicitarViewModel : ObservableObject
{
    private const int DiasVisibles = 10;

    private readonly CitaService _citas;
    private readonly HorarioService _horarios;
    private readonly CitasRepository _repositorio;
    private readonly AppState _estado;
    private string _motivo = "";
    private bool _cambiandoMedico;
    private bool _tomado;
    private bool _ocupado;
    private int _libres;
    private IReadOnlyList<DatoItem>? _datosProgramada;
    private IReadOnlyList<BloqueItem> _bloques = Array.Empty<BloqueItem>();
    private IReadOnlyDictionary<CampoSolicitud, string> _errores = new Dictionary<CampoSolicitud, string>();

    public SolicitarViewModel(CitaService citas, HorarioService horarios, CitasRepository repositorio, AppState estado)
    {
        _citas = citas;
        _horarios = horarios;
        _repositorio = repositorio;
        _estado = estado;
        RevisarCommand = new Command(async () => await RevisarAsync());
        CambiarMedicoCommand = new Command(() =>
        {
            _cambiandoMedico = true;
            NotificarMedico();
        });
        NuevaCommand = new Command(Reiniciar);
        HistorialCommand = new Command(async () =>
        {
            await Shell.Current.GoToAsync("//historial");
            Reiniciar();
        });
        IrAPacienteCommand = new Command(async () => await Shell.Current.GoToAsync("//paciente"));
    }

    public event Action<int>? FechaVisible;

    public event Action? AvisoMostrado;

    public Func<IReadOnlyList<DatoItem>, Task<bool>>? ConfirmarAsync { get; set; }

    public AppState Estado => _estado;

    public ObservableCollection<MedicoOpcion> Medicos { get; } = new();

    public ObservableCollection<FechaItem> Fechas { get; } = new();

    public ICommand RevisarCommand { get; }

    public ICommand CambiarMedicoCommand { get; }

    public ICommand NuevaCommand { get; }

    public ICommand HistorialCommand { get; }

    public ICommand IrAPacienteCommand { get; }

    public int MaximoMotivo => CitaService.MaximoMotivo;

    public IReadOnlyList<BloqueItem> Bloques
    {
        get => _bloques;
        private set => SetProperty(ref _bloques, value);
    }

    public IReadOnlyList<DatoItem> DatosProgramada => _datosProgramada ?? Array.Empty<DatoItem>();

    public string Motivo
    {
        get => _motivo;
        set
        {
            if (SetProperty(ref _motivo, value ?? ""))
            {
                OnPropertyChanged(nameof(ContadorMotivo));
                if (_errores.ContainsKey(CampoSolicitud.Motivo))
                {
                    Errores = _errores.Where(e => e.Key != CampoSolicitud.Motivo).ToDictionary(e => e.Key, e => e.Value);
                }
            }
        }
    }

    public string ContadorMotivo => $"{_motivo.Length}/{CitaService.MaximoMotivo}";

    public bool SinPaciente => _estado.Paciente is null;

    public bool MostrarFormulario => !SinPaciente && _datosProgramada is null;

    public bool MostrarProgramada => !SinPaciente && _datosProgramada is not null;

    public string Titulo => MostrarProgramada ? "Cita programada" : "Solicitar cita";

    public bool TieneMedico => _estado.Medico is not null;

    public bool ListaMedicosVisible => !TieneMedico || _cambiandoMedico;

    public bool TarjetaMedicoVisible => TieneMedico && !_cambiandoMedico;

    public string MedicoIniciales => MedicoItem.InicialesDe(_estado.Medico?.Nombre ?? "");

    public string MedicoNombre => _estado.Medico?.Nombre ?? "";

    public string MedicoDetalle => _estado.Medico is { } m ? $"{m.Especialidad} · {m.Centro}" : "";

    public bool PuedeVerBloques => TieneMedico && _estado.Fecha is not null;

    public bool SinSeleccionParaBloques => !PuedeVerBloques;

    public bool HayBloques => PuedeVerBloques && _bloques.Count > 0;

    public bool SinLibres => PuedeVerBloques && _libres == 0;

    public bool Tomado => _tomado;

    public bool HayErrores => !_tomado && _errores.Count > 0;

    public string ResumenErrores => _errores.Count == 1 ? "Revisa 1 campo" : $"Revisa {_errores.Count} campos";

    public string TextoAviso => _tomado
        ? "Ese horario ya fue tomado. Elige otro de los libres. Tus datos y el motivo se conservaron."
        : $"{ResumenErrores} antes de continuar.";

    public string ErrorMedico => Error(CampoSolicitud.Medico);

    public string ErrorHorario => Error(CampoSolicitud.Horario);

    public string ErrorMotivo => Error(CampoSolicitud.Motivo);

    public bool HayErrorMotivo => ErrorMotivo.Length > 0;

    private IReadOnlyDictionary<CampoSolicitud, string> Errores
    {
        set
        {
            _errores = value;
            OnPropertyChanged(nameof(HayErrores));
            OnPropertyChanged(nameof(ResumenErrores));
            OnPropertyChanged(nameof(TextoAviso));
            OnPropertyChanged(nameof(ErrorMedico));
            OnPropertyChanged(nameof(ErrorHorario));
            OnPropertyChanged(nameof(ErrorMotivo));
            OnPropertyChanged(nameof(HayErrorMotivo));
        }
    }

    public void Actualizar()
    {
        if (Medicos.Count == 0)
        {
            foreach (var medico in _repositorio.ListarMedicos())
            {
                Medicos.Add(new MedicoOpcion(medico, ElegirMedico));
            }
        }

        if (_datosProgramada is not null && _estado.Horario is not null)
        {
            _datosProgramada = null;
            _motivo = "";
            _tomado = false;
            Errores = new Dictionary<CampoSolicitud, string>();
        }

        var hoy = _horarios.Ahora.Date;
        if (_estado.Fecha is null && _estado.Horario is { } horario)
        {
            _estado.Fecha = horario.Date;
        }

        if (_estado.Fecha is { } fecha && (fecha.Date < hoy || !HorarioService.EsDiaHabil(fecha)))
        {
            _estado.Fecha = null;
            _estado.Horario = null;
        }

        ReconstruirFechas();
        RecargarBloques();
        NotificarTodo();
    }

    private void ReconstruirFechas()
    {
        var hoy = _horarios.Ahora.Date;
        Fechas.Clear();
        foreach (var item in FechaItem.Proximas(hoy, _estado.Fecha?.Date ?? hoy, DiasVisibles, SeleccionarFecha))
        {
            Fechas.Add(item);
        }

        MarcarFechaSeleccionada();
    }

    private void MarcarFechaSeleccionada()
    {
        for (var i = 0; i < Fechas.Count; i++)
        {
            Fechas[i].Seleccionada = _estado.Fecha?.Date == Fechas[i].Fecha;
            if (Fechas[i].Seleccionada)
            {
                FechaVisible?.Invoke(i);
            }
        }
    }

    private void RecargarBloques()
    {
        _libres = 0;
        if (_estado.Medico is not { } medico || _estado.Fecha is not { } fecha)
        {
            Bloques = Array.Empty<BloqueItem>();
            return;
        }

        var bloques = _horarios.Bloques(medico.Id, fecha);
        if (_estado.Horario is { } actual && !bloques.Any(b => b.Inicio == actual && b.Libre))
        {
            _estado.Horario = null;
        }

        _libres = bloques.Count(b => b.Libre);
        Bloques = bloques.Select(b => new BloqueItem(b, b.Inicio == _estado.Horario, ElegirBloque)).ToList();
    }

    private void ElegirMedico(MedicoOpcion opcion)
    {
        _estado.Medico = opcion.Medico;
        _cambiandoMedico = false;
        _tomado = false;
        QuitarError(CampoSolicitud.Medico);
        RecargarBloques();
        NotificarTodo();
    }

    private void SeleccionarFecha(FechaItem item)
    {
        if (_estado.Fecha?.Date != item.Fecha)
        {
            _estado.Horario = null;
        }

        _estado.Fecha = item.Fecha;
        _tomado = false;
        MarcarFechaSeleccionada();
        RecargarBloques();
        NotificarTodo();
    }

    private void ElegirBloque(BloqueItem item)
    {
        _estado.Horario = item.Inicio;
        _tomado = false;
        QuitarError(CampoSolicitud.Horario);
        RecargarBloques();
        NotificarTodo();
    }

    private void QuitarError(CampoSolicitud campo)
    {
        if (_errores.ContainsKey(campo))
        {
            Errores = _errores.Where(e => e.Key != campo).ToDictionary(e => e.Key, e => e.Value);
        }
    }

    private Dictionary<CampoSolicitud, string> Validar()
    {
        var errores = new Dictionary<CampoSolicitud, string>();
        if (_estado.Medico is null)
        {
            errores[CampoSolicitud.Medico] = "Elige un médico.";
        }

        if (_estado.Horario is null)
        {
            errores[CampoSolicitud.Horario] = "Elige un horario.";
        }

        if (_motivo.Trim().Length == 0)
        {
            errores[CampoSolicitud.Motivo] = "Escribe el motivo de la consulta.";
        }

        return errores;
    }

    private async Task RevisarAsync()
    {
        if (_ocupado)
        {
            return;
        }

        _ocupado = true;
        try
        {
            _tomado = false;
            var errores = Validar();
            Errores = errores;
            NotificarTodo();
            if (errores.Count > 0)
            {
                AvisoMostrado?.Invoke();
                return;
            }

            var medico = _estado.Medico!;
            var inicio = _estado.Horario!.Value;
            if (ConfirmarAsync is null || !await ConfirmarAsync(DatosConfirmacion(medico, inicio)))
            {
                return;
            }

            var resultado = _citas.Solicitar(_estado.Paciente?.Id, new SolicitudCita(medico.Id, inicio, _motivo));
            Aplicar(resultado, medico);
        }
        finally
        {
            _ocupado = false;
        }
    }

    private void Aplicar(ResultadoSolicitud resultado, Medico medico)
    {
        if (resultado.Exitoso)
        {
            _datosProgramada = DatosProgramadaDe(resultado.Cita!, medico);
            Errores = new Dictionary<CampoSolicitud, string>();
            _estado.Horario = null;
        }
        else if (resultado.HorarioTomado)
        {
            _tomado = true;
            Errores = new Dictionary<CampoSolicitud, string>();
            _estado.Horario = null;
        }
        else
        {
            Errores = resultado.Errores;
        }

        RecargarBloques();
        NotificarTodo();
        if (!resultado.Exitoso)
        {
            AvisoMostrado?.Invoke();
        }
    }

    private void Reiniciar()
    {
        _datosProgramada = null;
        _motivo = "";
        _tomado = false;
        _cambiandoMedico = false;
        Errores = new Dictionary<CampoSolicitud, string>();
        _estado.Medico = null;
        _estado.Fecha = null;
        _estado.Horario = null;
        ReconstruirFechas();
        RecargarBloques();
        NotificarTodo();
    }

    private IReadOnlyList<DatoItem> DatosConfirmacion(Medico medico, DateTime inicio) =>
    [
        new("Paciente", _estado.Paciente?.NombreCompleto ?? ""),
        new("Médico", $"{medico.Nombre}, {medico.Especialidad}"),
        new("Centro", medico.Centro),
        new("Fecha", FechaCompleta(inicio)),
        new("Hora", FormatoFecha.Hora(inicio)),
        new("Motivo", _motivo.Trim())
    ];

    private static IReadOnlyList<DatoItem> DatosProgramadaDe(Cita cita, Medico medico) =>
    [
        new("Cita", $"#{cita.Id}"),
        new("Médico", $"{medico.Nombre}, {medico.Especialidad}"),
        new("Centro", medico.Centro),
        new("Fecha", FechaCompleta(cita.Inicio)),
        new("Hora", FormatoFecha.Hora(cita.Inicio)),
        new("Estado", "Programada", "ic_calendar_primary")
    ];

    private static string FechaCompleta(DateTime fecha) => $"{FormatoFecha.DiaLargoMes(fecha)} de {fecha.Year}";

    private string Error(CampoSolicitud campo) => _errores.TryGetValue(campo, out var mensaje) ? mensaje : "";

    private void NotificarMedico()
    {
        OnPropertyChanged(nameof(ListaMedicosVisible));
        OnPropertyChanged(nameof(TarjetaMedicoVisible));
    }

    private void NotificarTodo()
    {
        foreach (var opcion in Medicos)
        {
            opcion.Seleccionado = opcion.Medico.Id == _estado.Medico?.Id;
        }

        NotificarMedico();
        OnPropertyChanged(nameof(SinPaciente));
        OnPropertyChanged(nameof(MostrarFormulario));
        OnPropertyChanged(nameof(MostrarProgramada));
        OnPropertyChanged(nameof(Titulo));
        OnPropertyChanged(nameof(DatosProgramada));
        OnPropertyChanged(nameof(Motivo));
        OnPropertyChanged(nameof(ContadorMotivo));
        OnPropertyChanged(nameof(TieneMedico));
        OnPropertyChanged(nameof(MedicoIniciales));
        OnPropertyChanged(nameof(MedicoNombre));
        OnPropertyChanged(nameof(MedicoDetalle));
        OnPropertyChanged(nameof(PuedeVerBloques));
        OnPropertyChanged(nameof(SinSeleccionParaBloques));
        OnPropertyChanged(nameof(HayBloques));
        OnPropertyChanged(nameof(SinLibres));
        OnPropertyChanged(nameof(Tomado));
        OnPropertyChanged(nameof(HayErrores));
        OnPropertyChanged(nameof(TextoAviso));
    }
}
