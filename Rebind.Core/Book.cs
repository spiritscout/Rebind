namespace Rebind.Core;

public record Book(
    string Title,
    IReadOnlyList<string> Authors,
    string? Language,
    IReadOnlyList<Chapter> Chapters);

public record Chapter(
    string? Title,
    IReadOnlyList<Block> Content);

public abstract record Block;
public record Paragraph(string Html) : Block;
public record Heading(int Level, string Text) : Block;
public record SceneBreak : Block;
public record BlockQuote(IReadOnlyList<Block> Content) : Block;
public record BookImage(string Source, string? Alt) : Block;
public record Verse(IReadOnlyList<Stanza> Stanzas) : Block;
public record Stanza(IReadOnlyList<string> Lines);