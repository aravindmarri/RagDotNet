using System.Text.RegularExpressions;
using RagDotNet.Rag;
using Match = RagDotNet.Rag.Match;

namespace RagDotNet.Tests;

/// <summary>
/// One entry in the evaluation set: a question we know the answer to, plus the
/// document(s) the answer should come from. A golden set like this is how you
/// turn "it feels wrong" into a number you can assert on.
/// </summary>
internal sealed record GoldenQuestion(string Question, string[] ExpectedSources, bool InScope = true);

/// <summary>
/// The golden questions and the retrieval metrics computed over them. Keeping the
/// metrics here (not inside the tests) means the same precision/recall and
/// faithfulness logic is reused everywhere and is easy to read.
/// </summary>
internal static class GoldenSet
{
    /// <summary>The exact refusal the system prompt tells the model to use.</summary>
    public const string Refusal = "I don't have that in the documentation.";

    public static readonly GoldenQuestion[] Questions =
    [
        new("How do I create an OllamaApiClient that generates embeddings?",
            ["ollama-local-models.md"]),
        new("Why must the vector dimensions match the embedding model?",
            ["vector-data.md"]),
        new("What is a good chunk size, and why add overlap between chunks?",
            ["rag-pipeline.md"]),
        new("When should I use RAG instead of fine-tuning?",
            ["rag-pipeline.md"]),
        new("With cosine similarity, does a higher or a lower score mean a closer match?",
            ["vector-data.md"]),
        new("What are the six steps of a RAG pipeline?",
            ["rag-pipeline.md"]),
        new("Which interfaces does Microsoft.Extensions.AI provide for chat and embeddings?",
            ["microsoft-extensions-ai.md"]),

        // Out of scope on purpose: the answer is in none of the documents, so the
        // app must refuse instead of making something up.
        new("How do I deploy a Blazor app to Azure App Service?", [], InScope: false),
    ];

    public static IEnumerable<GoldenQuestion> InScope => Questions.Where(q => q.InScope);

    /// <summary>
    /// Recall@k at the document level: of the documents we expected, how many
    /// turned up in the retrieved passages? This is the retrieval metric that
    /// matters most — if the right document never gets retrieved, no amount of
    /// prompt tuning can save the answer.
    /// </summary>
    public static double Recall(IReadOnlyList<Match> retrieved, IReadOnlyCollection<string> expected)
    {
        if (expected.Count == 0)
            return 1.0; // nothing was expected, so nothing can be missed

        HashSet<string> sources = retrieved.Select(m => m.Source).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int found = expected.Count(sources.Contains);
        return (double)found / expected.Count;
    }

    /// <summary>
    /// Precision@k at the passage level: of the passages we retrieved, how many
    /// came from a document we expected? Lower than recall is normal — the top-k
    /// usually pulls in a little off-topic context — but a very low number means
    /// the context is mostly noise.
    /// </summary>
    public static double Precision(IReadOnlyList<Match> retrieved, IReadOnlyCollection<string> expected)
    {
        if (retrieved.Count == 0)
            return 0.0;

        HashSet<string> exp = expected.ToHashSet(StringComparer.OrdinalIgnoreCase);
        int onTarget = retrieved.Count(m => exp.Contains(m.Source));
        return (double)onTarget / retrieved.Count;
    }

    /// <summary>Every file name the answer cites, e.g. "(vector-data.md)".</summary>
    public static IReadOnlyList<string> CitedFiles(string answer) =>
        Regex.Matches(answer, @"[A-Za-z0-9._-]+\.(?:md|txt)")
            .Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
