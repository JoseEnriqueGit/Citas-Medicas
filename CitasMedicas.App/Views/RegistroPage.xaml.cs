using CitasMedicas.App.Services;

namespace CitasMedicas.App.Views;

public partial class RegistroPage : ContentPage
{
    public RegistroPage(AppState estado)
    {
        InitializeComponent();
        BindingContext = estado;
    }
}
