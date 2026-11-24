namespace CitasMedicas.App.Services;

public static class RutaBaseDatos
{
    private static readonly string[] Sufijos = ["", "-wal", "-shm", "-journal"];

    public static string Actual => Path.Combine(FileSystem.AppDataDirectory, "citas.db3");

    public static string ApartarArchivos()
    {
        var marca = DateTime.Now.ToString("yyyyMMddHHmmss");
        var ruta = Actual;
        foreach (var sufijo in Sufijos)
        {
            var origen = ruta + sufijo;
            if (File.Exists(origen))
            {
                File.Move(origen, $"{ruta}.corrupta-{marca}{sufijo}", true);
            }
        }

        return $"{ruta}.corrupta-{marca}";
    }
}
