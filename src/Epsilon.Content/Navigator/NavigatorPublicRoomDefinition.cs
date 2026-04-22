using Epsilon.CoreGame;

namespace Epsilon.Content;

public sealed record NavigatorPublicRoomDefinition(
    int EntryId,
    int OrderNumber,
    string BannerTypeCode,
    string Caption,
    string ImagePath,
    string ImageKind,
    RoomId RoomId,
    int CategoryId,
    int ParentCategoryId,
    string AssetPackageKey);
