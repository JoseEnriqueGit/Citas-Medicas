using CitasMedicas.Core.Models;

namespace CitasMedicas.Core.Data;

internal static class MedicosIniciales
{
    private const string PlazaSalud = "Centro Médico Plaza Salud";
    private const string SantaLucia = "Clínica Santa Lucía";
    private const string SanRafael = "Hospital San Rafael";

    public static IReadOnlyList<Medico> Lista { get; } = new[]
    {
        new Medico { Nombre = "Dra. Carolina Mejía", Especialidad = "Cardiología", Centro = PlazaSalud },
        new Medico { Nombre = "Dr. Luis Almonte", Especialidad = "Pediatría", Centro = SantaLucia },
        new Medico { Nombre = "Dra. Yesenia Cabral", Especialidad = "Dermatología", Centro = PlazaSalud },
        new Medico { Nombre = "Dr. Rafael Pimentel", Especialidad = "Medicina general", Centro = SantaLucia },
        new Medico { Nombre = "Dra. Ana Beltré", Especialidad = "Ginecología", Centro = SantaLucia },
        new Medico { Nombre = "Dr. Julián Ortiz", Especialidad = "Cardiología", Centro = SantaLucia },
        new Medico { Nombre = "Dra. Patricia Núñez", Especialidad = "Pediatría", Centro = SanRafael },
        new Medico { Nombre = "Dr. Miguel Santana", Especialidad = "Medicina general", Centro = PlazaSalud },
        new Medico { Nombre = "Dra. Laura Espinal", Especialidad = "Ginecología", Centro = SanRafael }
    };
}
