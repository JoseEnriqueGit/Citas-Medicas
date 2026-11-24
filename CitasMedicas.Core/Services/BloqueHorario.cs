namespace CitasMedicas.Core.Services;

public enum EstadoBloque
{
    Libre,
    Ocupado,
    Pasado
}

public sealed record BloqueHorario(DateTime Inicio, EstadoBloque Estado)
{
    public DateTime Fin => Inicio.AddMinutes(HorarioService.MinutosPorBloque);

    public bool Libre => Estado == EstadoBloque.Libre;
}
