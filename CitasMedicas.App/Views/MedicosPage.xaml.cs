using CitasMedicas.App.Services;

namespace CitasMedicas.App.Views;

public partial class MedicosPage : ContentPage
{
    public MedicosPage(AppState estado)
    {
        InitializeComponent();
        BindingContext = estado;
    }
}
