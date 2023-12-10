using System.Threading.Channels;

namespace CourseWorkIMUS;

public class DistributionSystem
{
    public DistributionSystem(int mainChannelCapacity, int reserveChannelCapacity)
    {
        MainChannel = mainChannelCapacity > 0
            ? Channel.CreateBounded<AirQualityData>(mainChannelCapacity)
            : Channel.CreateUnbounded<AirQualityData>();

        ReservedChannel = reserveChannelCapacity > 0
            ? Channel.CreateBounded<AirQualityData>(reserveChannelCapacity)
            : Channel.CreateUnbounded<AirQualityData>();

        Stats = new DistributionStats();
    }

    public Channel<AirQualityData> MainChannel { get; }

    public Channel<AirQualityData> ReservedChannel { get; }

    public DistributionStats Stats { get; }

    public void AcceptDataAsync(AirQualityData data)
    {
        if (MainChannel.Writer.TryWrite(data))
        {
            Interlocked.Increment(ref Stats.TransferredThroughMainChannel);
            return;
        }

        if (ReservedChannel.Writer.TryWrite(data))
        {
            Interlocked.Increment(ref Stats.TransferredThroughReserveChannel);
            return;
        }

        Interlocked.Increment(ref Stats.FailedToTransfer);
    }

    public void BreakMainChannel()
    {
        MainChannel.Writer.Complete();
    }
    
    public void Complete()
    {
        MainChannel.Writer.Complete();
        ReservedChannel.Writer.Complete();
    }
}