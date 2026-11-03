using RagDotNet.Rag;

namespace RagDotNet.Tests;

/// <summary>
/// Chunking is pure text processing — no model, no Ollama — so it is the cheapest
/// thing to test and the first place to look when retrieval misses. If chunks are
/// too big, too small, or lose their overlap, retrieval quality drops before an
/// embedding is ever computed.
/// </summary>
public sealed class ChunkingTests
{
    // 60 numbered words give us something we can assert exact boundaries on.
    private static readonly string LongText =
        string.Join(' ', Enumerable.Range(1, 60).Select(i => $"word{i}"));

    [Fact]
    public void Empty_or_whitespace_text_produces_no_chunks()
    {
        Assert.Empty(TextChunker.Chunk(""));
        Assert.Empty(TextChunker.Chunk("   \n  \t "));
    }

    [Fact]
    public void Short_text_stays_a_single_chunk()
    {
        string[] chunks = TextChunker.Chunk("a short passage", maxWords: 120).ToArray();

        string single = Assert.Single(chunks);
        Assert.Equal("a short passage", single);
    }

    [Fact]
    public void Long_text_is_split_into_several_chunks()
    {
        string[] chunks = TextChunker.Chunk(LongText, maxWords: 25, overlapWords: 5).ToArray();

        Assert.True(chunks.Length > 1, "a 60-word document should produce more than one chunk");
    }

    [Fact]
    public void No_chunk_exceeds_the_configured_max_words()
    {
        const int maxWords = 25;

        foreach (string chunk in TextChunker.Chunk(LongText, maxWords, overlapWords: 5))
        {
            int words = chunk.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            Assert.True(words <= maxWords, $"chunk had {words} words, max is {maxWords}");
        }
    }

    [Fact]
    public void Consecutive_chunks_overlap_by_the_configured_word_count()
    {
        const int maxWords = 25;
        const int overlapWords = 5;

        string[] chunks = TextChunker.Chunk(LongText, maxWords, overlapWords).ToArray();

        // The last `overlap` words of one chunk must be the first `overlap` words
        // of the next, so a sentence spanning the boundary survives in both.
        for (int i = 0; i < chunks.Length - 1; i++)
        {
            string[] current = chunks[i].Split(' ');
            string[] next = chunks[i + 1].Split(' ');

            string[] tail = current[^overlapWords..];
            string[] head = next[..overlapWords];

            Assert.Equal(tail, head);
        }
    }
}
