using CitasMedicas.App.Services;

namespace CitasMedicas.App.Views;

public partial class ErrorArranquePage : ContentPage
{
    private readonly Func<bool> _reintentar;

    public ErrorArranquePage(Func<bool> reintentar)
    {
        InitializeComponent();
        _reintentar = reintentar;
    }

    private void OnReintentar(object? sender, EventArgs e) => Intentar(false);

    private void OnRestablecer(object? sender, EventArgs e) => Intentar(true);

    private void Intentar(bool restablecer)
    {
        try
        {
            if (restablecer)
            {
                RutaBaseDatos.ApartarArchivos();
            }
        }
        catch (Exception)
        {
            Mostrar("No se pudo apartar el archivo dañado. Libera espacio e inténtalo de nuevo.");
            return;
        }

        if (!_reintentar())
        {
            Mostrar("Todavía no se pueden abrir los datos. Inténtalo de nuevo.");
        }
    }

    private void Mostrar(string mensaje)
    {
        MensajeLabel.Text = mensaje;
        MensajeLabel.IsVisible = true;
        SemanticScreenReader.Announce(mensaje);
    }
}
