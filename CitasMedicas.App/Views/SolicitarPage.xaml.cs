using CitasMedicas.App.Services;

namespace CitasMedicas.App.Views;

public partial class SolicitarPage : ContentPage
{
    public SolicitarPage(AppState estado)
    {
        InitializeComponent();
        BindingContext = estado;
    }
}
