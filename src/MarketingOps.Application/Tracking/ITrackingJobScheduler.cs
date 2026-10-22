namespace MarketingOps.Application.Tracking;

public interface ITrackingJobScheduler
{
    void Enqueue(Guid siteId);
}
