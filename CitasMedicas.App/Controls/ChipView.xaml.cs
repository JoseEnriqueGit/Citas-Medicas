using System.Windows.Input;

namespace CitasMedicas.App.Controls;

public partial class ChipView : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(ChipView), "",
            propertyChanged: (b, _, _) => ((ChipView)b).Actualizar());

    public static readonly BindableProperty IsSelectedProperty =
        BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(ChipView), false,
            propertyChanged: (b, _, _) => ((ChipView)b).Actualizar());

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ChipView));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(ChipView));

    public ChipView()
    {
        InitializeComponent();
        Actualizar();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private void Actualizar()
    {
        var resources = Application.Current!.Resources;
        var seleccionado = IsSelected;
        ChipFrame.BackgroundColor = (Color)resources[seleccionado ? "PrimaryTint" : "Surface"];
        ChipFrame.Stroke = (Color)resources[seleccionado ? "Primary" : "Border"];
        ChipFrame.StrokeThickness = seleccionado ? 2 : 1;
        ChipFrame.Padding = new Thickness(seleccionado ? 14 : 15, 0);
        CheckIcon.IsVisible = seleccionado;
        TextLabel.TextColor = (Color)resources[seleccionado ? "PrimaryDark" : "Ink"];
        TextLabel.FontAttributes = seleccionado ? FontAttributes.Bold : FontAttributes.None;
        SemanticProperties.SetDescription(ChipFrame, seleccionado ? $"{Text}, seleccionado" : Text);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }
}
