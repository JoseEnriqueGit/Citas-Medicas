using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;

namespace CitasMedicas.Core.Services;

public enum CampoSolicitud
{
    Paciente,
    Medico,
    Horario,
    Motivo
}

public sealed record SolicitudCita(int? MedicoId, DateTime? Inicio, string? Motivo);

public sealed class ResultadoSolicitud
{
    public Cita? Cita { get; init; }

    public IReadOnlyDictionary<CampoSolicitud, string> Errores { get; init; } =
        new Dictionary<CampoSolicitud, string>();

    public bool HorarioTomado { get; init; }

    public bool Exitoso => Cita is not null && Errores.Count == 0;
}

public sealed record ItemHistorial(Cita Cita, Medico Medico);

public sealed class CitaService
{
    public const int MaximoMotivo = 200;

    private readonly CitasRepository _repositorio;
    private readonly TimeProvider _reloj;

    public CitaService(CitasRepository repositorio, TimeProvider reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    public ResultadoSolicitud Solicitar(int? pacienteId, SolicitudCita solicitud)
    {
        var errores = new Dictionary<CampoSolicitud, string>();
        var ahora = _reloj.GetLocalNow().DateTime;

        if (pacienteId is null || _repositorio.ObtenerPaciente(pacienteId.Value) is null)
        {
            errores[CampoSolicitud.Paciente] = "Regístrate para solicitar una cita.";
        }

        if (solicitud.MedicoId is null || _repositorio.ObtenerMedico(solicitud.MedicoId.Value) is null)
        {
            errores[CampoSolicitud.Medico] = "Elige un médico.";
        }

        if (solicitud.Inicio is null)
        {
            errores[CampoSolicitud.Horario] = "Elige un horario.";
        }
        else if (!HorarioService.EsInicioDeBloque(solicitud.Inicio.Value))
        {
            errores[CampoSolicitud.Horario] = "Elige un horario de lunes a viernes entre 08:00 y 16:00.";
        }
        else if (solicitud.Inicio.Value <= ahora)
        {
            errores[CampoSolicitud.Horario] = "No puedes reservar un horario que ya pasó.";
        }

        var motivo = (solicitud.Motivo ?? "").Trim();
        if (motivo.Length == 0)
        {
            errores[CampoSolicitud.Motivo] = "Escribe el motivo de la consulta.";
        }
        else if (motivo.Length > MaximoMotivo)
        {
            errores[CampoSolicitud.Motivo] = $"El motivo no puede pasar de {MaximoMotivo} caracteres.";
        }

        if (errores.Count > 0)
        {
            return new ResultadoSolicitud { Errores = errores };
        }

        var cita = new Cita
        {
            PacienteId = pacienteId!.Value,
            MedicoId = solicitud.MedicoId!.Value,
            Inicio = solicitud.Inicio!.Value,
            Motivo = motivo,
            Estado = EstadoCita.Programada,
            CreadaEn = ahora
        };

        try
        {
            return new ResultadoSolicitud { Cita = _repositorio.InsertarCita(cita) };
        }
        catch (HorarioOcupadoException)
        {
            return new ResultadoSolicitud
            {
                HorarioTomado = true,
                Errores = new Dictionary<CampoSolicitud, string>
                {
                    [CampoSolicitud.Horario] = "Ese horario ya fue tomado. Elige otro."
                }
            };
        }
    }

    public bool Cancelar(int citaId) => CambiarEstado(citaId, EstadoCita.Cancelada);

    public bool MarcarCompletada(int citaId) => CambiarEstado(citaId, EstadoCita.Completada);

    public IReadOnlyList<ItemHistorial> Historial(int pacienteId, EstadoCita? estado = null)
    {
        var medicos = _repositorio.ListarMedicos().ToDictionary(m => m.Id);
        return _repositorio.CitasDePaciente(pacienteId)
            .Where(c => estado is null || c.Estado == estado)
            .Select(c => new ItemHistorial(c, medicos[c.MedicoId]))
            .ToList();
    }

    private bool CambiarEstado(int citaId, EstadoCita nuevo)
    {
        var cita = _repositorio.ObtenerCita(citaId);
        if (cita is null || cita.Estado != EstadoCita.Programada)
        {
            return false;
        }

        cita.Estado = nuevo;
        _repositorio.ActualizarCita(cita);
        return true;
    }
}
