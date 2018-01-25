using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CsQuery;
using NLog;
using Torronto.DAL.Models;

namespace Torronto.BLL.Detector
{
    public class QualityResult
    {
        public VideoQuality VideoQuality { get; set; }
        public AudioQuality AudioQuality { get; set; }
        public Translation TranslationQuality { get; set; }
    }

    public class QualityDetector : IQualityDetector
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static readonly List<Tuple<string, VideoQuality>> _qualities = new List<Tuple<string, VideoQuality>>
        {
            new Tuple<string, VideoQuality>("bdrip", VideoQuality.HighDefinition),
            new Tuple<string, VideoQuality>("hdrip", VideoQuality.HighDefinition),
            new Tuple<string, VideoQuality>("bdremux", VideoQuality.HighDefinition),
            new Tuple<string, VideoQuality>("hdtvrip", VideoQuality.HighDefinition),
            new Tuple<string, VideoQuality>("webrip", VideoQuality.HighDefinition),
            new Tuple<string, VideoQuality>("web-dl", VideoQuality.HighDefinition),
            new Tuple<string, VideoQuality>("webdl", VideoQuality.HighDefinition),
            new Tuple<string, VideoQuality>("blu-ray", VideoQuality.HighDefinition),
            new Tuple<string, VideoQuality>("hd", VideoQuality.HighDefinition),//last rule

            new Tuple<string, VideoQuality>("tvrip", VideoQuality.Good),
            new Tuple<string, VideoQuality>("satrip", VideoQuality.Good),
            new Tuple<string, VideoQuality>("dvdrip", VideoQuality.Good),
            new Tuple<string, VideoQuality>("dvdscr", VideoQuality.Good),
            new Tuple<string, VideoQuality>("vcdrip", VideoQuality.Good),
            new Tuple<string, VideoQuality>("dvb", VideoQuality.Good),
            new Tuple<string, VideoQuality>("dvd", VideoQuality.Good),//last rule

            new Tuple<string, VideoQuality>("camrip", VideoQuality.Bad),
            new Tuple<string, VideoQuality>("ts", VideoQuality.Bad),
            new Tuple<string, VideoQuality>("telecine", VideoQuality.Bad),
            new Tuple<string, VideoQuality>("vhsrip", VideoQuality.Bad),
        };

        private static readonly List<Tuple<int, VideoQuality>> _widths = new List<Tuple<int, VideoQuality>>
        {
            new Tuple<int, VideoQuality>(1280, VideoQuality.HighDefinition),
            new Tuple<int, VideoQuality>(700, VideoQuality.Good),
            new Tuple<int, VideoQuality>(500, VideoQuality.Mediocre),
            new Tuple<int, VideoQuality>(1, VideoQuality.Bad),
            new Tuple<int, VideoQuality>(0, VideoQuality.Unknown),
        };

        private static List<Tuple<string, AudioQuality>> _soundQualities = new List<Tuple<string, AudioQuality>>
        {
            new Tuple<string, AudioQuality>("ac3", AudioQuality.HighDefinition),
            new Tuple<string, AudioQuality>("ас3", AudioQuality.HighDefinition),//russian letters
            new Tuple<string, AudioQuality>("ac-3", AudioQuality.HighDefinition),
            new Tuple<string, AudioQuality>("dolby", AudioQuality.HighDefinition),
            new Tuple<string, AudioQuality>("dd ", AudioQuality.HighDefinition),
            new Tuple<string, AudioQuality>("dts", AudioQuality.HighDefinition),

            new Tuple<string, AudioQuality>("aac", AudioQuality.Good),
            new Tuple<string, AudioQuality>("mp3", AudioQuality.Good),
            new Tuple<string, AudioQuality>("mpeg", AudioQuality.Good),
        };

        private static readonly List<Tuple<string, Translation>> _translations = new List<Tuple<string, Translation>>
        {
            new Tuple<string, Translation>("дубл", Translation.Dub),

            new Tuple<string, Translation>("любител", Translation.VoiceOver),
            new Tuple<string, Translation>("закадр", Translation.VoiceOver),
            new Tuple<string, Translation>("одноголос", Translation.VoiceOver),
            new Tuple<string, Translation>("двухголос", Translation.VoiceOver),
            new Tuple<string, Translation>("многоголос", Translation.VoiceOver),
            new Tuple<string, Translation>("автор", Translation.VoiceOver),
            new Tuple<string, Translation>("russian", Translation.VoiceOver),
            new Tuple<string, Translation>("русский", Translation.VoiceOver),

            new Tuple<string, Translation>("субтитр", Translation.Subtitles),
        };

        private static readonly string[] _trailerDetectors =
        {
            "трейлер",
            "трэйлер",
            "тизер",
            "trailer"
        };

        private static readonly string[] _badAudioFactors =
        {
            "звук с ts",
            "camrip"
        };

        public VideoQuality ParseVideoQuality(string qualityDescr, string videoDescr, string title)
        {
            var qualityResult = ParseVideoQualityDescr(qualityDescr, title);
            var videoResult = ParseVideoParamsDescr(videoDescr, title);

            VideoQuality result;

            if (videoResult == VideoQuality.Unknown)
            {
                result = qualityResult;
            }
            else
            {
                result = qualityResult < videoResult ? qualityResult : videoResult;
            }

            if (qualityResult == VideoQuality.Unknown) _logger.Warn("QQ FAIL '{0}'", qualityDescr);
            if (videoResult == VideoQuality.Unknown) _logger.Warn("VQ FAIL '{0}'", videoDescr);

            var titleParts = title
                .Split('|')
                .Skip(1)
                .ToArray();

            if (_trailerDetectors.Any(detector => titleParts.Any(s => s.Contains(detector))))
            {
                result = VideoQuality.Unknown;
            }

            return result;
        }

