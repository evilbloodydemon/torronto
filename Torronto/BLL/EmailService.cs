using NLog;
using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;
using Torronto.DAL.Models;

namespace Torronto.BLL
{
    public class EmailService
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static readonly string _smtpHost = ConfigurationManager.AppSettings["Mail.Smtp.Host"];
        private static readonly int _smtpPort = Convert.ToInt32(ConfigurationManager.AppSettings["Mail.Smtp.Port"]);
        private static readonly string _smtpUsername = ConfigurationManager.AppSettings["Mail.Smtp.Username"];
        private static readonly string _smtpPassword = ConfigurationManager.AppSettings["Mail.Smtp.Password"];
        private static readonly bool _smtpEnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["Mail.Smtp.EnableSsl"]);
        private static readonly string _contactEmailFrom = ConfigurationManager.AppSettings["Mail.EmailFrom"];

        public void NotifyUserAboutMovie(User user, Torrent torrent, Movie movie)
        {
            _logger.Info("Sending movie notification to user #{0}, torrent #{1}", user.ID, torrent.ID);

            try
            {
                var subject = "[Torronto] " + movie.Title;
                var originalTitle = string.IsNullOrEmpty(movie.OriginalTitle) ? string.Empty : $"({movie.OriginalTitle}) ";
                var torrentUrl = "http://torronto.evilbloodydemon.ru/#!/torrents/" + torrent.ID;
                var body = new StringBuilder()
                    .AppendLine()
                    .AppendLine($"Новый торрент для фильма {movie.Title} {originalTitle}доступен на нашем сайте.<br>")
                    .AppendLine($@"<a href=""{torrentUrl}"">{torrent.Title}</a> ({torrent.Size} Mb)<br>");

                SendMail(user, subject, body);
            }
            catch (Exception ex)
            {
                _logger.Error("MAIL", ex);
            }
        }

        public void NotifyUserAboutTorrent(User user, Torrent torrent)
        {
            _logger.Info("Sending torrent notification to user #{0}, torrent #{1}", user.ID, torrent.ID);

            try
            {
                var subject = "[Torronto] " + torrent.Title;

                var torrentUrl = "http://torronto.evilbloodydemon.ru/#!/torrents/" + torrent.ID;
                var body = new StringBuilder()
                    .AppendLine()
                    .AppendLine("Торрент, на который вы подписаны, обновился.<br>")
                    .AppendLine($"<a href=\"{torrentUrl}\">{torrent.Title}</a><br>");

                SendMail(user, subject, body);
            }
            catch (Exception ex)
            {
                _logger.Error("MAIL", ex);
            }
        }

        public static void SendMail(User user, string subject, StringBuilder body)
        {
            using (var client = new SmtpClient(_smtpHost, _smtpPort))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = _smtpEnableSsl;

                var mail = new MailMessage(_contactEmailFrom, user.Email, subject, body.ToString())
                {
                    IsBodyHtml = true
                };

                ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;

                client.Send(mail);
            }
        }
    }
}