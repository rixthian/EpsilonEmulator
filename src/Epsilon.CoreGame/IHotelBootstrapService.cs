namespace Epsilon.CoreGame;

public interface IHotelBootstrapService
{
    ValueTask<HotelBootstrapSnapshot?> BuildAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}

