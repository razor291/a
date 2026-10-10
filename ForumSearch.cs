using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace CSRinEnhanced
{
    internal sealed class ForumSecurityCheckException : Exception
    {
        public ForumSecurityCheckException(string message) : base(message) { }
    }

    internal static class ForumSearch
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
            return client;
        }

        public static string FindTopicUrl(Game game)
        {
            var appId = GetSteamAppId(game);
            var searchUrl = appId != null
                ? "https://cs.rin.ru/forum/search.php?keywords=" + Uri.EscapeDataString(appId) + "&fid%5B%5D=10&sr=topics&sf=firstpost"
                : BuildFallbackSearchUrl(game);

            var html = Download(searchUrl);
            if (LooksLikeSecurityCheck(html))
            {
                throw new ForumSecurityCheckException("Forum security challenge detected.");
            }

            var topics = ExtractTopicLinks(html);
            if (topics.Count == 0 && appId != null)
            {
                // Retry with both the Steam App ID and game name, mirroring the userscript's advanced search.
                var developer = GetDeveloper(game);
                var advancedKeywords = appId + " " + (string.IsNullOrWhiteSpace(developer) ? game.Name : developer);
                html = Download("https://cs.rin.ru/forum/search.php?keywords=" +
                    Uri.EscapeDataString(advancedKeywords) + "&fid%5B%5D=10&sr=topics&sf=firstpost");
                if (LooksLikeSecurityCheck(html))
                {
                    throw new ForumSecurityCheckException("Forum security challenge detected.");
                }
                topics = ExtractTopicLinks(html);
            }

            if (topics.Count == 0)
            {
                // No topic found, or forum markup has changed.
                return null;
            }

            var name = (game.Name ?? string.Empty).Trim();
            var exactOrLikely = topics.FirstOrDefault(t =>
                !string.IsNullOrWhiteSpace(name) &&
                t.Title.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);

            return (exactOrLikely ?? topics[0]).Url;
        }

        public static string BuildFallbackSearchUrl(Game game)
        {
            var terms = GetSteamAppId(game);
            if (string.IsNullOrWhiteSpace(terms))
            {
                terms = game?.Name;
            }
            if (string.IsNullOrWhiteSpace(terms))
            {
                terms = "game";
            }

            return "https://cs.rin.ru/forum/search.php?keywords=" + Uri.EscapeDataString(terms) +
                "&fid%5B%5D=10&sr=topics&sf=firstpost";
        }

        private static string Download(string url)
        {
            using (var response = Client.GetAsync(url).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }

        private static bool LooksLikeSecurityCheck(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return true;
            var text = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
            return text.IndexOf("security check", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("checking your browser", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("verify you are human", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("captcha", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class TopicLink
        {
            public string Title { get; set; }
            public string Url { get; set; }
        }

        private static System.Collections.Generic.List<TopicLink> ExtractTopicLinks(string html)
        {
            var results = new System.Collections.Generic.List<TopicLink>();
            // phpBB topic links commonly use class="topictitle" or a title-cell class.
            var matches = Regex.Matches(html,
                "<a\\b(?=[^>]*\\bclass\\s*=\\s*['\\\"][^'\\\"]*(?:topictitle|titles)[^'\\\"]*['\\\"])[^>]*href\\s*=\\s*['\\\"]([^'\\\"]+)['\\\"][^>]*>(.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var href = WebUtility.HtmlDecode(match.Groups[1].Value);
                var title = WebUtility.HtmlDecode(Regex.Replace(match.Groups[2].Value, "<[^>]+>", string.Empty)).Trim();
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href)) continue;

                Uri absolute;
                if (!Uri.TryCreate(new Uri("https://cs.rin.ru/forum/"), href, out absolute)) continue;
                if (!absolute.Host.Equals("cs.rin.ru", StringComparison.OrdinalIgnoreCase)) continue;
                if (!absolute.AbsolutePath.Contains("viewtopic.php")) continue;

                var cleanUrl = absolute.GetLeftPart(UriPartial.Path) + absolute.Query;
                if (!results.Any(t => t.Url.Equals(cleanUrl, StringComparison.OrdinalIgnoreCase)))
                {
                    results.Add(new TopicLink { Title = title, Url = cleanUrl });
                }
            }
            return results;
        }

        private static string GetSteamAppId(Game game)
        {
            if (game == null) return null;

            // Prefer a Steam store URL present in the game's links.
            if (game.Links != null)
            {
                foreach (var link in game.Links)
                {
                    if (link == null || string.IsNullOrWhiteSpace(link.Url)) continue;
                    var match = Regex.Match(link.Url, @"/app/(\d+)(?:/|$|[?#])", RegexOptions.IgnoreCase);
                    if (match.Success) return match.Groups[1].Value;
                }
            }

            // Some library plugins store the Steam App ID in source metadata. Do not assume
            // Playnite's internal Game.Id is a Steam App ID; fall back to name search instead.
            return null;
        }

        private static string GetDeveloper(Game game)
        {
            if (game == null || game.Developers == null || game.Developers.Count == 0) return null;
            return string.Join(" ", game.Developers.OfType<MetadataNameProperty>().Select(d => d.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
        }
    }
}
