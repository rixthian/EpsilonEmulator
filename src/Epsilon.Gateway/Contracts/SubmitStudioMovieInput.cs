namespace Epsilon.Gateway;

public sealed record SubmitStudioMovieInput(
    string Title,
    string Synopsis,
    string ScriptPayload,
    string? ContactHandle,
    string? LanguageCode);
