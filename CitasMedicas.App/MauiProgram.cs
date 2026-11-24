using CitasMedicas.App.ViewModels;
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

#if ANDROID
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("SinSubrayado", (handler, _) =>
			handler.PlatformView.BackgroundTintList =
				Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
		Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("SinSubrayado", (handler, _) =>
			handler.PlatformView.BackgroundTintList =
				Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
#endif

		builder.Services.AddSingleton(_ => new CitasRepository(RutaBaseDatos.Actual));
		builder.Services.AddSingleton(TimeProvider.System);
		builder.Services.AddSingleton<HorarioService>();
		builder.Services.AddSingleton<PacienteService>();
		builder.Services.AddSingleton<CitaService>();
		builder.Services.AddSingleton<AppState>();

		builder.Services.AddSingleton<RegistroViewModel>();
		builder.Services.AddSingleton<MedicosViewModel>();
		builder.Services.AddSingleton<HorariosViewModel>();
		builder.Services.AddSingleton<SolicitarViewModel>();
		builder.Services.AddSingleton<HistorialViewModel>();
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
