namespace CitasMedicas.App.Controls;

public partial class AppBar : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(AppBar), "");

    public static readonly BindableProperty PatientNameProperty =
        BindableProperty.Create(nameof(PatientName), typeof(string), typeof(AppBar), null,
            propertyChanged: (b, _, _) => ((AppBar)b).ActualizarSubtitulo());

    public AppBar()
    {
        InitializeComponent();
        ActualizarSubtitulo();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? PatientName
    {
        get => (string?)GetValue(PatientNameProperty);
        set => SetValue(PatientNameProperty, value);
    }

    private void ActualizarSubtitulo()
    {
        if (string.IsNullOrEmpty(PatientName))
        {
            PatientIcon.IsVisible = false;
            SubtitleLabel.FormattedText = null;
            SubtitleLabel.Text = "Sin paciente registrado";
            SemanticProperties.SetDescription(SubtitleLabel, "Sin paciente registrado");
            return;
        }

        PatientIcon.IsVisible = true;
        SubtitleLabel.Text = null;
        SubtitleLabel.FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = "Paciente: " },
                new Span { Text = PatientName, FontAttributes = FontAttributes.Bold, TextColor = (Color)Application.Current!.Resources["Ink"] }
            }
        };
        SemanticProperties.SetDescription(SubtitleLabel, $"Paciente: {PatientName}");
    }
}
