namespace Epsilon.Gateway;

public sealed record SubmitHabbowoodMovieInput(
    string Title,
    string Synopsis,
    string ScriptPayload,
    string? ContactHandle,
    string? LanguageCode);
