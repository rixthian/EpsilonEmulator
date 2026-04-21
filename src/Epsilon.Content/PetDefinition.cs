namespace Epsilon.Content;

public sealed record PetDefinition(
    int PetTypeId,
    string SpeciesCode,
    string CatalogCode,
    IReadOnlyList<string> BreedCodes,
    IReadOnlyList<string> ChatterKeys);

