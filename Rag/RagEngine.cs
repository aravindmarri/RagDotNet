using CommunityToolkit.VectorData.InMemory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OllamaSharp;

namespace RagDotNet.Rag;

/// <summary>A generated answer plus the passages it was grounded in.</summary>
internal sealed record RagAnswer(string Text, IReadOnlyList<Match> Matches);

/// <summary>One retrieved passage and how close it was to the question.</summary>
internal sealed record Match(string Source, string Text, double Score);

/// <summary>
/// The RAG engine: it indexes documents into a vector store, then answers
/// questions by retrieving the closest passages and asking a chat model to
/// answer using only those passages.
/// </summary>
internal sealed class RagEngine
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private readonly IChatClient _chat;
    private readonly VectorStoreCollection<Guid, DocChunk> _collection;

    public RagEngine(string ollamaUri, string embeddingModel, string chatModel)
    {
        _embedder = new OllamaApiClient(new Uri(ollamaUri), embeddingModel);
        _chat = new OllamaApiClient(new Uri(ollamaUri), chatModel);

        var store = new InMemoryVectorStore();
        _collection = store.GetCollection<Guid, DocChunk>("documents");
    }

    /// <summary>Chunks, embeds, and stores every document. Returns the chunk count.</summary>
    public async Task<int> IndexAsync(IReadOnlyList<LoadedDocument> documents)
    {
        await _collection.EnsureCollectionExistsAsync();

        int count = 0;
        foreach (LoadedDocument doc in documents)
        {
            foreach (string chunk in TextChunker.Chunk(doc.Text))
            {
                ReadOnlyMemory<float> vector = await _embedder.GenerateVectorAsync(chunk);
                await _collection.UpsertAsync(new DocChunk
                {
                    Key = Guid.NewGuid(),
                    Source = doc.Source,
                    Text = chunk,
                    Vector = vector,
                });
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// The retrieval step on its own: embeds the question and returns the closest
    /// passages, ordered from closest to least close. Exposed separately so you can
    /// test retrieval without paying for (or being misled by) generation.
    /// </summary>
    public async Task<IReadOnlyList<Match>> RetrieveAsync(string question, int topK = 4)
    {
        ReadOnlyMemory<float> queryVector = await _embedder.GenerateVectorAsync(question);

        var matches = new List<Match>();
        await foreach (VectorSearchResult<DocChunk> result in
            _collection.SearchAsync(queryVector, top: topK))
        {
            matches.Add(new Match(result.Record.Source, result.Record.Text, result.Score ?? 0));
        }

        return matches;
    }

    /// <summary>Retrieves the top matches and generates a grounded answer.</summary>
    public async Task<RagAnswer> AskAsync(string question, int topK = 4)
    {
        IReadOnlyList<Match> matches = await RetrieveAsync(question, topK);

        string context = string.Join(
            "\n\n",
            matches.Select(m => $"[{m.Source}]\n{m.Text}"));

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "You are a precise documentation assistant. Use ONLY the context below.\n" +
                "- If the context answers the question, give one clear, direct answer and end " +
                "with the source file name(s) in parentheses.\n" +
                "- If the context does NOT contain the answer, reply with exactly this and " +
                "nothing else: I don't have that in the documentation.\n" +
                "Do one or the other, never both. Never invent details and never cite a file " +
                "that is not in the context."),
            new(ChatRole.User, $"Context:\n{context}\n\nQuestion: {question}"),
        };

        ChatResponse response = await _chat.GetResponseAsync(messages);
        return new RagAnswer(response.Text.Trim(), matches);
    }
}
