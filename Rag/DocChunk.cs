using Microsoft.Extensions.VectorData;

namespace RagDotNet.Rag;

/// <summary>
/// One passage taken from a source document, together with its embedding.
/// The attributes tell the vector store what each property is for.
/// </summary>
internal sealed class DocChunk
{
    [VectorStoreKey]
    public Guid Key { get; set; }

    /// <summary>The file this passage came from, used for citations.</summary>
    [VectorStoreData]
    public string Source { get; set; } = "";

    [VectorStoreData]
    public string Text { get; set; } = "";

    // 768 must match the embedding model (nomic-embed-text). Cosine similarity
    // means a higher score is a closer match.
    [VectorStoreVector(dimensions: 768, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
