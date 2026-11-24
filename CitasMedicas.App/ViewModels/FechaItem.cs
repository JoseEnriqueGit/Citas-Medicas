using System.Globalization;
using System.Windows.Input;

namespace CitasMedicas.App.ViewModels;

public sealed class FechaItem : ObservableObject
{
    private bool _seleccionada;

    public FechaItem(DateTime fecha, bool esHoy, Action<FechaItem> alElegir)
    {
        Fecha = fecha;
        EsHoy = esHoy;
        ElegirCommand = new Command(() => alElegir(this));
    }

    public DateTime Fecha { get; }

    public bool EsHoy { get; }

    public ICommand ElegirCommand { get; }

    public string DiaSemana => FormatoFecha.DiaCorto(Fecha).ToLowerInvariant();

    public string Numero => Fecha.Day.ToString(CultureInfo.InvariantCulture);

    public string Etiqueta => EsHoy ? "Hoy" : "";

    public bool Seleccionada
    {
        get => _seleccionada;
        set
        {
            if (SetProperty(ref _seleccionada, value))
            {
                OnPropertyChanged(nameof(Descripcion));
            }
        }
    }

    public string Descripcion =>
        $"{FormatoFecha.DiaLargo(Fecha)} {Fecha.Day}{(EsHoy ? ", hoy" : "")}{(_seleccionada ? ", seleccionado" : "")}";
}

public static class FormatoFecha
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    public static string DiaCorto(DateTime fecha) =>
        Cultura.DateTimeFormat.AbbreviatedDayNames[(int)fecha.DayOfWeek].TrimEnd('.');

    public static string DiaLargo(DateTime fecha) =>
        Cultura.DateTimeFormat.DayNames[(int)fecha.DayOfWeek];

    public static string Mes(DateTime fecha) =>
        Cultura.DateTimeFormat.MonthNames[fecha.Month - 1];

    public static string MesCorto(DateTime fecha) =>
        Cultura.DateTimeFormat.AbbreviatedMonthNames[fecha.Month - 1].TrimEnd('.');

    public static string Hora(DateTime fecha) => fecha.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string Capitalizar(string texto) =>
        texto.Length == 0 ? texto : char.ToUpper(texto[0], Cultura) + texto[1..];

    public static string DiaConHora(DateTime fecha) =>
        $"{Capitalizar(DiaCorto(fecha))} {fecha.Day}, {Hora(fecha)}";

    public static string DiaLargoMes(DateTime fecha) =>
        $"{Capitalizar(DiaCorto(fecha))} {fecha.Day} de {Mes(fecha)}";

    public static string DiaMesHora(DateTime fecha) =>
        $"{Capitalizar(DiaCorto(fecha))} {fecha.Day} {MesCorto(fecha)} · {Hora(fecha)}";
}
