using System.Text;
using RagDotNet.Rag;
using Xunit.Abstractions;

namespace RagDotNet.Tests;

/// <summary>
/// Retrieval tested on its own — <see cref="RagEngine.RetrieveAsync"/> — with the
/// chat model never involved. If a wrong answer starts here (the right passage was
/// never retrieved), no prompt or model change downstream can fix it, so this is
/// where debugging a wrong answer should start.
/// </summary>
[Collection("rag")]
public sealed class RetrievalTests(RagFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public void Indexing_produced_chunks()
    {
        Assert.True(fixture.ChunkCount > 0, "the knowledge base indexed to zero chunks");
    }

    [Theory]
    [MemberData(nameof(InScopeQuestions))]
    public async Task Expected_document_is_retrieved_for_each_golden_question(string question, string[] expected)
    {
        IReadOnlyList<Match> matches = await fixture.Engine.RetrieveAsync(question);

        double recall = GoldenSet.Recall(matches, expected);

        // Recall@k must be perfect: the document that holds the answer has to be in
        // the retrieved set. Report what we actually got when it is not.
        Assert.True(
            recall == 1.0,
            $"recall {recall:P0} for \"{question}\"\n" +
            $"  expected: {string.Join(", ", expected)}\n" +
            $"  retrieved: {string.Join(", ", matches.Select(m => $"{m.Source} ({m.Score:F2})"))}");
    }

    [Fact]
    public async Task Scores_come_back_ordered_from_closest_to_least_close()
    {
        IReadOnlyList<Match> matches = await fixture.Engine.RetrieveAsync("what is a good chunk size?");

        Assert.NotEmpty(matches);

        double[] scores = matches.Select(m => m.Score).ToArray();
        Assert.Equal(scores.OrderByDescending(s => s), scores); // descending = closest first
        Assert.True(scores[0] > 0.4, $"top match looked weak at {scores[0]:F2}");
    }

    [Fact]
    public async Task Golden_eval_report()
    {
        double recallSum = 0, precisionSum = 0;
        int n = 0;

        var table = new StringBuilder();
        table.AppendLine("question                                          recall  prec@k  top source");
        table.AppendLine(new string('-', 92));

        foreach (GoldenQuestion q in GoldenSet.InScope)
        {
            IReadOnlyList<Match> matches = await fixture.Engine.RetrieveAsync(q.Question);
            double recall = GoldenSet.Recall(matches, q.ExpectedSources);
            double precision = GoldenSet.Precision(matches, q.ExpectedSources);
            recallSum += recall;
            precisionSum += precision;
            n++;

            Match top = matches[0];
            table.AppendLine($"{Trim(q.Question, 48),-48}  {recall,5:P0}  {precision,5:P0}  {top.Source} ({top.Score:F2})");
        }

        double meanRecall = recallSum / n;
        double meanPrecision = precisionSum / n;

        table.AppendLine(new string('-', 92));
        table.AppendLine($"mean recall@4 {meanRecall:P0}   mean precision@4 {meanPrecision:P0}   ({n} golden questions)");
        output.WriteLine(table.ToString());

        // The gate that fails the build when retrieval regresses.
        Assert.True(meanRecall >= 0.85, $"mean recall@4 dropped to {meanRecall:P0}");
    }

    public static IEnumerable<object[]> InScopeQuestions() =>
        GoldenSet.InScope.Select(q => new object[] { q.Question, q.ExpectedSources });

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
