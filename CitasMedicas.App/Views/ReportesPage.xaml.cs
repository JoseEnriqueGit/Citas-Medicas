using CitasMedicas.App.ViewModels;

namespace CitasMedicas.App.Views;

public partial class ReportesPage : ContentPage
{
    private readonly TaskCompletionSource _resultado = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _cerrando;

    private ReportesPage(ReporteViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.CierreSolicitado += () => _ = CerrarAsync();
    }

    public static async Task MostrarAsync(INavigation navegacion, ReporteViewModel vm)
    {
        var pagina = new ReportesPage(vm);
        await navegacion.PushModalAsync(pagina, false);
        await pagina._resultado.Task;
    }

    protected override void OnDisappearing()
    {
        _cerrando = true;
        _resultado.TrySetResult();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CerrarAsync();
        return true;
    }

    private async Task CerrarAsync()
    {
        if (_cerrando)
        {
            return;
        }

        _cerrando = true;
        _resultado.TrySetResult();
        await Navigation.PopModalAsync(false);
    }
}
