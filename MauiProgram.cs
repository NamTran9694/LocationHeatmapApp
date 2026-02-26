using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using LocationHeatmapApp.Data;

namespace LocationHeatmapApp;

/// <summary>
/// The entry point of the .NET MAUI application. 
/// Handles service registration, dependency injection, and component configuration.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            // Essential: Registers the native map handlers for iOS and Android
            .UseMauiMaps() // enables Microsoft.Maui.Controls.Maps Map control
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        // Only enable debug logging in development builds to optimize production performance
        builder.Logging.AddDebug();
#endif

        // ----------------------------
        // SQLite database registration
        // ----------------------------
        // Define a platform-agnostic path for the database file.
        // FileSystem.AppDataDirectory points to the app's private sandbox folder.
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "locations.db3");

        // Register the Database as a Singleton.
        // This ensures only one connection exists, preventing "Database is locked" errors.
        builder.Services.AddSingleton<LocationDatabase>(_ =>
            new LocationDatabase(dbPath));

        // ----------------------------
        // Page registration (DI)
        // ----------------------------
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}