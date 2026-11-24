using SQLite;

namespace CitasMedicas.Core.Models;

public class Medico
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Especialidad { get; set; } = "";

    public string Centro { get; set; } = "";
}
