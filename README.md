# RagDotNet

A local **"chat with your documents"** assistant, built in .NET with
[Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI)
and [Ollama](https://ollama.com). It runs entirely on your machine — no API key,
no cloud calls.

Point it at a folder of documents and ask questions in plain English. It
retrieves the most relevant passages, answers using only those passages, cites
the source files, and says "I don't have that in the documentation" when the
answer isn't there.

## What it does

1. **Loads** every `.md` and `.txt` file in the `docs/` folder.
2. **Chunks** each document into overlapping, word-based passages.
3. **Embeds** each chunk with `nomic-embed-text` and stores it in an in-memory
   vector store.
4. **Retrieves** the closest passages to your question and **generates** a
   grounded answer with `llama3.2`, showing the source files and match scores.

The included `docs/` are a real, accurate knowledge base about building RAG in
.NET, so the app is useful the moment you run it. Replace them with your own
notes, a product handbook, meeting minutes — anything in `.md` or `.txt`.

## Project layout

```
RagDotNet/
├── Program.cs              entry point: load, index, and the Q&A loop
├── Rag/
│   ├── RagEngine.cs        indexing + retrieval + grounded generation
│   ├── DocumentLoader.cs   reads .md/.txt files from a folder
│   ├── TextChunker.cs      overlapping, word-based chunking
│   └── DocChunk.cs         the vector-store record
└── docs/                   the knowledge base (swap in your own files)
```

## Stack

- **.NET 10** console app
- **Microsoft.Extensions.AI** — `IChatClient`, `IEmbeddingGenerator`
- **Microsoft.Extensions.VectorData** — vector store abstractions
- **CommunityToolkit.VectorData.InMemory** — in-memory vector store
- **OllamaSharp** — Ollama client (implements both AI interfaces)

## Prerequisites

1. Install Ollama: `winget install --id Ollama.Ollama -e`
2. Pull the models:
   ```bash
   ollama pull nomic-embed-text   # 768-dimension embeddings
   ollama pull llama3.2           # ~2 GB chat model
   ```
   `nomic-embed-text` produces **768-dimension** vectors — this must match the
   `[VectorStoreVector(dimensions: 768, ...)]` attribute in `Rag/DocChunk.cs`.

## Run

```bash
dotnet run -c Release
```

You get an interactive prompt:

```
> How do I retrieve the closest passages from the store?
A: Call SearchAsync(queryVector, top: k) ... (vector-data.md)
   sources: vector-data.md (0.71), rag-pipeline.md (0.63)

> exit
```

If input is piped (no console), it runs a few sample questions instead, so it
also works in scripts and CI.

## Make it your own

- **Use your own documents:** drop `.md`/`.txt` files into `docs/`.
- **Change the models:** edit the constants at the top of `Program.cs`. If you
  switch the embedding model, update the `dimensions` in `DocChunk.cs` to match.
- **Go to a real database:** swap `InMemoryVectorStore` in `Rag/RagEngine.cs` for
  a Qdrant, Azure AI Search, or pgvector connector — the rest of the code stays
  the same.
- **Tune retrieval:** adjust `topK` in `RagEngine.AskAsync`, or the `maxWords`
  and `overlapWords` in `TextChunker`.
