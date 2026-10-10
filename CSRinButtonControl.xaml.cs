using System.Windows;
using System.Windows.Controls;
using Playnite.SDK.Controls;
using Playnite.SDK.Models;

namespace CSRinEnhanced
{
    public partial class CSRinButtonControl : PluginUserControl
    {
        private readonly CSRinEnhancedPlugin plugin;

        public CSRinButtonControl(CSRinEnhancedPlugin plugin)
        {
            this.plugin = plugin;
            InitializeComponent();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Game game = GameContext as Game;
            plugin.SearchForGame(game);
        }
    }
}
