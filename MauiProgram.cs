using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using LocationHeatmapApp.Data;

namespace LocationHeatmapApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiMaps() // enables Microsoft.Maui.Controls.Maps Map control
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ----------------------------
        // SQLite database registration
        // ----------------------------
        // IMPORTANT: Do NOT call async .Wait() here (can hang app startup).
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "locations.db3");

        builder.Services.AddSingleton<LocationDatabase>(_ =>
            new LocationDatabase(dbPath));

        // ----------------------------
        // Page registration (DI)
        // ----------------------------
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}