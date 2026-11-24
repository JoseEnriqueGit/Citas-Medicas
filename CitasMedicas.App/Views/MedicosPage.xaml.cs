using CitasMedicas.App.ViewModels;

namespace CitasMedicas.App.Views;

public partial class MedicosPage : ContentPage
{
    private readonly MedicosViewModel _vm;

    public MedicosPage(MedicosViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.RegistrarAsync = formulario => RegistroMedicoPage.MostrarAsync(Navigation, formulario);
        _vm.AvisoMostrado += SemanticScreenReader.Announce;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Cargar();
    }
}
