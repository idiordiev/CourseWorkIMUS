namespace CourseWorkIMUS;

internal class Program
{ 
    private static readonly ProducerOptions ProducerOptions = new ProducerOptions
    {
        Producers = 4_000,
        TotalEvents = 10_000_000,
        IntervalMilliseconds = 10
    };

    private static readonly DistributionSystem DistributionSystem = new DistributionSystem(0, 0);
    
    public static async Task Main(string[] args)
    {
        Console.WriteLine($"Number of producers {ProducerOptions.Producers}");
        Console.WriteLine($"Events to generate {ProducerOptions.TotalEvents}");
        Console.WriteLine($"Interval for event generation by single producer {ProducerOptions.IntervalMilliseconds}ms");
        
        var storage = new Storage(DistributionSystem.MainChannel, DistributionSystem.ReservedChannel);
        
        var startedAt = DateTime.UtcNow;
        Console.WriteLine($"Starting at {startedAt}");
        
        await GenerateAsync();
        await storage.WriteToFileAsync();
        
        var finishedAt = DateTime.UtcNow;
        Console.WriteLine($"Finished at {finishedAt}");
        Console.WriteLine($"Time passed: {finishedAt - startedAt}");
        
        var stats = DistributionSystem.Stats;
        
        Console.WriteLine($"Total transferred through both channels {stats.TransferredTotal}");
        Console.WriteLine($"Total transferred through main channel {stats.TransferredThroughMainChannel}");
        Console.WriteLine($"Total transferred through reserve channel {stats.TransferredThroughReserveChannel}");
        Console.WriteLine($"Failed to transfer {stats.FailedToTransfer}");
    }

    private static async Task GenerateAsync()
    {
        var tasks = new List<Task>();
        var toGenerate = Divide(ProducerOptions.TotalEvents, ProducerOptions.Producers);

        for (var i = 0; i < ProducerOptions.Producers; i++)
        {
            tasks.Add(GenerateEvents(i + 1, toGenerate[i], TimeSpan.FromMilliseconds(ProducerOptions.IntervalMilliseconds)));
        }

        await Task.WhenAll(tasks);
        DistributionSystem.Complete();
    }
    
    private static async Task GenerateEvents(int sensorId, int count, TimeSpan interval)
    {
        for (var i = 0; i < count; i++)
        {
            var data = new AirQualityData
            {
                Timestamp = DateTime.UtcNow,
                SensorId = sensorId,
                TemperatureCelsius = (decimal)Random.Shared.NextDouble() * 30m - 10m,
                Humidity = (decimal)Random.Shared.NextDouble() * 100m,
                CO2Level = (decimal)Random.Shared.NextDouble(),
            };
            
            DistributionSystem.AcceptDataAsync(data);
            
            await Task.Delay(interval);
        }
    }
    
    private static int[] Divide(int number, int parts)
    {
        if (parts <= 0)
        {
            throw new ArgumentException("Number of parts must be greater than zero.");
        }

        var quotient = number / parts;
        var remainder = number % parts;

        var result = new int[parts];

        for (var i = 0; i < parts; i++)
        {
            result[i] = quotient;
        }

        for (var i = 0; i < remainder; i++)
        {
            result[i]++;
        }

        return result;
    }
}