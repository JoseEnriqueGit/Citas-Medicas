using CitasMedicas.App.ViewModels;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.Views;

public partial class RegistroPage : ContentPage
{
    private readonly RegistroViewModel _vm;
    private bool _ajustando;

    public RegistroPage(RegistroViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        PrepararSelector();
        _vm.FocoSolicitado += OnFocoSolicitado;
        _vm.PropertyChanged += OnPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        PrepararSelector();
    }

    private void PrepararSelector()
    {
        SelectorFecha.MaximumDate = DateTime.Today;
        if (_vm.FechaNacimiento is null)
        {
            _ajustando = true;
            SelectorFecha.Date = DateTime.Today.AddYears(-30);
            _ajustando = false;
        }
    }

    private void OnFechaSeleccionada(object? sender, DateChangedEventArgs e)
    {
        if (!_ajustando)
        {
            _vm.FechaNacimiento = e.NewDate;
        }
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RegistroViewModel.FechaNacimiento) && _vm.FechaNacimiento is { } fecha)
        {
            _ajustando = true;
            SelectorFecha.Date = fecha;
            _ajustando = false;
        }

        if (e.PropertyName == nameof(RegistroViewModel.ResumenErrores) && _vm.HayErrores)
        {
            SemanticScreenReader.Announce(_vm.ResumenErrores);
        }
    }

    private void OnFocoSolicitado(CampoPaciente campo)
    {
        View? destino = campo switch
        {
            CampoPaciente.NombreCompleto => CampoNombre,
            CampoPaciente.Cedula => CampoCedula,
            CampoPaciente.Telefono => CampoTelefono,
            CampoPaciente.Correo => CampoCorreo,
            CampoPaciente.Contrasena => CampoContrasena,
            _ => null
        };

        if (destino is Controls.FormField campoFormulario)
        {
            campoFormulario.Focus();
        }
    }
}
