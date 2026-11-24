using CitasMedicas.App.ViewModels;

namespace CitasMedicas.App.Views;

public partial class CancelarCitaPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _resultado = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _cerrando;

    private CancelarCitaPage(IReadOnlyList<DatoItem> datos)
    {
        InitializeComponent();
        Datos.ItemsSource = datos;
    }

    public static async Task<bool> MostrarAsync(INavigation navegacion, IReadOnlyList<DatoItem> datos)
    {
        var pagina = new CancelarCitaPage(datos);
        await navegacion.PushModalAsync(pagina, false);
        return await pagina._resultado.Task;
    }

    protected override void OnDisappearing()
    {
        _cerrando = true;
        _resultado.TrySetResult(false);
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CerrarAsync(false);
        return true;
    }

    private async void OnMantener(object? sender, EventArgs e) => await CerrarAsync(false);

    private async void OnCancelar(object? sender, EventArgs e) => await CerrarAsync(true);

    private async Task CerrarAsync(bool confirmado)
    {
        if (_cerrando)
        {
            return;
        }

        _cerrando = true;
        _resultado.TrySetResult(confirmado);
        await Navigation.PopModalAsync(false);
    }
}
