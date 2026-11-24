namespace CitasMedicas.App.Controls;

public partial class FormField : ContentView
{
    public static readonly BindableProperty CaptionProperty =
        BindableProperty.Create(nameof(Caption), typeof(string), typeof(FormField), "",
            propertyChanged: (b, _, _) => ((FormField)b).Actualizar());

    public static readonly BindableProperty IsOptionalProperty =
        BindableProperty.Create(nameof(IsOptional), typeof(bool), typeof(FormField), false,
            propertyChanged: (b, _, _) => ((FormField)b).Actualizar());

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FormField), "",
            BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FormField), "");

    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(FormField), "",
            propertyChanged: (b, _, _) => ((FormField)b).Actualizar());

    public static readonly BindableProperty ErrorProperty =
        BindableProperty.Create(nameof(Error), typeof(string), typeof(FormField), "",
            propertyChanged: (b, _, _) => ((FormField)b).Actualizar());

    public static readonly BindableProperty KeyboardProperty =
        BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(FormField), Keyboard.Default);

    public static readonly BindableProperty MaxLengthProperty =
        BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(FormField), int.MaxValue);

    public FormField()
    {
        InitializeComponent();
        Actualizar();
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public bool IsOptional
    {
        get => (bool)GetValue(IsOptionalProperty);
        set => SetValue(IsOptionalProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public string Error
    {
        get => (string)GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }

    public Keyboard Keyboard
    {
        get => (Keyboard)GetValue(KeyboardProperty);
        set => SetValue(KeyboardProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public new bool Focus() => Input.Focus();

    private void Actualizar()
    {
        var resources = Application.Current!.Resources;
        var hayError = !string.IsNullOrEmpty(Error);
        var caption = Caption;

        CaptionLabel.FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = caption },
                new Span
                {
                    Text = IsOptional ? " (opcional)" : "",
                    TextColor = (Color)resources["Muted"]
                }
            }
        };

        FieldFrame.Stroke = (Color)resources[hayError ? "Danger" : "Border"];
        FieldFrame.StrokeThickness = hayError ? 2 : 1;
        FieldFrame.Padding = new Thickness(hayError ? 10 : 11, 0);
        ErrorRow.IsVisible = hayError;
        ErrorText.Text = Error;
        HintLabelText.IsVisible = !hayError && !string.IsNullOrEmpty(Hint);

        var descripcion = IsOptional ? $"{caption}, opcional" : caption;
        SemanticProperties.SetDescription(Input, hayError ? $"{descripcion}. Error: {Error}" : descripcion);
    }
}
