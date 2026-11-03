using Microsoft.Extensions.AI;
using OllamaSharp;

namespace RagDotNet.Tests;

/// <summary>
/// Tests the embedding layer on its own, before any vector store is involved.
/// Two classic RAG bugs live here: the embedding width not matching the
/// <c>dimensions</c> declared on the vector record, and misreading the distance
/// score (treating "closer" as a smaller number when cosine similarity is larger).
/// </summary>
public sealed class EmbeddingTests
{
    private static IEmbeddingGenerator<string, Embedding<float>> Embedder() =>
        new OllamaApiClient(new Uri(RagFixture.OllamaUri), RagFixture.EmbeddingModel);

    [Fact]
    public async Task Embedding_width_matches_the_768_the_record_declares()
    {
        ReadOnlyMemory<float> vector = await Embedder().GenerateVectorAsync("any text at all");

        // DocChunk declares [VectorStoreVector(dimensions: 768, ...)]. If the model
        // ever returns a different width, the store rejects the upsert — so assert
        // the real width here instead of discovering it as a runtime crash.
        Assert.Equal(768, vector.Length);
    }

    [Fact]
    public async Task Related_texts_score_higher_than_unrelated_ones()
    {
        IEmbeddingGenerator<string, Embedding<float>> embedder = Embedder();

        ReadOnlyMemory<float> query = await embedder.GenerateVectorAsync("running AI models locally with Ollama");
        ReadOnlyMemory<float> related = await embedder.GenerateVectorAsync("serve open-source models on your own machine, no API key");
        ReadOnlyMemory<float> unrelated = await embedder.GenerateVectorAsync("a recipe for Neapolitan pizza dough");

        double near = Cosine(query.Span, related.Span);
        double far = Cosine(query.Span, unrelated.Span);

        // Higher cosine = closer. If this is ever backwards, every retrieval is too.
        Assert.True(near > far, $"expected related ({near:F3}) to beat unrelated ({far:F3})");
    }

    private static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
