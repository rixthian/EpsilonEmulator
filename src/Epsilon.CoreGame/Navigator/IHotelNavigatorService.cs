namespace Epsilon.CoreGame;

public interface IHotelNavigatorService
{
    ValueTask<NavigatorSearchSnapshot> SearchPublicRoomsAsync(
        NavigatorSearchRequest request,
        CancellationToken cancellationToken = default);
}
