namespace RagDotNet.Rag;

/// <summary>
/// Splits text into overlapping, word-based passages. Overlap keeps a sentence
/// that straddles a boundary from being cut in a way that loses its meaning.
/// </summary>
internal static class TextChunker
{
    public static IEnumerable<string> Chunk(string text, int maxWords = 120, int overlapWords = 20)
    {
        string[] words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            yield break;

        int step = Math.Max(1, maxWords - overlapWords);
        for (int start = 0; start < words.Length; start += step)
        {
            yield return string.Join(' ', words.Skip(start).Take(maxWords));

            // Stop once this chunk reached the end, so we don't emit a tiny tail.
            if (start + maxWords >= words.Length)
                yield break;
        }
    }
}
