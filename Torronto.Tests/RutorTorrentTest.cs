using CsQuery;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Torronto.BLL;
using Torronto.BLL.Detector;
using Torronto.DAL.Models;

namespace Torronto.Tests
{
    [TestFixture]
    public class RutorTorrentTest
    {



        [Test]
        public void TestGetDescriptionLines()
        {
            Config.OutputFormatter = OutputFormatters.HtmlEncodingNone;

            CQ dom;

            dom = CQ.Create(@"
                <a target=""_blank"" href=""http://sendfile.su/1010141"">
                <img src=""http://i48.fastpic.ru/big/2013/0630/d8/9461603ab397a272147937cc0bd705d8.png"">
                </a>
                <a target=""_blank"" href=""http://www.kinopoisk.ru/film/94589/"">
                <img src=""http://www.kinopoisk.ru/rating/94589.gif"">
                </a>
                <br>
                <br>
                <b>Качество</b>
                : BDRip-AVC [
                <span style=""color:gray;"">
                <b>LES_GENDARMES_HDCLUB</b>
                </span>
                ]
                <br>
                <b>Формат</b>
                : MKV
                <br>
                <b>Видео кодек</b>
                : H.264
                <br>
                <b>Аудио кодек</b>
                : AAC
                <br>
                <b>Видео</b>
                : 728x310 (2.35:1), 24.000 fps, H264 ~ 1 100 Kbps, 0.203 bit/pixel
                <br>
                <b>Аудио</b>
                : 48 KHz / 24 KHz, AAC HE-AAC / LC, 2/0 (L,R) ch, ~64 kbps
                <hr>
                <br>
                <b>Раздача от</b>
                :
            ");

            var lines = QualityDetector.GetDescriptionLines(dom);

            Assert.IsTrue(lines.Any(x => x.Item1 == "аудио" && x.Item2 == "48 khz / 24 khz, aac he-aac / lc, 2/0 (l,r) ch, ~64 kbps"));
            Assert.IsTrue(lines.Any(x => x.Item1 == "видео" && x.Item2 == "728x310 (2.35:1), 24.000 fps, h264 ~ 1 100 kbps, 0.203 bit/pixel"));
            Assert.IsTrue(lines.Any(x => x.Item1 == "качество" && x.Item2.StartsWith("bdrip-avc [")));
        }

        [Test]
        public void TestGetDescriptionLines2()
        {
            Config.OutputFormatter = OutputFormatters.HtmlEncodingNone;

            CQ dom;

            dom = CQ.Create(@"
                <li>
                <b>
                Качество:
                <span style=""color:brown;"">BDRip-AVC</span>
                </b>
                <br>
                </li>
            ");

            var lines = QualityDetector.GetDescriptionLines(dom);

            Assert.IsTrue(lines.Any(x => x.Item1 == "качество" && x.Item2 == "bdrip-avc"));
        }

    }
}