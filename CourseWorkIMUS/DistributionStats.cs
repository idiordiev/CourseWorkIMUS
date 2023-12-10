namespace CourseWorkIMUS;

public class DistributionStats
{
    public int TransferredThroughMainChannel;
    public int TransferredThroughReserveChannel;
    public int TransferredTotal => TransferredThroughReserveChannel + TransferredThroughReserveChannel;
    public int FailedToTransfer;
}