namespace Epsilon.CoreGame;

public sealed record PetProfile(
    PetId PetId,
    CharacterId OwnerCharacterId,
    RoomId CurrentRoomId,
    string Name,
    int PetTypeId,
    string RaceCode,
    string ColorCode,
    int Experience,
    int Energy,
    int Nutrition,
    int Respect,
    int Level);

