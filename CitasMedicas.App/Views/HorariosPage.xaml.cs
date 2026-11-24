using CitasMedicas.App.ViewModels;

namespace CitasMedicas.App.Views;

public partial class HorariosPage : ContentPage
{
    private readonly HorariosViewModel _vm;

    public HorariosPage(HorariosViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.FechaVisible += TiraFechas.MostrarIndice;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Recargar();
    }
}
