using CitasMedicas.App.ViewModels;

namespace CitasMedicas.App.Views;

public partial class SolicitarPage : ContentPage
{
    private readonly SolicitarViewModel _vm;

    public SolicitarPage(SolicitarViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.ConfirmarAsync = datos => ConfirmarCitaPage.MostrarAsync(Navigation, datos);
        _vm.FechaVisible += TiraFechas.MostrarIndice;
        _vm.AvisoMostrado += OnAvisoMostrado;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Actualizar();
    }

    private async void OnAvisoMostrado()
    {
        SemanticScreenReader.Announce(_vm.TextoAviso);
        await Desplazamiento.ScrollToAsync(0, 0, true);
    }
}
