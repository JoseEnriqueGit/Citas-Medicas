namespace CitasMedicas.App.ViewModels;

public sealed record DatoItem(string Etiqueta, string Valor, string? Icono = null)
{
    public bool TieneIcono => Icono is not null;

    public string Descripcion => $"{Etiqueta}: {Valor}";
}
