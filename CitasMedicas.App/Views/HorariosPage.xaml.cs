using CitasMedicas.App.Services;

namespace CitasMedicas.App.Views;

public partial class HorariosPage : ContentPage
{
    public HorariosPage(AppState estado)
    {
        InitializeComponent();
        BindingContext = estado;
    }
}
