using System.Windows.Input;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.ViewModels;

public sealed class BloqueItem
{
    public BloqueItem(BloqueHorario bloque, bool elegido, Action<BloqueItem> alElegir)
    {
        Inicio = bloque.Inicio;
        Hora = FormatoFecha.Hora(bloque.Inicio);
        Elegido = elegido;
        Variante = elegido ? "elegido" : bloque.Estado switch
        {
            EstadoBloque.Libre => "libre",
            EstadoBloque.Ocupado => "ocupado",
            _ => "pasado"
        };
        Estado = Variante switch
        {
            "elegido" => "Elegido",
            "libre" => "Libre",
            "ocupado" => "Ocupado",
            _ => "No disponible"
        };
        Descripcion = $"{Hora}, {Estado.ToLowerInvariant()}";
        ElegirCommand = bloque.Libre ? new Command(() => alElegir(this)) : null;
    }

    public DateTime Inicio { get; }

    public string Hora { get; }

    public string Estado { get; }

    public string Variante { get; }

    public bool Elegido { get; }

    public bool Deshabilitado => Variante is "ocupado" or "pasado";

    public string EstadoTexto => Estado.ToLowerInvariant();

    public string Descripcion { get; }

    public ICommand? ElegirCommand { get; }
}
