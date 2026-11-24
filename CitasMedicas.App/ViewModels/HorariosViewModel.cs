using System.Collections.ObjectModel;
using System.Windows.Input;
using CitasMedicas.App.Services;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed class HorariosViewModel : ObservableObject
{
    private const int DiasVisibles = 10;
    private const int TotalBloques = 16;

    private readonly HorarioService _horarios;
    private readonly AppState _estado;
    private int? _medicoCargado;
    private DateTime _fecha;
    private DateTime? _elegido;
    private DateTime? _proximoDia;
    private DateTime? _proximaHora;
    private int _libres;
    private IReadOnlyList<BloqueItem> _bloques = Array.Empty<BloqueItem>();

    public HorariosViewModel(HorarioService horarios, AppState estado)
    {
        _horarios = horarios;
        _estado = estado;
        _estado.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.Medico))
            {
                OnPropertyChanged(nameof(SinMedico));
                OnPropertyChanged(nameof(TieneMedico));
            }
        };
        IrAMedicosCommand = new Command(async () => await Shell.Current.GoToAsync("//medicos"));
        IrAProximoDiaCommand = new Command(IrAProximoDia);
        SolicitarCommand = new Command(Solicitar);
    }

    public event Action<int>? FechaVisible;

    public AppState Estado => _estado;

    public ObservableCollection<FechaItem> Fechas { get; } = new();

    public ICommand IrAMedicosCommand { get; }

    public ICommand IrAProximoDiaCommand { get; }

    public ICommand SolicitarCommand { get; }

    public IReadOnlyList<BloqueItem> Bloques
    {
        get => _bloques;
        private set => SetProperty(ref _bloques, value);
    }

    public bool TieneMedico => _estado.Medico is not null;

    public bool SinMedico => _estado.Medico is null;

    public string MedicoIniciales => MedicoItem.InicialesDe(_estado.Medico?.Nombre ?? "");

    public string MedicoNombre => _estado.Medico?.Nombre ?? "";

    public string MedicoDetalle => _estado.Medico is { } m ? $"{m.Especialidad} · {m.Centro}" : "";

    public string ResumenFecha => FormatoFecha.DiaLargoMes(_fecha);

    public string ResumenLibres => $" · {_libres} libres de {TotalBloques}";

    public bool DiaLleno => TieneMedico && _libres == 0;

    public bool HayBloques => _libres > 0;

    public bool HayProximo => DiaLleno && _proximoDia is not null;

    public bool SinProximo => DiaLleno && _proximoDia is null;

    public string TextoLleno => _proximoDia is { } dia && _proximaHora is { } hora
        ? $" El próximo libre es el {FormatoFecha.DiaCorto(dia).ToLowerInvariant()} {dia.Day} de {FormatoFecha.Mes(dia)} a las {FormatoFecha.Hora(hora)}."
        : " No hay horarios libres en los próximos 60 días.";

    public string TextoIrProximo => _proximoDia is { } dia
        ? $"Ir al {FormatoFecha.DiaLargo(dia)} {dia.Day}"
        : "";

    public bool HayElegido => _elegido is not null;

    public string ElegidoTitulo => _elegido is { } e ? FormatoFecha.DiaMesHora(e) : "";

    public string ElegidoMedico => MedicoNombre;

    public void Recargar()
    {
        var medico = _estado.Medico;
        if (medico?.Id != _medicoCargado)
        {
            _elegido = null;
            _medicoCargado = medico?.Id;
        }

        var hoy = _horarios.Ahora.Date;
        if (_fecha < hoy || !HorarioService.EsDiaHabil(_fecha))
        {
            _fecha = SiguienteHabil(hoy);
        }

        ConstruirFechas(hoy, _fecha);
        ActualizarSeleccion();
        OnPropertyChanged(nameof(TieneMedico));
        OnPropertyChanged(nameof(SinMedico));
        OnPropertyChanged(nameof(MedicoIniciales));
        OnPropertyChanged(nameof(MedicoNombre));
        OnPropertyChanged(nameof(MedicoDetalle));
        OnPropertyChanged(nameof(ElegidoMedico));
    }

    private static DateTime SiguienteHabil(DateTime dia)
    {
        while (!HorarioService.EsDiaHabil(dia))
        {
            dia = dia.AddDays(1);
        }

        return dia;
    }

    private void ConstruirFechas(DateTime hoy, DateTime hasta)
    {
        Fechas.Clear();
        foreach (var fecha in FechaItem.Proximas(hoy, hasta, DiasVisibles, SeleccionarFecha))
        {
            Fechas.Add(fecha);
        }
    }

    private void SeleccionarFecha(FechaItem item)
    {
        if (item.Fecha != _fecha)
        {
            _elegido = null;
        }

        _fecha = item.Fecha;
        ActualizarSeleccion();
    }

    private void ActualizarSeleccion()
    {
        for (var i = 0; i < Fechas.Count; i++)
        {
            Fechas[i].Seleccionada = Fechas[i].Fecha == _fecha;
            if (Fechas[i].Seleccionada)
            {
                FechaVisible?.Invoke(i);
            }
        }

        ActualizarBloques();
    }

    private void ActualizarBloques()
    {
        _proximoDia = null;
        _proximaHora = null;
        if (_estado.Medico is not { } medico)
        {
            _libres = 0;
            _elegido = null;
            Bloques = Array.Empty<BloqueItem>();
            NotificarDia();
            return;
        }

        var bloques = _horarios.Bloques(medico.Id, _fecha);
        if (_elegido is { } actual && !bloques.Any(b => b.Inicio == actual && b.Libre))
        {
            _elegido = null;
        }

        _libres = bloques.Count(b => b.Libre);
        if (_libres == 0)
        {
            _proximoDia = _horarios.ProximoDiaConHueco(medico.Id, _fecha);
            _proximaHora = _proximoDia is { } dia
                ? _horarios.Bloques(medico.Id, dia).First(b => b.Libre).Inicio
                : null;
            Bloques = Array.Empty<BloqueItem>();
        }
        else
        {
            Bloques = bloques.Select(b => new BloqueItem(b, b.Inicio == _elegido, ElegirBloque)).ToList();
        }

        NotificarDia();
    }

    private void ElegirBloque(BloqueItem item)
    {
        _elegido = _elegido == item.Inicio ? null : item.Inicio;
        ActualizarBloques();
    }

    private void IrAProximoDia()
    {
        if (_proximoDia is not { } dia)
        {
            return;
        }

        _elegido = null;
        _fecha = dia;
        ConstruirFechas(_horarios.Ahora.Date, _fecha);
        ActualizarSeleccion();
    }

    private void Solicitar()
    {
        if (_elegido is not { } horario)
        {
            return;
        }

        _estado.Fecha = horario.Date;
        _estado.Horario = horario;
        _ = Shell.Current.GoToAsync("//solicitar");
    }

    private void NotificarDia()
    {
        OnPropertyChanged(nameof(ResumenFecha));
        OnPropertyChanged(nameof(ResumenLibres));
        OnPropertyChanged(nameof(DiaLleno));
        OnPropertyChanged(nameof(HayBloques));
        OnPropertyChanged(nameof(HayProximo));
        OnPropertyChanged(nameof(SinProximo));
        OnPropertyChanged(nameof(TextoLleno));
        OnPropertyChanged(nameof(TextoIrProximo));
        OnPropertyChanged(nameof(HayElegido));
        OnPropertyChanged(nameof(ElegidoTitulo));
    }
}
