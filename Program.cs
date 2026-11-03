using System.Text;
using RagDotNet.Rag;

// RagDotNet — a local "chat with your documents" assistant.
//
// It loads the .md and .txt files in the docs/ folder, splits them into
// overlapping passages, embeds them into an in-memory vector store, and then
// answers your questions using only the passages it retrieves. Every answer
// shows which files it came from and how close each match was.
//
// Everything runs locally through Ollama: no API key, no cloud calls.

const string OllamaUri = "http://localhost:11434";
const string EmbeddingModel = "nomic-embed-text"; // 768-dimension embeddings
const string ChatModel = "llama3.2";

Console.OutputEncoding = Encoding.UTF8;

// Load documents. docs/ is copied next to the app on build, so this works from
// both `dotnet run` and a published build.
string docsFolder = Path.Combine(AppContext.BaseDirectory, "docs");
IReadOnlyList<LoadedDocument> documents = DocumentLoader.LoadFrom(docsFolder);

if (documents.Count == 0)
{
    Console.WriteLine($"No .md or .txt files found in:\n  {docsFolder}");
    Console.WriteLine("Add some documents there and run again.");
    return;
}

Console.WriteLine($"Loaded {documents.Count} document(s): {string.Join(", ", documents.Select(d => d.Source))}");
Console.WriteLine($"Indexing with '{EmbeddingModel}' — embedding every chunk locally...");

var engine = new RagEngine(OllamaUri, EmbeddingModel, ChatModel);
int chunks = await engine.IndexAsync(documents);

Console.WriteLine($"Indexed {chunks} chunks. Ask away.\n");

if (Console.IsInputRedirected)
{
    // Non-interactive (piped input, CI): run a few sample questions so the app
    // still demonstrates itself. The last one is deliberately out of scope.
    string[] samples =
    [
        "How do I create an IEmbeddingGenerator that uses Ollama?",
        "Why do the vector dimensions have to match the embedding model?",
        "What is a good chunk size for RAG, and why add overlap?",
        "When should I use RAG instead of fine-tuning?",
        "How do I deploy a Blazor app to Azure App Service?",
    ];

    foreach (string q in samples)
        await AnswerAsync(engine, q);
}
else
{
    Console.WriteLine("Type a question about the docs, or 'exit' to quit.\n");
    while (true)
    {
        Console.Write("> ");
        string? line = Console.ReadLine();
        if (line is null || line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;
        if (string.IsNullOrWhiteSpace(line))
            continue;

        Console.WriteLine();
        await AnswerAsync(engine, line.Trim());
    }
}

static async Task AnswerAsync(RagEngine engine, string question)
{
    RagAnswer answer = await engine.AskAsync(question);

    Console.WriteLine($"Q: {question}");
    Console.WriteLine($"A: {answer.Text}");

    string sources = string.Join(
        ", ",
        answer.Matches
            .GroupBy(m => m.Source)
            .Select(g => $"{g.Key} ({g.Max(m => m.Score):F2})"));
    Console.WriteLine($"   sources: {sources}");
    Console.WriteLine();
}
