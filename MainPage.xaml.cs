using LocationHeatmapApp.Data;
using LocationHeatmapApp.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;

namespace LocationHeatmapApp;

public partial class MainPage : ContentPage
{
    private readonly LocationDatabase _db;
    private CancellationTokenSource? _trackingCts;
    private int _savedCount = 0;

    private readonly List<Microsoft.Maui.Controls.Maps.Circle> _heatCircles = new();


 

    public MainPage(LocationDatabase db)
    {
        InitializeComponent();
        _db = db;

        // Update label when slider changes
        RadiusSlider.ValueChanged += (_, e) =>
        {
            RadiusValueLabel.Text = $"{(int)e.NewValue}";
        };
    }

    // ===== Tracking Loop =====
    private async void OnStartClicked(object? sender, EventArgs e)
    {
        if (_trackingCts != null)
        {
            StatusLabel.Text = "Tracking already running...";
            return;
        }

        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                StatusLabel.Text = "Location permission denied.";
                return;
            }

            _trackingCts = new CancellationTokenSource();
            StatusLabel.Text = "Tracking started...";

            while (!_trackingCts.IsCancellationRequested)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var loc = await Geolocation.GetLocationAsync(request, _trackingCts.Token);

                if (loc != null)
                {
                    var point = new LocationPoint
                    {
                        Latitude = loc.Latitude,
                        Longitude = loc.Longitude,
                        TimestampUtc = DateTime.UtcNow
                    };

                    await _db.AddPointAsync(point);
                    _savedCount++;

                    StatusLabel.Text = $"Saved #{_savedCount}: {point.Latitude:F5}, {point.Longitude:F5}";

                    var mapLocation = new Location(point.Latitude, point.Longitude);
                    MapControl.MoveToRegion(MapSpan.FromCenterAndRadius(mapLocation, Distance.FromKilometers(1)));
                }

                await Task.Delay(TimeSpan.FromSeconds(10), _trackingCts.Token);
            }
        }
        catch (TaskCanceledException)
        {
            StatusLabel.Text = "Tracking stopped.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            _trackingCts = null;
        }
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        _trackingCts?.Cancel();
    }

    // ===== Heatmap Rendering =====

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        try
        {
            var points = await _db.GetPointsAsync();
            StatusLabel.Text = $"DB has {points.Count} saved point(s). Drawing heatmap...";

            DrawHeatmap(points);

            StatusLabel.Text = $"Heatmap drawn. Points: {points.Count}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnClearClicked(object? sender, EventArgs e)
    {
        try
        {
            await _db.ClearAsync();
            _savedCount = 0;

            ClearHeatmap();
            StatusLabel.Text = "Database cleared + heatmap removed.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private void ClearHeatmap()
    {
        foreach (var c in _heatCircles)
        {
            MapControl.MapElements.Remove(c);
        }
        _heatCircles.Clear();
    }

    /// <summary>
    /// Heatmap approach:
    /// 1) Bin points into a small grid (reduces circles).
    /// 2) For each bin, draw a circle at the bin center.
    /// 3) Larger bin count => "hotter" => we add multiple circles to intensify.
    /// </summary>
    private void DrawHeatmap(List<LocationPoint> points)
    {
        ClearHeatmap();

        if (points.Count == 0)
            return;

        // Grid size in degrees. Smaller = more detailed but more circles.
        // 0.001 degrees ~ 111 meters (latitude). Good for a demo heatmap.
        const double cellSize = 0.001;

        // Bin points: key = (latCell, lonCell)
        var bins = new Dictionary<(int latCell, int lonCell), int>();

        foreach (var p in points)
        {
            int latCell = (int)Math.Floor(p.Latitude / cellSize);
            int lonCell = (int)Math.Floor(p.Longitude / cellSize);

            var key = (latCell, lonCell);

            if (bins.ContainsKey(key))
                bins[key]++;
            else
                bins[key] = 1;
        }

        int maxCount = bins.Values.Max();
        double radiusMeters = RadiusSlider.Value;

        foreach (var kvp in bins)
        {
            var (latCell, lonCell) = kvp.Key;
            int count = kvp.Value;

            // Center of the bin
            double lat = (latCell + 0.5) * cellSize;
            double lon = (lonCell + 0.5) * cellSize;

            // Intensity from 1..5 based on relative density
            // More density => add more circles (overlap makes it look hotter)
            int intensity = Math.Clamp((int)Math.Ceiling(5.0 * count / maxCount), 1, 3);

            for (int i = 0; i < intensity; i++)
            {
                var circle = new Microsoft.Maui.Controls.Maps.Circle
                {
                    Center = new Location(lat, lon),
                    Radius = new Distance(radiusMeters*0.3 +(i*10)), // slightly larger layers
                    StrokeWidth = 1,
                    StrokeColor = Colors.Red,
                    // Heat effect: red-ish fill with transparency
                    FillColor = Color.FromRgba(255, 0, 0, 100) // low alpha; stacking increases "heat"
                };

                MapControl.MapElements.Add(circle);
                _heatCircles.Add(circle);
            }

            // Zoom map to cover all points so you can actually see the heatmap
        var minLat = points.Min(p => p.Latitude);
        var maxLat = points.Max(p => p.Latitude);
        var minLon = points.Min(p => p.Longitude);
        var maxLon = points.Max(p => p.Longitude);

        var center = new Microsoft.Maui.Devices.Sensors.Location((minLat + maxLat) / 2.0, (minLon + maxLon) / 2.0);

        // Rough radius: half the diagonal distance (km)
        double latKm = (maxLat - minLat) * 111.0;
        double lonKm = (maxLon - minLon) * 111.0 * Math.Cos(center.Latitude * Math.PI / 180.0);
        double radiusKm = Math.Max(0.5, Math.Sqrt(latKm * latKm + lonKm * lonKm) / 2.0);

        MapControl.MoveToRegion(
            Microsoft.Maui.Maps.MapSpan.FromCenterAndRadius(
                center,
                Microsoft.Maui.Maps.Distance.FromKilometers(radiusKm + 0.5)));
        }
    }
}