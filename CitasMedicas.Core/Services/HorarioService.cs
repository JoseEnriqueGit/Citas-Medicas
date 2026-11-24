using CitasMedicas.Core.Data;

namespace CitasMedicas.Core.Services;

public sealed class HorarioService
{
    public const int HoraApertura = 8;
    public const int HoraCierre = 16;
    public const int MinutosPorBloque = 30;
    public const int DiasDeBusqueda = 60;

    private readonly CitasRepository _repositorio;
    private readonly TimeProvider _reloj;

    public HorarioService(CitasRepository repositorio, TimeProvider reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    public DateTime Ahora => _reloj.GetLocalNow().DateTime;

    public bool EsDiaHabil(DateTime fecha) =>
        fecha.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    public IReadOnlyList<BloqueHorario> Bloques(int medicoId, DateTime fecha)
    {
        var dia = fecha.Date;
        var ahora = Ahora;
        if (!EsDiaHabil(dia) || dia < ahora.Date)
        {
            return Array.Empty<BloqueHorario>();
        }

        var ocupados = _repositorio
            .CitasProgramadas(medicoId, dia, dia.AddDays(1))
            .Select(c => c.Inicio)
            .ToHashSet();

        var bloques = new List<BloqueHorario>();
        for (var inicio = dia.AddHours(HoraApertura); inicio < dia.AddHours(HoraCierre); inicio = inicio.AddMinutes(MinutosPorBloque))
        {
            var estado = inicio <= ahora
                ? EstadoBloque.Pasado
                : ocupados.Contains(inicio) ? EstadoBloque.Ocupado : EstadoBloque.Libre;
            bloques.Add(new BloqueHorario(inicio, estado));
        }

        return bloques;
    }

    public bool TieneHueco(int medicoId, DateTime fecha) =>
        Bloques(medicoId, fecha).Any(b => b.Libre);

    public DateTime? ProximoDiaConHueco(int medicoId, DateTime desde)
    {
        var inicio = desde.Date;
        for (var i = 1; i <= DiasDeBusqueda; i++)
        {
            var dia = inicio.AddDays(i);
            if (TieneHueco(medicoId, dia))
            {
                return dia;
            }
        }

        return null;
    }

    public DateTime? ProximoHorarioLibre(int medicoId)
    {
        var hoy = Ahora.Date;
        for (var i = 0; i <= DiasDeBusqueda; i++)
        {
            var libre = Bloques(medicoId, hoy.AddDays(i)).FirstOrDefault(b => b.Libre);
            if (libre is not null)
            {
                return libre.Inicio;
            }
        }

        return null;
    }
}
