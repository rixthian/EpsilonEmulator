namespace Epsilon.CoreGame;

public sealed record CharacterProfile(
    CharacterId CharacterId,
    AccountId AccountId,
    string Username,
    string Motto,
    string Figure,
    string Gender,
    RoomId HomeRoomId,
    int CreditsBalance,
    int ActivityPointsBalance,
    int RespectPoints,
    int DailyRespectPoints,
    int DailyPetRespectPoints,
    string PublicId = "");
