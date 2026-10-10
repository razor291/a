using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace CSRinEnhanced
{
    public class CSRinEnhancedPlugin : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("6fbbdf72-cc6b-4d7a-88bd-cf93c4c18f01");

        public CSRinEnhancedPlugin(IPlayniteAPI api) : base(api)
        {
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                SourceName = "CSRinEnhanced",
                ElementList = new List<string> { "CSRinButton" }
            });
        }

        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args == null || args.Name != "CSRinButton")
            {
                return null;
            }

            return new CSRinButtonControl(this);
        }

        public void SearchForGame(Playnite.SDK.Models.Game game)
        {
            if (game == null)
            {
                PlayniteApi.Dialogs.ShowMessage("Select a game first.", "CS.RIN.RU Enhanced");
                return;
            }

            try
            {
                var url = ForumSearch.FindTopicUrl(game);
                if (string.IsNullOrWhiteSpace(url))
                {
                    PlayniteApi.Dialogs.ShowMessage(
                        "A forum topic could not be identified. Opening a CS.RIN.RU search instead.",
                        "CS.RIN.RU Enhanced");
                    url = ForumSearch.BuildFallbackSearchUrl(game);
                }

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
                    !uri.Host.Equals("cs.rin.ru", StringComparison.OrdinalIgnoreCase))
                {
                    PlayniteApi.Dialogs.ShowMessage("The forum returned an invalid or unexpected URL.", "CS.RIN.RU Enhanced");
                    return;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch (ForumSecurityCheckException ex)
            {
                Logger.Warn(ex, "CS.RIN.RU search was blocked by a security check.");
                PlayniteApi.Dialogs.ShowMessage(
                    "CS.RIN.RU presented a security check, so automatic topic matching was stopped. Opening a search page instead.",
                    "CS.RIN.RU Enhanced");
                OpenFallback(game);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to search CS.RIN.RU.");
                PlayniteApi.Dialogs.ShowMessage(
                    "The CS.RIN.RU lookup failed. This may be a network issue or a change to the forum page. Opening a search page instead.",
                    "CS.RIN.RU Enhanced");
                OpenFallback(game);
            }
        }

        private void OpenFallback(Playnite.SDK.Models.Game game)
        {
            try
            {
                var url = ForumSearch.BuildFallbackSearchUrl(game);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to open fallback forum search.");
                PlayniteApi.Dialogs.ShowMessage("Could not open the CS.RIN.RU search page.", "CS.RIN.RU Enhanced");
            }
        }
    }
}
