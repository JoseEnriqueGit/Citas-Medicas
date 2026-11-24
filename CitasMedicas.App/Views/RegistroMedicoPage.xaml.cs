using CitasMedicas.App.ViewModels;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.Views;

public partial class RegistroMedicoPage : ContentPage
{
    private readonly RegistroMedicoViewModel _vm;
    private readonly TaskCompletionSource<Medico?> _resultado = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _cerrando;

    private RegistroMedicoPage(RegistroMedicoViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.FocoSolicitado += OnFocoSolicitado;
        _vm.CierreSolicitado += OnCierreSolicitado;
        _vm.PropertyChanged += OnPropertyChanged;
    }

    public static async Task<Medico?> MostrarAsync(INavigation navegacion, RegistroMedicoViewModel vm)
    {
        var pagina = new RegistroMedicoPage(vm);
        await navegacion.PushModalAsync(pagina, false);
        return await pagina._resultado.Task;
    }

    protected override void OnDisappearing()
    {
        _cerrando = true;
        _resultado.TrySetResult(null);
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CerrarAsync(null);
        return true;
    }

    private void OnCierreSolicitado(Medico? medico) => _ = CerrarAsync(medico);

    private async Task CerrarAsync(Medico? medico)
    {
        if (_cerrando)
        {
            return;
        }

        _cerrando = true;
        _resultado.TrySetResult(medico);
        await Navigation.PopModalAsync(false);
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RegistroMedicoViewModel.ResumenErrores) && _vm.HayErrores)
        {
            SemanticScreenReader.Announce(_vm.ResumenErrores);
        }
    }

    private void OnFocoSolicitado(CampoMedico campo)
    {
        var destino = campo switch
        {
            CampoMedico.Nombre => CampoNombre,
            CampoMedico.Especialidad => CampoEspecialidad,
            _ => CampoCentro
        };

        destino.Focus();
    }
}
