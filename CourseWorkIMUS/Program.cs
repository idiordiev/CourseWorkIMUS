using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CourseWorkIMUS;

internal class Program
{
    private const int QueueSize = 100;
    private const int TotalEvents = 1_000;
    private const int GenerationIntervalMilliseconds = 150;
    private const int ProcessingTimeMilliseconds = 130;

    private static readonly Channel<DataPackage> Queue = Channel.CreateBounded<DataPackage>(
        new BoundedChannelOptions(QueueSize)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        }, dataPackage =>
        {
            dataPackage.Status = DataPackageStatus.Rejected;
            Interlocked.Increment(ref _rejected);
        });
    private static readonly ConcurrentBag<DataPackage> ProcessedEvents = [];

    private static int _generated;
    private static int _rejected;

    private static Task _mainChannelTask = Task.CompletedTask;
    private static Task _reserveChannelTask = Task.CompletedTask;
    
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
            ProcessEventsFirstNonBlockedAsync()
        };

        await Task.WhenAll(tasks);
        
        var finishedAt = DateTime.UtcNow;
        Console.WriteLine($"Finished at {finishedAt}");
        Console.WriteLine($"Time passed: {(finishedAt - startedAt).TotalSeconds}");
        
        Console.WriteLine($"Generated {_generated} events");
        Console.WriteLine($"Rejected {_rejected}");

        var totalProcessed = ProcessedEvents.Count;
        Console.WriteLine($"Processed through both channels {totalProcessed}");
        
        var processedByMain = ProcessedEvents.Count(x => x.Status == DataPackageStatus.ProcessedByMainChannel);
        Console.WriteLine($"Processed through main channel {processedByMain}");
        
        var processedByReserve = ProcessedEvents.Count(x => x.Status == DataPackageStatus.ProcessedByReserveChannel);
        Console.WriteLine($"Processed through reserve channel {processedByReserve}");
        
        var averageWaitingTimeMs = ProcessedEvents
            .Average(x => (x.StartedProcessingAt - x.GeneratedAt).TotalMilliseconds);
        Console.WriteLine($"Average waiting time (for processed events) {averageWaitingTimeMs:F2}ms");
    }
    
    private static async Task GenerateAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(GenerationIntervalMilliseconds));

        var i = 0;
        while (await timer.WaitForNextTickAsync() && i < TotalEvents)
        {
            i++;
            var dataPackage = new DataPackage
            {
                Status = DataPackageStatus.Generated,
                GeneratedAt = DateTime.UtcNow
            };
            
            Interlocked.Increment(ref _generated);
            
            await Queue.Writer.WriteAsync(dataPackage);
        }
        
        Queue.Writer.Complete();
    }

    private static async Task ProcessEventsFirstNonBlockedAsync()
    {
        await foreach (var item in Queue.Reader.ReadAllAsync())
        {
            await Task.WhenAny(_mainChannelTask, _reserveChannelTask);
            
            if (_mainChannelTask.IsCompleted)
            {
                _mainChannelTask = ProcessItemAsync(item, DataPackageStatus.ProcessedByMainChannel);
            }
            else if (_reserveChannelTask.IsCompleted)
            {
                _reserveChannelTask = ProcessItemAsync(item, DataPackageStatus.ProcessedByReserveChannel);
            }
        }
        
        await Task.WhenAll(_mainChannelTask, _reserveChannelTask);
    }
    
    private static async Task ProcessItemAsync(DataPackage item, DataPackageStatus status)
    {
        item.StartedProcessingAt = DateTime.UtcNow;
        item.Status = status;
        
        await Task.Delay(TimeSpan.FromMilliseconds(ProcessingTimeMilliseconds));
        
        ProcessedEvents.Add(item);
    }

    // private static async Task ProcessEventsRandomAsync()
    // {
    //     await Task.WhenAll(ProcessByMainChannelAsync(), ProcessByReserveChannelAsync());
    // }
    //
    // private static async Task ProcessByMainChannelAsync()
    // {
    //     await foreach (var item in Queue.Reader.ReadAllAsync())
    //     {
    //         item.StartedProcessingAt = DateTime.UtcNow;
    //         item.Status = EventStatus.ProcessedByMainChannel;
    //         ProcessedEvents.Add(item);
    //         
    //         await Task.Delay(TimeSpan.FromMilliseconds(ProcessingTimeMilliseconds));
    //     }
    // }
    //
    // private static async Task ProcessByReserveChannelAsync()
    // {
    //     await foreach (var item in Queue.Reader.ReadAllAsync())
    //     {
    //         item.StartedProcessingAt = DateTime.UtcNow;
    //         item.Status = EventStatus.ProcessedByReserveChannel;
    //         ProcessedEvents.Add(item);
    //         
    //         await Task.Delay(TimeSpan.FromMilliseconds(ProcessingTimeMilliseconds));
    //     }
    // }
}