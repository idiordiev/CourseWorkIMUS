using System.Threading.Channels;

namespace CourseWorkIMUS;

public class Storage
{
    private readonly string _fileName;
    
    private readonly Channel<AirQualityData> _toWriteChannel;

    public Storage(Channel<AirQualityData> mainChannel, Channel<AirQualityData> reservedChannel)
    {
        _toWriteChannel = Channel.CreateUnbounded<AirQualityData>();
        
        ReadToSingleChannelAsync(mainChannel).ConfigureAwait(false);
        ReadToSingleChannelAsync(reservedChannel).ConfigureAwait(false);

        _fileName = $"data_{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv";
    }

    private async Task ReadToSingleChannelAsync(Channel<AirQualityData> source)
    {
        await foreach (var item in source.Reader.ReadAllAsync())
        {
            await _toWriteChannel.Writer.WriteAsync(item);
        }
        
        _toWriteChannel.Writer.Complete();
    }

    public async Task WriteToFileAsync()
    {
        using (var writer = new StreamWriter(_fileName))
        {
            await writer.WriteLineAsync("SensorId,Timestamp,TemperatureCelsius,Humidity,CO2Level");
            
            await foreach (var item in _toWriteChannel.Reader.ReadAllAsync())
            {
                //await writer.WriteLineAsync($"{item.SensorId},{item.Timestamp:yyyy-MM-dd HH:mm:ss},{item.TemperatureCelsius.ToString(CultureInfo.InvariantCulture)},{item.Humidity.ToString(CultureInfo.InvariantCulture)},{item.CO2Level.ToString(CultureInfo.InvariantCulture)}");
            }
        }
    }
}