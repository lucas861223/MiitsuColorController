using Microsoft.UI.Xaml.Controls;
using MiitsuColorController.Helper;
using MiitsuColorController.ViewModel;

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
        public FeatureManager Feature_Manager = FeatureManager.Instance;
        public ResourceManager Resource_Manager = ResourceManager.Instance;
        public ArtMeshTingtingViewModel dataContext = new();
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

        public void NavigateToColorTintingFeature(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ((App)App.Current).m_window.NavigationViewNavigate("artmeshtintingfeature");
        }
    }
}
