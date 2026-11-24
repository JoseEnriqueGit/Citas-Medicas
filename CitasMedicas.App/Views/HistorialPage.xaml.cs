using CitasMedicas.App.ViewModels;

namespace CitasMedicas.App.Views;

public partial class HistorialPage : ContentPage
{
    private readonly HistorialViewModel _vm;

    public HistorialPage(HistorialViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.ConfirmarAsync = datos => CancelarCitaPage.MostrarAsync(Navigation, datos);
        _vm.AvisoMostrado += () => SemanticScreenReader.Announce("Cita cancelada");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Cargar();
    }
}
