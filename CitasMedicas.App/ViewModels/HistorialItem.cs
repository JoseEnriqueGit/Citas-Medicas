using System.Windows.Input;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed class HistorialItem
{
    public HistorialItem(ItemHistorial item, Action<HistorialItem> alCancelar)
    {
        Cita = item.Cita;
        Medico = item.Medico;
        var recursos = Application.Current!.Resources;
        (EstadoTexto, EstadoIcono, EstadoFondo, EstadoTinta) = item.Cita.Estado switch
        {
            EstadoCita.Programada => ("Programada", "ic_calendar_primary", (Color)recursos["PrimaryTint"], (Color)recursos["PrimaryDark"]),
            EstadoCita.Completada => ("Completada", "ic_check_success", (Color)recursos["SuccessTint"], (Color)recursos["Success"]),
            _ => ("Cancelada", "ic_x_muted", (Color)recursos["Disabled"], (Color)recursos["Muted"])
        };
        Descripcion = $"Cita con {Medico.Nombre}, {FormatoFecha.DiaCorto(Cita.Inicio)} {Cita.Inicio.Day} {FormatoFecha.MesCorto(Cita.Inicio)} {Hora}, {EstadoTexto}";
        CancelarCommand = new Command(() => alCancelar(this));
    }

    public Cita Cita { get; }

    public Medico Medico { get; }

    public string MesCorto => $"{FormatoFecha.DiaCorto(Cita.Inicio)} {FormatoFecha.MesCorto(Cita.Inicio)}";

    public string Dia => Cita.Inicio.Day.ToString();

    public string Hora => FormatoFecha.Hora(Cita.Inicio);

    public string Nombre => Medico.Nombre;

    public string EspecialidadCentro => $"{Medico.Especialidad} · {Medico.Centro}";

    public string Motivo => Cita.Motivo;

    public string EstadoTexto { get; }

    public string EstadoIcono { get; }

    public Color EstadoFondo { get; }

    public Color EstadoTinta { get; }

    public bool EsProgramada => Cita.Estado == EstadoCita.Programada;

    public string Descripcion { get; }

    public string DescripcionCancelar => $"Cancelar cita con {Medico.Nombre}, {FormatoFecha.DiaCorto(Cita.Inicio)} {Cita.Inicio.Day} {FormatoFecha.MesCorto(Cita.Inicio)} {Hora}";

    public ICommand CancelarCommand { get; }
}

public sealed class FiltroHistorialItem : ObservableObject
{
    private bool _seleccionado;

    public FiltroHistorialItem(string nombre, EstadoCita? estado, Action<FiltroHistorialItem> alElegir)
    {
        Nombre = nombre;
        Estado = estado;
        ElegirCommand = new Command(() => alElegir(this));
    }

    public string Nombre { get; }

    public EstadoCita? Estado { get; }

    public ICommand ElegirCommand { get; }

    public bool Seleccionado
    {
        get => _seleccionado;
        set => SetProperty(ref _seleccionado, value);
    }
}
