namespace Epsilon.Gateway;

public interface IHotelIntelligenceService
{
    ValueTask<HotelIntelligenceSummary> BuildAsync(
        CancellationToken cancellationToken = default);
}
