using SQLite;

namespace CitasMedicas.Core.Models;

public class Cita
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int PacienteId { get; set; }

    [Indexed]
    public int MedicoId { get; set; }

    public DateTime Inicio { get; set; }

    public string Motivo { get; set; } = "";

    public EstadoCita Estado { get; set; }

    public DateTime CreadaEn { get; set; }
}
