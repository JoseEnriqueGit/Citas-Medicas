using CitasMedicas.App.Services;
using CitasMedicas.App.Views;
using CitasMedicas.Core.Data;
using CitasMedicas.Core.Services;
using Microsoft.Extensions.Logging;

namespace CitasMedicas.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>();

		builder.Services.AddSingleton(_ => new CitasRepository(Path.Combine(FileSystem.AppDataDirectory, "citas.db3")));
		builder.Services.AddSingleton(TimeProvider.System);
		builder.Services.AddSingleton<HorarioService>();
		builder.Services.AddSingleton<PacienteService>();
		builder.Services.AddSingleton<CitaService>();
		builder.Services.AddSingleton<AppState>();

		builder.Services.AddTransient<RegistroPage>();
		builder.Services.AddTransient<MedicosPage>();
		builder.Services.AddTransient<HorariosPage>();
		builder.Services.AddTransient<SolicitarPage>();
		builder.Services.AddTransient<HistorialPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
