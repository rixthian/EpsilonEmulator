namespace Epsilon.CoreGame;

public sealed record MessengerContact(
    CharacterId CharacterId,
    string Username,
    string Motto,
    bool IsOnline,
    int CategoryId,
    string RelationshipCode);