        public VideoQuality ParseVideoQualityDescr(string qualityDescr, string title)
        {
            var qualityResult = _qualities
                .Where(q => qualityDescr != null && qualityDescr.StartsWith(q.Item1))
                .Select(q => q.Item2)
                .FirstOrDefault();

            return qualityResult;
        }

        public VideoQuality ParseVideoParamsDescr(string videoDescr, string title)
        {
            var width = 0;
            var resRegex = new Regex("(\\d{3,4})\\s*[^,.()/]\\s*(\\d{3,4})");
            var descr = Regex.Replace(videoDescr, "(\\d)\\s+(\\d)", "$1$2");
            var resMatch = resRegex.Match(descr);

            if (resMatch.Success)
            {
                width = Convert.ToInt32(resMatch.Groups[1].Value);
            }

            var videoResult = _widths
                .Where(w => w.Item1 <= width)
                .Select(w => w.Item2)
                .FirstOrDefault();

            return videoResult;
        }

        public AudioQuality ParseSoundQualityDescr(List<Tuple<string, string>> descrLines, string title)
        {
            var result = AudioQuality.Unknown;
            var whitelist = new[] { "аудио", "звук" };

            var processedLines = descrLines
                .Where(dl => whitelist.Any(white => dl.Item1.StartsWith(white)) && !dl.Item1.Contains("кодек"))
                .Select(dl => dl.Item2)
                .ToList();

            foreach (var processedLine in processedLines)
            {
                foreach (var quality in _soundQualities)
                {
                    if (processedLine.Contains(quality.Item1) && quality.Item2 > result)
                    {
                        result = quality.Item2;
                        break;
                    }
                }
            }

            if (_badAudioFactors.Any(title.Contains))
            {
                result = AudioQuality.Bad;
            }

            if (result == AudioQuality.Unknown)
            {
                _logger.Warn("AQR {0}", result.ToString());

                foreach (var processedLine in processedLines)
                {
                    _logger.Warn("AQ {0}", processedLine);
                }
            }

            return result;
        }

        public Translation ParseTranslationDescr(List<Tuple<string, string>> descrLines)
        {
            var result = Translation.Unknown;
            var whitelist = new[] { "озвучивание", "перевод", "аудио", "звук" };

            var processedLines = descrLines
                .Where(dl => whitelist.Any(white => dl.Item1.StartsWith(white)))
                .Select(dl => dl.Item2)
                .ToList();

            foreach (var processedLine in processedLines)
            {
                foreach (var quality in _translations)
                {
                    if (processedLine.Contains(quality.Item1) && quality.Item2 > result)
                    {
                        result = quality.Item2;
                    }
                }
            }

            if (result == Translation.Unknown)
            {
                _logger.Warn("TQR {0}", result.ToString());

                foreach (var processedLine in processedLines)
                {
                    _logger.Warn("TQ {0}", processedLine);
                }
            }

            return result;
        }

        public QualityResult Detect(CQ dom)
        {
            var descrLines = GetDescriptionLines(dom);
            var title = (dom["title"].Select(x => x.InnerText).FirstOrDefault() ?? string.Empty).ToLower();

            var qdKeys = new[]
            {
                "ачество",
                "тип релиза"
            };

            var qualityDescr = qdKeys
                .Select(k => descrLines.FirstOrDefault(dl => dl.Item1.Contains(k)))
                .Where(descr => descr != null)
                .Select(descr => descr.Item2)
                .FirstOrDefault() ?? string.Empty;

            var videoDescr = descrLines
                .Where(dl => dl.Item1.StartsWith("видео") && !dl.Item1.Contains("кодек"))
                .Select(descr => descr.Item2)
                .FirstOrDefault() ?? string.Empty;

            return new QualityResult
            {
                VideoQuality = ParseVideoQuality(qualityDescr, videoDescr, title),
                AudioQuality = ParseSoundQualityDescr(descrLines, title),
                TranslationQuality = ParseTranslationDescr(descrLines)
            };
        }

        public static List<Tuple<string, string>> GetDescriptionLines(CQ dom)
        {
            var list = new List<Tuple<string, string>>();

            foreach (var elemB in dom["b"])
            {
                var str = string.Empty;
                var elem = elemB;
                var title = elemB.InnerText;

                if (string.IsNullOrEmpty(title)) continue;

                while (elem != null && elem.NodeName != "BR")
                {
                    if (elem.ChildrenAllowed)
                    {
                        var enumerable = elem.ChildElements
                            .Where(x => x.ChildrenAllowed)
                            .Select(x => x.InnerText);

                        str += elem.InnerText + string.Join(string.Empty, enumerable);
                    }
                    else
                    {
                        str += elem.ToString();
                    }

                    elem = elem.NextSibling;
                }

                str = Regex.Replace(str, "<.*?>", string.Empty);

                if (str.Contains(":"))
                {
                    str = str.Remove(0, str.IndexOf(":", StringComparison.Ordinal));
                }

                title = title
                    .Trim(' ', ':', '\n')
                    .ToLower();

                str = str
                    .Trim(' ', ':', '\n')
                    .ToLower();

                list.Add(new Tuple<string, string>(title, str));
            }

            return list;
        }

    }

    public interface IQualityDetector
    {
        QualityResult Detect(CQ dom);
    }
}