using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MiitsuColorController.Helper;
using MiitsuColorController.Models;
using MiitsuColorController.Views;
using System;
using System.Collections.Generic;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MiitsuColorController
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private ResourceManager _resourceManager = ResourceManager.Instance;
        private AppWindow window;

        public MainWindow()
        {
            InitializeComponent();

            // Get the AppWindow from the XAML Window ("this" is your XAML window)
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            window = AppWindow.GetFromWindowId(myWndId);

            // And then set the icon
            window.SetIcon("Assets/3.0.ico");
            if (_resourceManager.IntResourceDictionary.ContainsKey(ResourceKey.WindowPositionX))
            {
                window.Move(new Windows.Graphics.PointInt32
                {
                    X = _resourceManager.IntResourceDictionary[ResourceKey.WindowPositionX],
                    Y = _resourceManager.IntResourceDictionary[ResourceKey.WindowPositionY]
                });
            }
            if (_resourceManager.IntResourceDictionary.ContainsKey(ResourceKey.WindowWidth))
            {
                window.Resize(new Windows.Graphics.SizeInt32
                {
                    Width = _resourceManager.IntResourceDictionary[ResourceKey.WindowWidth],
                    Height = _resourceManager.IntResourceDictionary[ResourceKey.WindowHeight]
                });
            }

            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = ElementTheme.Dark;
            }

            grid.RequestedTheme = ElementTheme.Dark;
        }

        public void OnExit()
        {
            _resourceManager.IntResourceDictionary[ResourceKey.WindowHeight] = window.Size.Height;
            _resourceManager.IntResourceDictionary[ResourceKey.WindowWidth] = window.Size.Width;
            _resourceManager.IntResourceDictionary[ResourceKey.WindowPositionX] = window.Position.X;
            _resourceManager.IntResourceDictionary[ResourceKey.WindowPositionY] = window.Position.Y;
            _resourceManager.SaveToPersistantStorage();
        }

        private readonly List<(string Tag, Type Page)> _pages = new()
        {
            ("overview", typeof(Overview)),
            ("artmeshtintingfeature", typeof(ArtMeshTintingFeature)),
            ("connection", typeof(Connection))
        };

        private void NavigationViewLoaded(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigated += On_Navigated;
            NavView.SelectedItem = NavView.MenuItems[0];
            NavigationViewNavigate("overview");
        }

        public void NavigationViewNavigate(string navItemTag)
        {
            Type _page = _pages.Find(x => x.Tag == navItemTag).Page;
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            //var preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (!(_page is null) && _page != ContentFrame.CurrentSourcePageType)// && !Type.Equals(preNavPageType, _page))
            {
                ContentFrame.Navigate(_page);
            }
        }

        private void NavigationViewItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                if (ContentFrame.CurrentSourcePageType != typeof(Setting))
                {
                    ContentFrame.Navigate(typeof(Setting));
                }
            }
            else
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavigationViewNavigate(navItemTag);
            }
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            var item = _pages.Find(p => p.Page == e.SourcePageType);

            for (int i = 0; i < _pages.Count; i++)
            {
                if ((((NavigationViewItem)NavView.MenuItems[i]).Tag as string) == item.Tag)
                {
                    NavView.SelectedItem = NavView.MenuItems[i];
                }
            }
            NavView.Header = ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
        }
    }
}