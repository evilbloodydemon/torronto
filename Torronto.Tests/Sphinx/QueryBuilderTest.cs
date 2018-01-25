using NUnit.Framework;
using Torronto.Lib.Sphinx;

namespace Torronto.Tests.Sphinx
{
    [TestFixture]
    public class QueryBuilderTest
    {
        [Test]
        public void TestSelect()
        {
            var query = new SphinxQueryBuilder("test")
                .AddWhere("video_quality", 30)
                .AddWhere("sound_quality", 20)
                .Build();

            var expected =
                @"SELECT *
FROM `test`
WHERE `video_quality` = 30 AND `sound_quality` = 20";

            Assert.AreEqual(expected, query);
        }

        [Test]
        public void TestSelect2()
        {
            var query = new SphinxQueryBuilder("first_index", "second_index")
                .SelectColumns("a", "b", "c")
                .AddWhere("video_quality", 30)
                .Build();

            var expected =
                @"SELECT `a`, `b`, `c`
FROM `first_index`, `second_index`
WHERE `video_quality` = 30";

            Assert.AreEqual(expected, query);
        }

        [Test]
        public void TestSelectLiteral()
        {
            var query = new SphinxQueryBuilder("first_index", "second_index")
                .SelectColumns("a", "b", "c")
                .SelectLiteral("(a & 1) alias")
                .AddWhere("video_quality", 30)
                .Build();

            var expected =
                @"SELECT `a`, `b`, `c`, (a & 1) alias
FROM `first_index`, `second_index`
WHERE `video_quality` = 30";

            Assert.AreEqual(expected, query);
        }

        [Test]
        public void TestSelectMatch()
        {
            var query = new SphinxQueryBuilder("first_index")
                .SelectColumns("id")
                .AddMatch("abyr'valg")
                .AddWhere("video_quality", 30)
                .Build();

            var expected =
                @"SELECT `id`
FROM `first_index`
WHERE MATCH('abyr\'valg') AND `video_quality` = 30";

            Assert.AreEqual(expected, query);
        }

        [Test]
        public void TestSelectLimit()
        {
            var query = new SphinxQueryBuilder("first_index")
                .AddLimits(22, 33)
                .Build();

            var expected =
                @"SELECT *
FROM `first_index`
LIMIT 22, 33";

            Assert.AreEqual(expected, query);
        }
    }
}