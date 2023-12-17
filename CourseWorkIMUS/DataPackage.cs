namespace CourseWorkIMUS;

public class DataPackage
{
    public DateTime GeneratedAt { get; set; }
    public DateTime StartedProcessingAt { get; set; }
    public DataPackageStatus Status { get; set; }
}