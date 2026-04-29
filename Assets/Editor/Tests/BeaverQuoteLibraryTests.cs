using NUnit.Framework;

public sealed class BeaverQuoteLibraryTests
{
    [Test]
    public void TryAnswerStructureQuestionAnswersKnownStructureKeywords()
    {
        Assert.IsTrue(BeaverQuoteLibrary.TryAnswerStructureQuestion("斗拱怎么用", out string bracketsAnswer));
        Assert.AreEqual("斗拱层层出跳，能以柔克刚承重。", bracketsAnswer);

        Assert.IsTrue(BeaverQuoteLibrary.TryAnswerStructureQuestion("榫卯是什么", out string mortiseAnswer));
        Assert.AreEqual("榫卯不用钉子，木头咬住木头。", mortiseAnswer);

        Assert.IsTrue(BeaverQuoteLibrary.TryAnswerStructureQuestion("瓦当有什么作用", out string tileEndAnswer));
        Assert.AreEqual("瓦当是屋檐的圆帽子，挡雨。", tileEndAnswer);
    }

    [Test]
    public void TryAnswerStructureQuestionIgnoresUnknownQueries()
    {
        Assert.IsFalse(BeaverQuoteLibrary.TryAnswerStructureQuestion("今天去哪一关", out string answer));
        Assert.IsTrue(string.IsNullOrEmpty(answer));
    }

    [Test]
    public void GetAmbientQuoteReturnsCleanShortBeaverMessage()
    {
        string quote = BeaverQuoteLibrary.GetAmbientQuote("GameScene", false);

        Assert.IsFalse(string.IsNullOrWhiteSpace(quote));
        Assert.IsTrue(quote.StartsWith("河狸："));
        Assert.LessOrEqual(quote.Length, 42);
        Assert.IsFalse(quote.Contains("8 ."));
    }
}
