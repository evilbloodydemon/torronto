using NUnit.Framework;
using System;
using System.Collections.Generic;
using Torronto.BLL;
using Torronto.BLL.Detector;
using Torronto.DAL.Models;

namespace Torronto.Tests
{
    [TestFixture]
    public class DetectorTest
    {
        public QualityDetector QualityDetector { get; set; }

        [TestFixtureSetUp]
        public void Init()
        {
            QualityDetector = new QualityDetector();
        }

        [Test]
        public void TestParseVideoQuality()
        {
            Assert.AreEqual(VideoQuality.Good, QualityDetector.ParseVideoQuality(
                "bdrip (источник: chdbits / blu-ray / 1080p)",
                "704x384 (1.83:1), 23.976 fps, xvid build 65 ~1807 kbps avg, 0.28 bit/pixel",
                "matrix (1999)"
                ));

            Assert.AreEqual(VideoQuality.HighDefinition, QualityDetector.ParseVideoQuality(
                "bdrip 1080p",
                "mpeg-4 avc, 13 мбит/с, 1920x1042, 23.976 кадр/с",
                "matrix (1999)"
                ));

            Assert.AreEqual(VideoQuality.Unknown, QualityDetector.ParseVideoQuality(
                "hdrip",
                "1920x1042",
                "matrix (1999) | трейлер"
                ));

            Assert.AreEqual(VideoQuality.HighDefinition, QualityDetector.ParseVideoQuality(
                "hdrip",
                "1920x1042",
                "matrix trailer park (1999)"
                ));
        }

        [Test]
        public void TestParseVideoParamsDescr()
        {
            Assert.AreEqual(VideoQuality.Good, QualityDetector.ParseVideoParamsDescr(
                "704x384 (1.83:1), 23.976 fps, xvid build 65 ~1807 kbps avg, 0.28 bit/pixel",
                "Matrix (1999)"
                ));

            Assert.AreEqual(VideoQuality.Good, QualityDetector.ParseVideoParamsDescr(
                "divx 5, 720х576 (5:4), 25.00 fps, 834 kbps",
                "Matrix (1999)"
                ));

            Assert.AreEqual(VideoQuality.Mediocre, QualityDetector.ParseVideoParamsDescr(
                "1755 кбит/с, 640x368",
                "Matrix (1999)"
                ));

            Assert.AreEqual(VideoQuality.Good, QualityDetector.ParseVideoParamsDescr(
                "h.264, 1072x456 (2,35:1), 24,000 fps, 2569 kbps, 0.219 bit/pixel",
                "Matrix (1999)"
                ));

            Assert.AreEqual(VideoQuality.HighDefinition, QualityDetector.ParseVideoParamsDescr(
                "avc, 1280x696 (1.85:1) at 23.976 fps, avc at 4714 kbps avg, 0.221 bit/pixel",
                "Matrix (1999)"
                ));

            Assert.AreEqual(VideoQuality.HighDefinition, QualityDetector.ParseVideoParamsDescr(
                "1 788 x 1 080, 9 057 kbps, 23.976 fps",
                "Matrix (1999)"
                ));
        }

        [Test]
        public void TestParseVideoQualityDescr()
        {
            Assert.AreEqual(VideoQuality.HighDefinition, QualityDetector.ParseVideoQualityDescr(
                "hdrip",
                "Matrix (1999)"
                ));
            Assert.AreEqual(VideoQuality.HighDefinition, QualityDetector.ParseVideoQualityDescr(
                "blu-ray remux 1080p",
                "Matrix (1999)"
                ));
        }

        [Test]
        public void TestParseSoundQualityDescr()
        {
            Assert.AreEqual(AudioQuality.HighDefinition, QualityDetector.ParseSoundQualityDescr(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("аудио", "русский dd 5.1 (448kbps)")
            }, string.Empty));

            Assert.AreEqual(AudioQuality.HighDefinition, QualityDetector.ParseSoundQualityDescr(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("звук", "1. 224 кбит/сек, 48,0 кгц, 16 бит, 2 канала, ас3")
            }, string.Empty));

            Assert.AreEqual(AudioQuality.Good, QualityDetector.ParseSoundQualityDescr(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("звук", "mpeg audio, 128 кбит/сек, 2 канала, 44,1 кгц")
            }, string.Empty));

            Assert.AreEqual(AudioQuality.Bad, QualityDetector.ParseSoundQualityDescr(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("звук", "mpeg audio, 128 кбит/сек, 2 канала, 44,1 кгц")
            }, "грань будущего / edge of tomorrow (2014) webrip 1080p | звук с ts"));

            Assert.AreEqual(AudioQuality.Bad, QualityDetector.ParseSoundQualityDescr(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("звук", "mpeg audio, 128 кбит/сек, 2 канала, 44,1 кгц")
            }, "пятьдесят оттенков серого / fifty shades of grey (2015) webrip 1080p | звук с camrip"));
        }

        [Test]
        public void TestParseTranslationDescr()
        {
            Assert.AreEqual(Translation.VoiceOver, QualityDetector.ParseTranslationDescr(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("аудио", "russian: ac3, 192 kb/s (2 ch) / english: ac3, 448 kb/s (2 ch)")
            }));
        }
    }
}