using NUnit.Framework;
using Torronto.BLL;
using Torronto.DAL.Models;

namespace Torronto.Tests
{
    [TestFixture]
    public class QueueTest
    {
        [Test]
        public void TestNoFilter()
        {
            Assert.IsTrue(QueueService.IsMatchUserFilter(
                new Torrent
                {
                    VideoQuality = VideoQuality.Good,
                    AudioQuality = AudioQuality.Good,
                    Translation = Translation.Dub,
                    Size = 1500
                },
                new User
                {
                    FilterSizes = string.Empty,
                    FilterVideo = VideoQuality.Unknown,
                    FilterAudio = AudioQuality.Unknown,
                    FilterTraslation = Translation.Unknown
                }));
        }

        [Test]
        public void TestFilter()
        {
            Assert.IsTrue(QueueService.IsMatchUserFilter(
                new Torrent
                {
                    VideoQuality = VideoQuality.Good,
                    AudioQuality = AudioQuality.Good,
                    Translation = Translation.Dub,
                    Size = 1500
                },
                new User
                {
                    FilterSizes = string.Empty,
                    FilterVideo = VideoQuality.Good | VideoQuality.HighDefinition,
                    FilterAudio = AudioQuality.Good | AudioQuality.HighDefinition,
                    FilterTraslation = Translation.Unknown
                }));

            Assert.IsFalse(QueueService.IsMatchUserFilter(
                new Torrent
                {
                    VideoQuality = VideoQuality.Bad,
                    AudioQuality = AudioQuality.Good,
                    Translation = Translation.Dub,
                    Size = 1500
                },
                new User
                {
                    FilterSizes = string.Empty,
                    FilterVideo = VideoQuality.Good | VideoQuality.HighDefinition,
                    FilterAudio = AudioQuality.Good | AudioQuality.HighDefinition,
                    FilterTraslation = Translation.Unknown
                }));

            Assert.IsTrue(QueueService.IsMatchUserFilter(
                new Torrent
                {
                    VideoQuality = VideoQuality.Good,
                    AudioQuality = AudioQuality.Good,
                    Translation = Translation.Dub,
                    Size = 1500
                },
                new User
                {
                    FilterSizes = "1",
                    FilterVideo = VideoQuality.Good | VideoQuality.HighDefinition,
                    FilterAudio = AudioQuality.Good | AudioQuality.HighDefinition,
                    FilterTraslation = Translation.Unknown
                }));

            Assert.IsFalse(QueueService.IsMatchUserFilter(
                new Torrent
                {
                    VideoQuality = VideoQuality.Good,
                    AudioQuality = AudioQuality.Good,
                    Translation = Translation.Dub,
                    Size = 1500
                },
                new User
                {
                    FilterSizes = "0,2,4",
                    FilterVideo = VideoQuality.Good | VideoQuality.HighDefinition,
                    FilterAudio = AudioQuality.Good | AudioQuality.HighDefinition,
                    FilterTraslation = Translation.Unknown
                }));
        }
    }
}