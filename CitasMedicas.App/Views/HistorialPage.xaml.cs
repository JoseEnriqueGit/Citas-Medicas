using CitasMedicas.App.Services;

namespace CitasMedicas.App.Views;

public partial class HistorialPage : ContentPage
{
    public HistorialPage(AppState estado)
    {
        InitializeComponent();
        BindingContext = estado;
    }
}
