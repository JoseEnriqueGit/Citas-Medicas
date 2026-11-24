namespace CitasMedicas.Core.Data;

public class CedulaDuplicadaException : Exception
{
    public CedulaDuplicadaException() : base("Esta cédula ya está registrada.")
    {
    }
}

public class MedicoDuplicadoException : Exception
{
    public MedicoDuplicadoException() : base("Ya hay un médico con ese nombre, especialidad y centro.")
    {
    }
}

public class HorarioOcupadoException : Exception
{
    public HorarioOcupadoException() : base("Ese horario ya fue tomado.")
    {
    }
}
