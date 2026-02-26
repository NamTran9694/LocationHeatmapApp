using SQLite;

namespace LocationHeatmapApp.Models;

/// <summary>
/// Data model representing a single GPS coordinate captured by the device.
/// This class is mapped directly to an SQLite table via attributes.
/// </summary>
public class LocationPoint
{
    /// <summary>
    /// Unique identifier for each database record.
    /// AutoIncrement ensures that the ID is generated automatically upon insertion.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// The exact moment the location was recorded. 
    /// Using UTC (Coordinated Universal Time) is a professional standard 
    /// to avoid issues with time zones and Daylight Saving Time.
    /// </summary>
    public DateTime TimestampUtc { get; set; }
}