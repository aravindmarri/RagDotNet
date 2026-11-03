using RagDotNet.Rag;

namespace RagDotNet.Tests;

/// <summary>
/// Generation tested after retrieval is trusted. Two failure modes live here: the
/// model inventing a citation that was never retrieved (unfaithful), and the model
/// answering anyway when nothing relevant was retrieved (it should say it does not
/// know). Both are checked without a human reading the output.
/// </summary>
[Collection("rag")]
public sealed class GenerationTests(RagFixture fixture)
{
    [Theory]
    [MemberData(nameof(InScopeQuestions))]
    public async Task In_scope_question_is_answered_and_cites_only_retrieved_sources(string question)
    {
        RagAnswer answer = await fixture.Engine.AskAsync(question);

        Assert.False(string.IsNullOrWhiteSpace(answer.Text));
        Assert.NotEqual(GoldenSet.Refusal, answer.Text); // it should have found the answer

        // Faithfulness: every file the answer names must be one that was actually
        // retrieved. A citation to anything else is a hallucinated source.
        HashSet<string> retrieved = answer.Matches
            .Select(m => m.Source)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string cited in GoldenSet.CitedFiles(answer.Text))
            Assert.Contains(cited, retrieved);
    }

    [Fact]
    public async Task Out_of_scope_question_takes_the_I_dont_know_path()
    {
        GoldenQuestion q = GoldenSet.Questions.Single(x => !x.InScope);

        RagAnswer answer = await fixture.Engine.AskAsync(q.Question);

        // The one behaviour that keeps RAG honest when retrieval comes up empty:
        // refuse, exactly, and cite nothing.
        Assert.Equal(GoldenSet.Refusal, answer.Text);
        Assert.Empty(GoldenSet.CitedFiles(answer.Text));
    }

    public static IEnumerable<object[]> InScopeQuestions() =>
        GoldenSet.InScope.Select(q => new object[] { q.Question });
}
