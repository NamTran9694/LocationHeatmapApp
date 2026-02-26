using SQLite;
using LocationHeatmapApp.Models;

namespace LocationHeatmapApp.Data;

/// <summary>
/// Provides a thread-safe wrapper for SQLite operations.
/// Handles asynchronous data persistence for GPS coordinates.
/// </summary>
public class LocationDatabase
{
    private readonly SQLiteAsyncConnection _db;
    
    // State flag to track if the database tables have been created
    private bool _initialized;

    // A Semaphore with a count of 1 acts as a "Lock." 
    // It ensures only one thread can initialize the database at a time.
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Constructor initialized via Dependency Injection in MauiProgram.cs.
    /// </summary>
    /// <param name="dbPath">The platform-specific file path for the .db3 file.</param>
    public LocationDatabase(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
    }

    /// <summary>
    /// Thread-safe lazy initialization. 
    /// Ensures the 'LocationPoint' table exists before any CRUD operations occur.
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        // Performance optimization: return immediately if already initialized
        if (_initialized) return;

        // Wait for the lock to become available
        await _initLock.WaitAsync();
        try
        {
            // Double-check flag after acquiring lock to prevent race conditions
            if (_initialized) return;
            // Create the table based on the LocationPoint model attributes
            await _db.CreateTableAsync<LocationPoint>().ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            // Always release the lock in a 'finally' block to avoid deadlocking the app
            _initLock.Release();
        }
    }

    /// <summary>
    /// Persists a new location coordinate to the database.
    /// </summary>
    /// <param name="point">The location model containing Lat, Lon, and Timestamp.</param>
    /// <returns>The number of rows added (usually 1).</returns>
    public async Task<int> AddPointAsync(LocationPoint point)
    {
        await EnsureInitializedAsync();
        return await _db.InsertAsync(point);
    }

    /// <summary>
    /// Retrieves all historical location data, sorted by newest first.
    /// </summary>
    /// <returns>A list of all captured LocationPoints.</returns>
    public async Task<List<LocationPoint>> GetPointsAsync()
    {
        await EnsureInitializedAsync();
        // Using LINQ-style syntax to sort data at the database level
        return await _db.Table<LocationPoint>()
            .OrderByDescending(p => p.TimestampUtc)
            .ToListAsync();
    }

    /// <summary>
    /// Wipes all data from the LocationPoint table. 
    /// Used when the user clicks the 'Clear' button in the UI.
    /// </summary>
    /// <returns>The number of rows deleted.</returns>
    public async Task<int> ClearAsync()
    {
        await EnsureInitializedAsync();
        return await _db.DeleteAllAsync<LocationPoint>();
    }
}