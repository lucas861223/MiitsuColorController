using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using MiitsuColorController.Helper;
using MiitsuColorController.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MiitsuColorController.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArtMeshTintingFeature : Page
    {
        public VTSSocket VTS_Socket = VTSSocket.Instance;
        public TwitchSocket Twitch_Socket = TwitchSocket.Instance;
        public string Filter { get; set; } = "";
        public ObservableCollection<string> FilteredTagList { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> FilteredList { get; } = new ObservableCollection<string>();
        private Ellipse _indicationCircle;
        private Line _indicationLine;
        private Polygon _indicationTriangle;
        private bool _isInsideCanvas = false;
        private bool _hasClicked = false;
        private bool _isInsideRect = false;
        private bool _hasClickedRect = false;
        private ArtMeshTintingViewModel _context;
        private FeatureManager _featureManager = FeatureManager.Instance;
        private bool _isClickTesting = false;

        public ArtMeshTintingFeature()
        {
            Action<List<string>, List<string>, List<string>, List<string>> _populateListsCallBack = PopulateListsCallBack;
            _context = new ArtMeshTintingViewModel(_populateListsCallBack);
            InitializeComponent();
            _indicationCircle = new Ellipse();
            _indicationCircle.StrokeThickness = 1;
            _indicationCircle.Height = 10;
            _indicationCircle.Width = 10;
            _indicationCircle.Stroke = new SolidColorBrush() { Color = Microsoft.UI.Colors.Black };
            _indicationTriangle = new Polygon();
            _indicationTriangle.Fill = new SolidColorBrush() { Color = Microsoft.UI.Colors.White };
            PointCollection points = new PointCollection();
            points.Add(new Point(-10, 5));
            points.Add(new Point(0, 0));
            points.Add(new Point(-10, -5));
            _indicationTriangle.Points = points;
            _indicationLine = new Line();
            _indicationLine.StrokeThickness = 1.5;
            _indicationLine.Stroke = new SolidColorBrush() { Color = Microsoft.UI.Colors.White };
            Loaded += (sender, e) =>
            {
                _context.StartLoadingModel(ColorPickerCanvas);
            };
            //todo
            //implement test button
            //implement start button
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_context.IsAutoTesting)
            {
                FeatureManager.Instance.StopTesting();
            }
            base.OnNavigatedFrom(e);
        }

        private void PopulateListsCallBack(List<string> names, List<string> tags, List<string> selected, List<string> selectedTags)
        {
            FilteredList.Clear();
            foreach (string artmesh in names)
            {
                FilteredList.Add(artmesh);
            }
            FilteredTagList.Clear();
            foreach (string tag in tags)
            {
                FilteredTagList.Add(tag);
            }
            _context.SelectedButFilteredName.Clear();
            foreach (string name in selected)
            {
                ArtMeshNameListView.SelectedItems.Add(name);
            }
            foreach (string tag in selectedTags)
            {
                TagListView.SelectedItems.Add(tag);
            }
        }


        private void FilterChanged(object sender, TextChangedEventArgs e)
        {
            FilterChanged(_context.ArtMeshNames, FilteredList, _context.SelectedButFilteredName, ArtMeshNameListView);
            FilterChanged(_context.Tags, FilteredTagList, _context.SelectedButFilteredTag, TagListView);
        }

        private void FilterChanged(List<string> fullList,
                                   ObservableCollection<string> filteredList,
                                   List<string> selectedButFilteredList,
                                   ListView listView)
        {
            var filtered = fullList.Where(item => item.Contains(Filter));

            for (int i = filteredList.Count - 1; i >= 0; i--)
            {
                if (!filtered.Contains(filteredList.ElementAt<string>(i)))
                {
                    if (listView.SelectedItems.Contains(filteredList.ElementAt<string>(i)))
                    {
                        selectedButFilteredList.Add(filteredList.ElementAt<string>(i));
                    }
                    filteredList.RemoveAt(i);
                }
            }
            int index = -1;
            foreach (var item in filtered)
            {
                // If item in filtered list is not currently in ListView's source collection, add it back in
                if (!filteredList.Contains(item))
                {
                    index++;
                    filteredList.Insert(index, item);
                    if (selectedButFilteredList.Contains(item))
                    {
                        listView.SelectedItems.Add(item);
                        selectedButFilteredList.Remove(item);
                    }
                }
                else
                {
                    index = filteredList.IndexOf(item);
                }
            }
        }

        private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (args.NewValue != (int)args.NewValue)
            {
                sender.Value = Math.Floor(args.NewValue);
            }
            else
            {
                sender.Value = args.NewValue;
            }
        }

        private void ColorPickerCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double thickness = ColorPickerCanvas.ActualWidth / 200d;
            Line tmpLine;
            foreach (UIElement line in ColorPickerCanvas.Children)
            {
                if (line.GetType() == typeof(Line))
                {
                    tmpLine = (Line)line;
                    tmpLine.Y2 = ColorPickerCanvas.ActualHeight;
                    tmpLine.X1 = thickness * ((int)tmpLine.Tag * 2 + 1);
                    tmpLine.X2 = thickness * ((int)tmpLine.Tag * 2 + 1);
                    tmpLine.StrokeThickness = thickness * 2.5;
                }
                else
                {
                    _indicationCircle.SetValue(Canvas.LeftProperty, Canvas.GetLeft(_indicationCircle) * (e.NewSize.Width / e.PreviousSize.Width));
                    _indicationCircle.SetValue(Canvas.TopProperty, Canvas.GetTop(_indicationCircle) * (e.NewSize.Height / e.PreviousSize.Height));
                }
            }
        }

        private void ColorPickerCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isInsideCanvas = true;
            Point mousePosition = e.GetCurrentPoint(ColorPickerCanvas).Position;
            if (!_hasClicked)
            {
                ColorPickerCanvas.Children.Add(_indicationCircle);
                _hasClicked = true;
            }
            _indicationCircle.SetValue(Canvas.LeftProperty, mousePosition.X - 5);
            _indicationCircle.SetValue(Canvas.TopProperty, mousePosition.Y - 5);
            _context.UpdateColor((float)(360.0 * (mousePosition.X / ColorPickerCanvas.ActualWidth)),
                                 (float)(mousePosition.Y / ColorPickerCanvas.ActualHeight));
            _context.UpdateHS(360.0 * (mousePosition.X / ColorPickerCanvas.ActualWidth),
                              (float)(mousePosition.Y / ColorPickerCanvas.ActualHeight));
        }

        private void ColorPickerCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isInsideCanvas)
            {
                Point mousePosition = e.GetCurrentPoint(ColorPickerCanvas).Position;
                _isInsideCanvas = mousePosition.X >= 0 && mousePosition.Y >= 0 &&
                                  mousePosition.X <= ColorPickerCanvas.ActualWidth &&
                                  mousePosition.Y <= ColorPickerCanvas.ActualHeight;
            }
            if (_isInsideCanvas)
            {
                _context.SetDescription(true, "ColorPickerCanvas");
                if (e.GetCurrentPoint(ColorPickerCanvas).Properties.IsLeftButtonPressed)
                {
                    ColorPickerCanvas_PointerPressed(sender, e);
                }
            }
        }

        private void ColorPickerCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isInsideCanvas = false;
            _context.SetDescription(false, null);
        }

        private void RectColor_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_hasClickedRect)
            {
                _indicationLine.X2 = RectColor.ActualWidth;
                _indicationLine.SetValue(Canvas.TopProperty, Canvas.GetTop(_indicationLine) * (e.NewSize.Height / e.PreviousSize.Height));
                _indicationTriangle.SetValue(Canvas.TopProperty, Canvas.GetTop(_indicationTriangle) * (e.NewSize.Height / e.PreviousSize.Height));
            }
        }

        private void RectColor_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isInsideRect = true;
            Point mousePosition = e.GetCurrentPoint(RectColor).Position;
            if (!_hasClickedRect)
            {
                RectColor.Children.Add(_indicationTriangle);
                RectColor.Children.Add(_indicationLine);
                _indicationTriangle.SetValue(Canvas.LeftProperty, 1);
                _indicationLine.X1 = 0;
                _indicationLine.X2 = RectColor.ActualWidth;
                _hasClickedRect = true;
            }
            _indicationTriangle.SetValue(Canvas.TopProperty, mousePosition.Y);
            _indicationLine.SetValue(Canvas.TopProperty, mousePosition.Y);
            _context.UpdateV((float)(1 - mousePosition.Y / RectColor.ActualHeight));
        }

        private void RectColor_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isInsideRect)
            {
                Point mousePosition = e.GetCurrentPoint(RectColor).Position;
                _isInsideRect = mousePosition.X >= 0 && mousePosition.Y >= 0 &&
                                  mousePosition.X <= RectColor.ActualWidth &&
                                  mousePosition.Y <= RectColor.ActualHeight;
            }
            if (_isInsideRect)
            {
                _context.SetDescription(true, "RectColor");
                if (e.GetCurrentPoint(RectColor).Properties.IsLeftButtonPressed)
                {
                    RectColor_PointerPressed(sender, e);
                }
            }
        }

        private void RectColor_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isInsideRect = false;
            _context.SetDescription(false, null);
        }

        private void StartClickTesting(object sender, RoutedEventArgs e)
        {
            _isClickTesting = true;
            _context.Clicktest();
        }
    }
}