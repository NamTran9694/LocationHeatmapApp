using SQLite;
using LocationHeatmapApp.Models;

namespace LocationHeatmapApp.Data;

public class LocationDatabase
{
    private readonly SQLiteAsyncConnection _db;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public LocationDatabase(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            await _db.CreateTableAsync<LocationPoint>().ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<int> AddPointAsync(LocationPoint point)
    {
        await EnsureInitializedAsync();
        return await _db.InsertAsync(point);
    }

    public async Task<List<LocationPoint>> GetPointsAsync()
    {
        await EnsureInitializedAsync();
        return await _db.Table<LocationPoint>()
            .OrderByDescending(p => p.TimestampUtc)
            .ToListAsync();
    }

    public async Task<int> ClearAsync()
    {
        await EnsureInitializedAsync();
        return await _db.DeleteAllAsync<LocationPoint>();
    }
}