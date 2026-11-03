namespace RagDotNet.Rag;

/// <summary>A document read from disk, before it is chunked.</summary>
internal sealed record LoadedDocument(string Source, string Text);

/// <summary>
/// Loads every .md and .txt file from a folder (recursively) so you can point
/// the app at your own documents instead of hardcoded text.
/// </summary>
internal static class DocumentLoader
{
    private static readonly string[] Extensions = [".md", ".txt"];

    public static IReadOnlyList<LoadedDocument> LoadFrom(string folder)
    {
        if (!Directory.Exists(folder))
            return [];

        var docs = new List<LoadedDocument>();
        foreach (string path in Directory
                     .EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                     .Where(p => Extensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
                     .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            string text = File.ReadAllText(path);
            if (!string.IsNullOrWhiteSpace(text))
                docs.Add(new LoadedDocument(Path.GetFileName(path), text));
        }

        return docs;
    }
}
