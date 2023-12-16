using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CourseWorkIMUS;

internal class Program
{
    private const int QueueSize = 100;
    private const int TotalEvents = 1_000;
    private const int GenerationIntervalMilliseconds = 130;
    private const int ProcessingTimeMilliseconds = 60;

    private static readonly Channel<SomeEvent> Queue = Channel.CreateBounded<SomeEvent>(
        new BoundedChannelOptions(QueueSize)
        {
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.DropWrite
        }, someEvent =>
        {
            someEvent.Status = EventStatus.Rejected;
            Interlocked.Increment(ref _rejected);
        });
    private static readonly ConcurrentBag<SomeEvent> ProcessedEvents = [];

    private static int _generated;
    private static int _rejected;
    
    public static async Task Main(string[] args)
    {
        Console.WriteLine($"Queue size {QueueSize}");
        Console.WriteLine($"Events to generate {TotalEvents}");
        Console.WriteLine($"Interval for event generation {GenerationIntervalMilliseconds}ms");
        Console.WriteLine($"Processing time {ProcessingTimeMilliseconds}ms");
        
        var startedAt = DateTime.UtcNow;
        Console.WriteLine($"Starting at {startedAt}");

        var tasks = new List<Task>
        {
            GenerateAsync(),
            ProcessByMainChannelAsync(),
            ProcessByReserveChannelAsync()
        };

        await Task.WhenAll(tasks);
        
        var finishedAt = DateTime.UtcNow;
        Console.WriteLine($"Finished at {finishedAt}");
        Console.WriteLine($"Time passed: {(finishedAt - startedAt).TotalSeconds}");
        
        Console.WriteLine($"Generated {_generated} events");
        Console.WriteLine($"Processed through both channels {ProcessedEvents.Count}");
        Console.WriteLine($"Processed through main channel {ProcessedEvents.Count(x => x.Status == EventStatus.ProcessedByMainChannel)}");
        Console.WriteLine($"Processed through reserve channel {ProcessedEvents.Count(x => x.Status == EventStatus.ProcessedByReserveChannel)}");
        Console.WriteLine($"Rejected {_rejected}");
        Console.WriteLine($"Average waiting time (for processed events) {ProcessedEvents.Average(x => (x.StartedProcessingAt - x.GeneratedAt).TotalMilliseconds):F2}ms");
    }

    private static async Task ProcessByMainChannelAsync()
    {
        await foreach (var item in Queue.Reader.ReadAllAsync())
        {
            item.StartedProcessingAt = DateTime.UtcNow;
            item.Status = EventStatus.ProcessedByMainChannel;
            ProcessedEvents.Add(item);
            
            await Task.Delay(TimeSpan.FromMilliseconds(ProcessingTimeMilliseconds));
        }
    }
    
    private static async Task ProcessByReserveChannelAsync()
    {
        await foreach (var item in Queue.Reader.ReadAllAsync())
        {
            item.StartedProcessingAt = DateTime.UtcNow;
            item.Status = EventStatus.ProcessedByReserveChannel;
            ProcessedEvents.Add(item);
            
            await Task.Delay(TimeSpan.FromMilliseconds(ProcessingTimeMilliseconds));
        }
    }
    
    private static async Task GenerateAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(GenerationIntervalMilliseconds));

        var i = 0;
        while (await timer.WaitForNextTickAsync() && i < TotalEvents)
        {
            i++;
            var someEvent = new SomeEvent
            {
                Status = EventStatus.Generated,
                GeneratedAt = DateTime.UtcNow
            };
            
            Interlocked.Increment(ref _generated);
            
            await Queue.Writer.WriteAsync(someEvent);
        }
        
        Queue.Writer.Complete();
    }
}