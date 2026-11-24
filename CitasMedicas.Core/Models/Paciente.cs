using SQLite;

namespace CitasMedicas.Core.Models;

public class Paciente
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = "";

    [Unique]
    public string Cedula { get; set; } = "";

    public string Telefono { get; set; } = "";

    public string? Correo { get; set; }

    public DateTime FechaNacimiento { get; set; }

    public bool Activo { get; set; }
}
