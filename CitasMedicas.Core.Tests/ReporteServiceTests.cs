using CitasMedicas.Core.Data;
using CitasMedicas.Core.Models;
using CitasMedicas.Core.Services;

namespace CitasMedicas.Core.Tests;

public class ReporteServiceTests : IDisposable
{
    private static readonly DateTime Martes = new(2026, 11, 24, 10, 15, 0);

    private readonly CitasRepository _repo = new(":memory:");
    private readonly FixedTimeProvider _clock = new(Martes);
    private readonly ReporteService _servicio;
    private readonly Paciente _paciente;
    private readonly IReadOnlyList<Medico> _medicos;

    public ReporteServiceTests()
    {
        _servicio = new ReporteService(_repo, _clock);
        _paciente = _repo.InsertarPaciente(new Paciente
        {
            NombreCompleto = "María Fernández Peña",
            Cedula = "001-1234567-8",
            Telefono = "809-555-0142",
            FechaNacimiento = new DateTime(1991, 3, 14)
        });
        _medicos = _repo.ListarMedicos();
    }

    public void Dispose() => _repo.Dispose();

    private Cita Cita(int medicoIndex, DateTime inicio, EstadoCita estado, string motivo = "Consulta") =>
        _repo.InsertarCita(new Cita
        {
            PacienteId = _paciente.Id,
            MedicoId = _medicos[medicoIndex].Id,
            Inicio = inicio,
            Motivo = motivo,
            Estado = estado,
            CreadaEn = Martes
        });

    private void Sembrar()
    {
        Cita(0, new DateTime(2026, 11, 25, 8, 0, 0), EstadoCita.Programada);
        Cita(0, new DateTime(2026, 11, 20, 9, 0, 0), EstadoCita.Completada);
        Cita(0, new DateTime(2026, 11, 26, 9, 0, 0), EstadoCita.Cancelada);
        Cita(1, new DateTime(2026, 11, 25, 8, 0, 0), EstadoCita.Programada);
        Cita(5, new DateTime(2026, 11, 27, 10, 0, 0), EstadoCita.Programada);
    }

    [Fact]
    public void TotalsAreCountedByStatusWithTheCancellationRate()
    {
        Sembrar();

        var reporte = _servicio.Generar(null, null);

        Assert.Equal(5, reporte.Total);
        Assert.Equal(3, reporte.Programadas);
        Assert.Equal(1, reporte.Completadas);
        Assert.Equal(1, reporte.Canceladas);
        Assert.Equal(0.2, reporte.TasaCancelacion, 6);
        Assert.Equal(Martes, reporte.GeneradoEn);
    }

    [Fact]
    public void EveryDoctorIsListedEvenWithoutAppointmentsOrderedByTotalThenName()
    {
        Sembrar();

        var porMedico = _servicio.Generar(null, null).PorMedico;

        Assert.Equal(9, porMedico.Count);
        Assert.Equal("Dra. Carolina Mejía", porMedico[0].Medico.Nombre);
        Assert.Equal((3, 1, 1, 1), (porMedico[0].Total, porMedico[0].Programadas, porMedico[0].Completadas, porMedico[0].Canceladas));
        Assert.Equal(new[] { 1, 1 }, porMedico.Skip(1).Take(2).Select(m => m.Total));
        Assert.Equal(new[] { "Dr. Julián Ortiz", "Dr. Luis Almonte" }, porMedico.Skip(1).Take(2).Select(m => m.Medico.Nombre));
        Assert.All(porMedico.Skip(3), m => Assert.Equal(0, m.Total));
    }

    [Fact]
    public void SpecialtiesAreAggregatedOnlyWhenTheyHaveAppointments()
    {
        Sembrar();

        var porEspecialidad = _servicio.Generar(null, null).PorEspecialidad;

        Assert.Equal(new[] { ("Cardiología", 4), ("Pediatría", 1) }, porEspecialidad.Select(e => (e.Especialidad, e.Total)));
    }

    [Fact]
    public void ADayRangeIncludesBothEndsWholeDaysAndIgnoresTimeOfDay()
    {
        Sembrar();
        Cita(2, new DateTime(2026, 11, 25, 15, 30, 0), EstadoCita.Programada);

        var reporte = _servicio.Generar(new DateTime(2026, 11, 25, 17, 0, 0), new DateTime(2026, 11, 25, 6, 0, 0));

        Assert.Equal(3, reporte.Total);
    }

    [Fact]
    public void RangeWithOnlyOneEndIsOpenOnTheOtherSide()
    {
        Sembrar();

        Assert.Equal(4, _servicio.Generar(new DateTime(2026, 11, 25), null).Total);
        Assert.Equal(3, _servicio.Generar(null, new DateTime(2026, 11, 25)).Total);
    }

    [Fact]
    public void StartingAfterTheEndIsRejected()
    {
        Assert.Throws<ArgumentException>(() => _servicio.Generar(new DateTime(2026, 11, 26), new DateTime(2026, 11, 25)));
    }

