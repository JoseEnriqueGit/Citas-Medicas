using CitasMedicas.App.ViewModels;

namespace CitasMedicas.App.Views;

public partial class HorariosPage : ContentPage
{
    private readonly HorariosViewModel _vm;

    public HorariosPage(HorariosViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.FechaVisible += OnFechaVisible;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Recargar();
    }

    private async void OnFechaVisible(int indice)
    {
        await Task.Yield();
        if (indice >= 0 && indice < FechasLayout.Children.Count && FechasLayout.Children[indice] is VisualElement destino)
        {
            await FechasScroll.ScrollToAsync(destino, ScrollToPosition.Center, false);
        }
    }
}
