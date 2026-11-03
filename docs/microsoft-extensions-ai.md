# Microsoft.Extensions.AI

Microsoft.Extensions.AI (often shortened to MEAI) is a set of provider-agnostic
abstractions for talking to AI models from .NET. Instead of coding against a
specific vendor SDK, you code against interfaces, so switching models or
providers is a small change rather than a rewrite.

## The two core interfaces

`IChatClient` represents a chat or completion model. You send it a list of
`ChatMessage` objects and call `GetResponseAsync`, which returns a
`ChatResponse`. The generated text is available on `response.Text`. Each message
has a `ChatRole` (System, User, Assistant, or Tool). A System message is used to
give the model its instructions, and a User message carries the actual request.

`IEmbeddingGenerator<string, Embedding<float>>` represents an embedding model. It
turns text into a vector of numbers that captures meaning. The convenience
method `GenerateVectorAsync(text)` returns a single `ReadOnlyMemory<float>`,
which is exactly the shape a vector store expects.

## Choosing a provider

The same interfaces are implemented by many providers:

- OpenAI, through the `Microsoft.Extensions.AI.OpenAI` package. You create an
  `OpenAIClient`, call `GetEmbeddingClient(model)` or the chat client, and call
  `AsIEmbeddingGenerator()` or `AsIChatClient()` to adapt it.
- Azure OpenAI, through `AzureOpenAIClient` with `DefaultAzureCredential`.
- Local models through Ollama, using the OllamaSharp package, whose
  `OllamaApiClient` implements both `IChatClient` and `IEmbeddingGenerator`.

Because your code only depends on the interfaces, you can develop against a free
local model and move to a hosted model in production by changing the lines that
construct the client, and nothing else.

## Why abstractions matter for RAG

A RAG application uses an embedding model during indexing and retrieval, and a
chat model during answer generation. Keeping both behind MEAI interfaces means
the retrieval and generation code never mentions a specific vendor. That is the
main reason to prefer MEAI over calling a provider SDK directly.
