using System.Windows.Input;
using CitasMedicas.Core.Models;

namespace CitasMedicas.App.ViewModels;

public sealed class MedicoItem
{
    public MedicoItem(Medico medico, DateTime? proximo, Action<MedicoItem> alElegir)
    {
        Medico = medico;
        Proximo = proximo is { } fecha ? $"Próximo libre: {FormatoFecha.DiaConHora(fecha)}" : "Sin horarios libres próximos";
        Descripcion = proximo is { } f
            ? $"{medico.Nombre}, {medico.Especialidad}, {medico.Centro}. Próximo horario libre {FormatoFecha.DiaConHora(f)}"
            : $"{medico.Nombre}, {medico.Especialidad}, {medico.Centro}. Sin horarios libres próximos";
        ElegirCommand = new Command(() => alElegir(this));
    }

    public Medico Medico { get; }

    public string Iniciales => InicialesDe(Medico.Nombre);

    public string Nombre => Medico.Nombre;

    public string Especialidad => Medico.Especialidad;

    public string Centro => Medico.Centro;

    public string Proximo { get; }

    public string Descripcion { get; }

    public ICommand ElegirCommand { get; }

    public static string InicialesDe(string nombre) =>
        string.Concat(nombre
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.EndsWith('.'))
            .Take(2)
            .Select(p => char.ToUpperInvariant(p[0])));
}

public sealed class EspecialidadItem : ObservableObject
{
    private bool _seleccionada;

    public EspecialidadItem(string nombre, string? valor, Action<EspecialidadItem> alElegir)
    {
        Nombre = nombre;
        Valor = valor;
        ElegirCommand = new Command(() => alElegir(this));
    }

    public string Nombre { get; }

    public string? Valor { get; }

    public ICommand ElegirCommand { get; }

    public bool Seleccionada
    {
        get => _seleccionada;
        set => SetProperty(ref _seleccionada, value);
    }
}
