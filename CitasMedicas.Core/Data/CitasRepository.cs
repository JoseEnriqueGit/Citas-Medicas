using CitasMedicas.Core.Models;
using SQLite;

namespace CitasMedicas.Core.Data;

public sealed class CitasRepository : IDisposable
{
    private readonly SQLiteConnection _db;
    private readonly object _gate = new();

    static CitasRepository()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    public CitasRepository(string path)
    {
        _db = new SQLiteConnection(path);
        _db.CreateTable<Paciente>();
        _db.CreateTable<Medico>();
        _db.CreateTable<Cita>();
        _db.Execute(
            "CREATE UNIQUE INDEX IF NOT EXISTS UX_Cita_Programada ON Cita (MedicoId, Inicio) WHERE Estado = 0");
        SembrarMedicos();
    }

    public void Dispose() => _db.Dispose();

    private void SembrarMedicos()
    {
        if (_db.Table<Medico>().Count() > 0)
        {
            return;
        }

        _db.InsertAll(MedicosIniciales.Lista.Select(m => new Medico
        {
            Nombre = m.Nombre,
            Especialidad = m.Especialidad,
            Centro = m.Centro
        }));
    }

    public IReadOnlyList<Medico> ListarMedicos()
    {
        lock (_gate)
        {
            return _db.Table<Medico>().OrderBy(m => m.Id).ToList();
        }
    }

    public Medico? ObtenerMedico(int id)
    {
        lock (_gate)
        {
            return _db.Find<Medico>(id);
        }
    }

    public Paciente InsertarPaciente(Paciente paciente)
    {
        lock (_gate)
        {
            _db.BeginTransaction();
            try
            {
                if (ExisteCedulaInterno(paciente.Cedula))
                {
                    throw new CedulaDuplicadaException();
                }

                _db.Execute("UPDATE Paciente SET Activo = 0");
                paciente.Activo = true;
                _db.Insert(paciente);
                _db.Commit();
                return paciente;
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
            {
                _db.Rollback();
                throw new CedulaDuplicadaException();
            }
            catch
            {
                _db.Rollback();
                throw;
            }
        }
    }

    public Paciente? ObtenerPaciente(int id)
    {
        lock (_gate)
        {
            return _db.Find<Paciente>(id);
        }
    }

    public Paciente? ObtenerPacienteActivo()
    {
        lock (_gate)
        {
            return _db.Table<Paciente>().Where(p => p.Activo).OrderByDescending(p => p.Id).FirstOrDefault();
        }
    }

    public bool ExisteCedula(string cedula)
    {
        lock (_gate)
        {
            return ExisteCedulaInterno(cedula);
        }
    }

    public int ContarPacientes()
    {
        lock (_gate)
        {
            return _db.Table<Paciente>().Count();
        }
    }

    public void ActualizarPaciente(Paciente paciente)
    {
        lock (_gate)
        {
            try
            {
                _db.Update(paciente);
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
            {
                throw new CedulaDuplicadaException();
            }
        }
    }

    private bool ExisteCedulaInterno(string cedula) =>
        _db.Table<Paciente>().Count(p => p.Cedula == cedula) > 0;

    public Cita InsertarCita(Cita cita)
    {
        lock (_gate)
        {
            try
            {
                _db.Insert(cita);
                return cita;
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
            {
                throw new HorarioOcupadoException();
            }
        }
    }

    public Cita? ObtenerCita(int id)
    {
        lock (_gate)
        {
            return _db.Find<Cita>(id);
        }
    }

    public void ActualizarCita(Cita cita)
    {
        lock (_gate)
        {
            try
            {
                _db.Update(cita);
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
            {
                throw new HorarioOcupadoException();
            }
        }
    }

    public IReadOnlyList<Cita> CitasProgramadas(int medicoId, DateTime desde, DateTime hasta)
    {
        lock (_gate)
        {
            var programada = EstadoCita.Programada;
            return _db.Table<Cita>()
                .Where(c => c.MedicoId == medicoId && c.Estado == programada && c.Inicio >= desde && c.Inicio < hasta)
                .ToList();
        }
    }

    public IReadOnlyList<Cita> CitasDePaciente(int pacienteId)
    {
        lock (_gate)
        {
            return _db.Table<Cita>()
                .Where(c => c.PacienteId == pacienteId)
                .OrderByDescending(c => c.Inicio)
                .ThenByDescending(c => c.Id)
                .ToList();
        }
    }
}
