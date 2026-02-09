using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public enum UnitSystem
{
    Metric,
    Imperial
}

public enum CoordinateFormat
{
    DecimalDegrees,
    DMS,
    UTM
}

public enum DistanceUnit
{
    Kilometers,
    Miles
}

public enum AltitudeUnit
{
    Meters,
    Feet
}

public enum PressureUnit
{
    KPa,
    InHg
}

public enum TemperatureUnit
{
    Celsius,
    Fahrenheit
}

public enum SpeedUnit
{
    KmH,
    Mph
}

public class LocationPreferences
{
    [Key]
    public Guid UserId { get; set; }
    
    public UnitSystem UnitSystem { get; set; }
    
    public CoordinateFormat CoordinateFormat { get; set; }
    
    public DistanceUnit DistanceUnit { get; set; }
    
    public AltitudeUnit AltitudeUnit { get; set; }
    
    public PressureUnit PressureUnit { get; set; }
    
    public TemperatureUnit TemperatureUnit { get; set; }
    
    public SpeedUnit SpeedUnit { get; set; }
    
    public int UpdateFrequency { get; set; }
    
    public bool BatteryOptimization { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
