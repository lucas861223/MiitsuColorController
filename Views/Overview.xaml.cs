using Microsoft.UI.Xaml.Controls;
using MiitsuColorController.Helper;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MiitsuColorController.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Overview : Page
    {
        public TwitchSocket Twitch_Socket;
        public VTSSocket VTS_Socket;
        public Overview()
        {
            InitializeComponent();
            VTS_Socket = VTSSocket.Instance;
            Twitch_Socket = TwitchSocket.Instance;
        }

        private void ConnectBothSockets(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (!Twitch_Socket.IsConnected)
            {
                Twitch_Socket.Connect();
            }
            if (VTS_Socket.IsNotInUse && (!VTS_Socket.IsConnected || !VTS_Socket.IsAuthorized))
            {
                VTS_Socket.ConnectAndAuthorize();
            }
        }
    }
}