    [Fact]
    public void EmptyReportHasZerosAndNoDivisionByZero()
    {
        var reporte = _servicio.Generar(null, null);

        Assert.Equal(0, reporte.Total);
        Assert.Equal(0, reporte.TasaCancelacion);
        Assert.Equal(9, reporte.PorMedico.Count);
        Assert.Empty(reporte.PorEspecialidad);
    }

    [Fact]
    public void PatientAndDoctorTotalsDoNotDependOnThePeriod()
    {
        Sembrar();

        var reporte = _servicio.Generar(new DateTime(2030, 1, 1), null);

        Assert.Equal(0, reporte.Total);
        Assert.Equal(1, reporte.TotalPacientes);
        Assert.Equal(9, reporte.TotalMedicos);
    }

    [Fact]
    public void ANewlyRegisteredDoctorAppearsInTheNextReport()
    {
        new MedicoService(_repo).Registrar(new RegistroMedico("Dra. Marta Peña", "Neurología", "Hospital San Rafael"));

        var reporte = _servicio.Generar(null, null);

        Assert.Equal(10, reporte.TotalMedicos);
        Assert.Contains(reporte.PorMedico, m => m.Medico.Nombre == "Dra. Marta Peña" && m.Total == 0);
    }

    [Fact]
    public void CsvHasHeaderAndOneRowPerAppointmentOrderedByStartWithCrLf()
    {
        Sembrar();

        var csv = _servicio.ExportarCsv(null, null);
        var lineas = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Fecha,Hora,Paciente,Cédula,Médico,Especialidad,Centro,Estado,Motivo", lineas[0]);
        Assert.Equal(6, lineas.Length);
        Assert.StartsWith("2026-11-20,09:00,María Fernández Peña,001-1234567-8,Dra. Carolina Mejía,Cardiología,Centro Médico Plaza Salud,Completada,Consulta", lineas[1]);
        Assert.Equal(lineas.Skip(1).Select(l => l[..16]).OrderBy(x => x), lineas.Skip(1).Select(l => l[..16]));
        Assert.EndsWith("\r\n", csv);
    }

    [Fact]
    public void EmptyCsvIsJustTheHeader()
    {
        Assert.Equal("Fecha,Hora,Paciente,Cédula,Médico,Especialidad,Centro,Estado,Motivo\r\n", _servicio.ExportarCsv(null, null));
    }

    [Fact]
    public void CsvQuotesCommasQuotesAndLineBreaks()
    {
        Cita(0, new DateTime(2026, 11, 25, 8, 0, 0), EstadoCita.Programada, "Dolor, \"fuerte\"\nde cabeza");

        var csv = _servicio.ExportarCsv(null, null);

        Assert.Contains("\"Dolor, \"\"fuerte\"\"\nde cabeza\"", csv);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://x\")")]
    [InlineData("+cmd")]
    [InlineData("-2+3")]
    [InlineData("@SUM(A1)")]
    public void CsvNeutralizesFormulasInUserTextCells(string motivo)
    {
        Cita(0, new DateTime(2026, 11, 25, 8, 0, 0), EstadoCita.Programada, motivo);

        var csv = _servicio.ExportarCsv(null, null);

        Assert.DoesNotContain("," + motivo, csv);
        Assert.Contains("'" + motivo[..1], csv);
    }

    [Fact]
    public void CsvNeutralizesFormulasInPatientNames()
    {
        var malo = _repo.InsertarPaciente(new Paciente
        {
            NombreCompleto = "=Evil()",
            Cedula = "001-7654321-0",
            Telefono = "809-555-0143",
            FechaNacimiento = new DateTime(1988, 9, 2)
        });
        _repo.InsertarCita(new Cita
        {
            PacienteId = malo.Id,
            MedicoId = _medicos[0].Id,
            Inicio = new DateTime(2026, 11, 25, 9, 0, 0),
            Motivo = "x",
            Estado = EstadoCita.Programada,
            CreadaEn = Martes
        });

        var csv = _servicio.ExportarCsv(null, null);

        Assert.Contains(",'=Evil(),", csv);
    }

    [Fact]
    public void AppointmentsWithMissingDoctorOrPatientAreStillReported()
    {
        _repo.InsertarCita(new Cita
        {
            PacienteId = 9999,
            MedicoId = 9999,
            Inicio = new DateTime(2026, 11, 25, 8, 0, 0),
            Motivo = "Huérfana",
            Estado = EstadoCita.Programada,
            CreadaEn = Martes
        });

        var csv = _servicio.ExportarCsv(null, null);
        var reporte = _servicio.Generar(null, null);

        Assert.Contains("Paciente desconocido", csv);
        Assert.Contains("Médico desconocido", csv);
        Assert.Equal(1, reporte.Total);
    }

    [Fact]
    public void CsvRespectsThePeriod()
    {
        Sembrar();

        var csv = _servicio.ExportarCsv(new DateTime(2026, 11, 27), new DateTime(2026, 11, 27));

        Assert.Equal(2, csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("2026-11-27,10:00", csv);
    }
}
