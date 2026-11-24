using System.Collections;

namespace CitasMedicas.App.Controls;

public partial class FechasStrip : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(FechasStrip));

    public FechasStrip()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public async void MostrarIndice(int indice)
    {
        await Task.Yield();
        if (indice >= 0 && indice < Contenedor.Children.Count && Contenedor.Children[indice] is VisualElement destino)
        {
            await Desplazamiento.ScrollToAsync(destino, ScrollToPosition.Center, false);
        }
    }
}
