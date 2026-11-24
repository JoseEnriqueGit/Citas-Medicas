using System.Windows.Input;
using CitasMedicas.Core.Models;

namespace CitasMedicas.App.ViewModels;

public sealed class MedicoOpcion : ObservableObject
{
    private bool _seleccionado;

    public MedicoOpcion(Medico medico, Action<MedicoOpcion> alElegir)
    {
        Medico = medico;
        ElegirCommand = new Command(() => alElegir(this));
    }

    public Medico Medico { get; }

    public string Iniciales => MedicoItem.InicialesDe(Medico.Nombre);

    public string Nombre => Medico.Nombre;

    public string Detalle => $"{Medico.Especialidad} · {Medico.Centro}";

    public ICommand ElegirCommand { get; }

    public bool Seleccionado
    {
        get => _seleccionado;
        set
        {
            if (SetProperty(ref _seleccionado, value))
            {
                OnPropertyChanged(nameof(Descripcion));
            }
        }
    }

    public string Descripcion => $"{Nombre}, {Detalle}{(_seleccionado ? ", seleccionado" : "")}";
}
