using CitasMedicas.App.ViewModels;
using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.App.Services;

public sealed class AppState : ObservableObject
{
    private readonly CitasRepository _repositorio;
    private Paciente? _paciente;
    private Medico? _medico;
    private DateTime? _fecha;
    private DateTime? _horario;

    public AppState(CitasRepository repositorio, PacienteService pacientes)
    {
        _repositorio = repositorio;
        _paciente = repositorio.ObtenerPacienteActivo();
        pacientes.PacientesCambiaron += OnPacientesCambiaron;
    }

    public event Action? PacienteActivoCambio;

    public event Action? DatosCambiaron;

    public Paciente? Paciente
    {
        get => _paciente;
        set
        {
            if (SetProperty(ref _paciente, value))
            {
                OnPropertyChanged(nameof(NombrePaciente));
            }
        }
    }

    public string? NombrePaciente => _paciente?.NombreCompleto;

    public Medico? Medico
    {
        get => _medico;
        set => SetProperty(ref _medico, value);
    }

    public DateTime? Fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    public DateTime? Horario
    {
        get => _horario;
        set => SetProperty(ref _horario, value);
    }

    public void RecargarPaciente() => Paciente = _repositorio.ObtenerPacienteActivo();

    private void OnPacientesCambiaron()
    {
        if (MainThread.IsMainThread)
        {
            Procesar();
            return;
        }

        MainThread.BeginInvokeOnMainThread(Procesar);
    }

    private void Procesar()
    {
        var previo = _paciente?.Id;
        RecargarPaciente();
        if (previo != _paciente?.Id)
        {
            Medico = null;
            Fecha = null;
            Horario = null;
            PacienteActivoCambio?.Invoke();
        }

        DatosCambiaron?.Invoke();
    }
}
