namespace Epsilon.Content;

public interface INavigatorPublicRoomRepository
{
    ValueTask<NavigatorPublicRoomDefinition?> GetByEntryIdAsync(
        int entryId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<NavigatorPublicRoomDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
