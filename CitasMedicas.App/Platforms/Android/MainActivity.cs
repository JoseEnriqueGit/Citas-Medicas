using Android.App;
using Android.Content.PM;
using Android.OS;

namespace CitasMedicas.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (Window is { } ventana)
        {
            AndroidX.Core.View.WindowCompat.GetInsetsController(ventana, ventana.DecorView).AppearanceLightStatusBars = true;
        }
    }
}
