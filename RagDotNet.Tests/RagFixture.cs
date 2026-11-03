using RagDotNet.Rag;

namespace RagDotNet.Tests;

/// <summary>
/// Indexes the knowledge base once and shares the ready-to-query engine with
/// every test in the "rag" collection. Indexing embeds every chunk through
/// Ollama, so doing it per-test would be slow; a fixture keeps it to one pass.
/// Requires Ollama running locally with nomic-embed-text and llama3.2 pulled.
/// </summary>
public sealed class RagFixture : IAsyncLifetime
{
    public const string OllamaUri = "http://localhost:11434";
    public const string EmbeddingModel = "nomic-embed-text";
    public const string ChatModel = "llama3.2";

    internal RagEngine Engine { get; private set; } = default!;
    internal IReadOnlyList<LoadedDocument> Documents { get; private set; } = [];
    public int ChunkCount { get; private set; }

    public async Task InitializeAsync()
    {
        string docsFolder = Path.Combine(AppContext.BaseDirectory, "docs");
        Documents = DocumentLoader.LoadFrom(docsFolder);
        Assert.NotEmpty(Documents); // tests are meaningless without the corpus

        Engine = new RagEngine(OllamaUri, EmbeddingModel, ChatModel);
        ChunkCount = await Engine.IndexAsync(Documents);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("rag")]
public sealed class RagCollection : ICollectionFixture<RagFixture>;
