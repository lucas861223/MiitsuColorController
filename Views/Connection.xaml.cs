using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MiitsuColorController.Helper;
using MiitsuColorController.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MiitsuColorController.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Connection : Page
    {
        public VTSSocket VTS_Socket;
        public TwitchSocket Twitch_Socket;

        public Connection()
        {
            InitializeComponent();
            VTS_Socket = VTSSocket.Instance;
            Twitch_Socket = TwitchSocket.Instance;
        }

        private void ConnectToVTubeStudio(object sender, RoutedEventArgs e)
        {
            if (VTS_Socket.IsNotInUse)
            {
                VTS_Socket.Connect();
            }
            else
            {
                VTS_Socket.Disconnect();
            }
        }

        private void ConnectToTwitch(object sender, RoutedEventArgs e)
        {
            if (Twitch_Socket.IsNotInUse)
            {
                Twitch_Socket.Connect();
            }
            else
            {
                Twitch_Socket.Disconnect();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ResourceManager manager = ResourceManager.Instance;
            manager.BoolResourceDictionary[ResourceKey.ConnectTwitchOnStart] = Twitch_Socket.ConnectOnStartup;
            manager.BoolResourceDictionary[ResourceKey.ConnectVTSOnStart] = VTS_Socket.ConnectOnStartup;
            manager.BoolResourceDictionary[ResourceKey.ReconnectTwitchOnError] = Twitch_Socket.AutoReconnect;
            manager.BoolResourceDictionary[ResourceKey.ReconnectVTSOnError] = VTS_Socket.AutoReconnect;
            base.OnNavigatedFrom(e);
        }
    }
}