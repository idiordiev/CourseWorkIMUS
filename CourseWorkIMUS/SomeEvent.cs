namespace CourseWorkIMUS;

public class SomeEvent
{
    public DateTime GeneratedAt { get; set; }
    public DateTime StartedProcessingAt { get; set; }
    public EventStatus Status { get; set; }
}