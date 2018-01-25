using NUnit.Framework;
using Torronto.Lib.Sphinx;

namespace Torronto.Tests.Sphinx
{
    [TestFixture]
    public class ReplaceBuilderTest
    {
        [Test]
        public void TestReplace()
        {
            var query = new SphinxReplaceBuilder("test")
                .AddField("id", 1)
                .AddField("title", "No'ah")
                .AddField("content", null)
                .AddField("video_quality", 30)
                .AddField("sound_quality", 20)
                .Build();

            var expected =
                @"REPLACE INTO `test`
(`id`, `title`, `content`, `video_quality`, `sound_quality`)
VALUES
('1', 'No\'ah', '', '30', '20')";

            Assert.AreEqual(expected, query);
        }
    }
}