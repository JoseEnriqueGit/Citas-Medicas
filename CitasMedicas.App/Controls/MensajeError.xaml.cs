namespace CitasMedicas.App.Controls;

public partial class MensajeError : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(MensajeError), "",
            propertyChanged: (b, _, nuevo) => ((MensajeError)b).IsVisible = !string.IsNullOrEmpty(nuevo as string));

    public MensajeError()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
