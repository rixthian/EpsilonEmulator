namespace Epsilon.CoreGame;

public delegate ValueTask HotelEventHandler(
    HotelEventEnvelope envelope,
    CancellationToken cancellationToken);
