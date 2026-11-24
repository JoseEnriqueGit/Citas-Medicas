using CitasMedicas.App.Views;
using CitasMedicas.Core.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CitasMedicas.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var ventana = new Window();
		ventana.Page = Abrir() ?? new ErrorArranquePage(() => Reintentar(ventana));
		return ventana;
	}

	private static bool Reintentar(Window ventana)
	{
		var pagina = Abrir();
		if (pagina is null)
		{
			return false;
		}

		ventana.Page = pagina;
		return true;
	}

	private static Page? Abrir()
	{
		try
		{
			_ = IPlatformApplication.Current!.Services.GetRequiredService<CitasRepository>();
			return new AppShell();
		}
		catch (Exception)
		{
			return null;
		}
	}
}
