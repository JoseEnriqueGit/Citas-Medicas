using System.Windows.Input;
using CitasMedicas.Core.Models;

namespace CitasMedicas.App.ViewModels;

public sealed class PacienteItem
{
    public PacienteItem(Paciente paciente, bool activo, Action<PacienteItem> cambiar)
    {
        Paciente = paciente;
        Activo = activo;
        CambiarCommand = new Command(() => cambiar(this), () => !activo);
    }

    public Paciente Paciente { get; }

    public bool Activo { get; }

    public bool Inactivo => !Activo;

    public string Nombre => Paciente.NombreCompleto;

    public string Cedula => Paciente.Cedula;

    public string Descripcion => Activo ? $"{Nombre}, paciente activo" : $"Cambiar a {Nombre}";

    public ICommand CambiarCommand { get; }
}
