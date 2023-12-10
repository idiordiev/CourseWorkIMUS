namespace CourseWorkIMUS;

public class AirQualityData
{
    public int SensorId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal TemperatureCelsius { get; set; }
    public decimal Humidity { get; set; }
    public decimal CO2Level { get; set; }
}