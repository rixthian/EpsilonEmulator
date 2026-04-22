using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class ChatNormalizationTests
{
    [Fact]
    public void NormalizeChatText_PreservesExtendedSymbolsAndAltStyleCharacters()
    {
        string normalized = RoomInteractionService.NormalizeChatText("  Hola ☺ ♥ ™ € §  ");

        Assert.Equal("Hola ☺ ♥ ™ € §", normalized);
    }

    [Fact]
    public void NormalizeChatText_StripsControlCharactersAndCollapsesWhitespace()
    {
        string normalized = RoomInteractionService.NormalizeChatText("Hi\tthere\u0000\r\nfriend");

        Assert.Equal("Hi there friend", normalized);
    }
}
