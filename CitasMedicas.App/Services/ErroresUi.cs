using System.Diagnostics;

namespace CitasMedicas.App.Services;

public static class ErroresUi
{
    public static async Task EjecutarAsync(Func<Task> accion)
    {
        try
        {
            await accion();
        }
        catch (Exception ex)
        {
            await InformarAsync(ex);
        }
    }

    public static void Ejecutar(Action accion)
    {
        try
        {
            accion();
        }
        catch (Exception ex)
        {
            _ = InformarAsync(ex);
        }
    }

    private static async Task InformarAsync(Exception ex)
    {
        Debug.WriteLine(ex);
        var pagina = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (pagina is null)
        {
            return;
        }

        try
        {
            await pagina.DisplayAlertAsync(
                "No se pudo completar la acción",
                "Ocurrió un problema. Inténtalo de nuevo.",
                "Aceptar");
        }
        catch (Exception alerta)
        {
            Debug.WriteLine(alerta);
        }
    }
}
