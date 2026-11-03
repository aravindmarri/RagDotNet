# Running models locally with Ollama

Ollama runs open-source AI models on your own machine and exposes them over HTTP
at http://localhost:11434. There is no API key and no per-request cost, which
makes it a good fit for development and for privacy-sensitive workloads.

## Installing and pulling models

On Windows you can install it with winget:

    winget install --id Ollama.Ollama -e

Then pull the models you need. A RAG app needs one embedding model and one chat
model:

    ollama pull nomic-embed-text
    ollama pull llama3.2

`nomic-embed-text` is a small, fast embedding model that produces 768-dimension
vectors. That number, 768, is what the vector store record must declare for its
embedding property. `llama3.2` is a 3-billion-parameter chat model that is about
a 2 GB download and runs comfortably on a laptop.

## Using Ollama from .NET

The OllamaSharp package provides `OllamaApiClient`. It implements both
`IChatClient` and `IEmbeddingGenerator`, so a single package covers chat and
embeddings. You create one client per model:

    IEmbeddingGenerator<string, Embedding<float>> embedder =
        new OllamaApiClient(new Uri("http://localhost:11434"), "nomic-embed-text");

    IChatClient chat =
        new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2");

Because `OllamaApiClient` is exposed through the standard interfaces, swapping to
a cloud provider later means changing only these two lines.

## Choosing models

Embedding quality matters more than chat model size for retrieval accuracy. If
answers are missing facts that are clearly in your documents, try a stronger
embedding model before reaching for a bigger chat model. If the chat model writes
good prose but ignores the context, the fix is usually a firmer system prompt,
not a different model.
