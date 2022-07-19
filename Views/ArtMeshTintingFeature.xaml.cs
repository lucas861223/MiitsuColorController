using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
        public string TagFilter { get; set; } = "";
        public string Filter { get; set; } = "";
        public ObservableCollection<string> FilteredTagList { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> FilteredList { get; } = new ObservableCollection<string>();
        public List<string> SelectedTags = new();
        public List<string> SelectedArtMesh = new();
        public FeatureManager FeatureManager = FeatureManager.Instance;
        private Ellipse _indicationCircle;
        private bool _isInsideCanvas = false;
        private bool _hasClicked = false;
        private ArtMeshTingtingViewModel _context;
        public ArtMeshTintingFeature()
        {
            InitializeComponent();
            Action<List<string>, List<string>, List<string>, List<string>> _populateListsCallBack = PopulateListsCallBack;
            _context = new ArtMeshTingtingViewModel(ColorPickerCanvas, _populateListsCallBack);
            DataContext = _context;
            _indicationCircle = new Ellipse();
            _indicationCircle.StrokeThickness = 1;
            _indicationCircle.Height = 10;
            _indicationCircle.Width = 10;
            _indicationCircle.Stroke = new SolidColorBrush() { Color = Microsoft.UI.Colors.Black };
            //todo 
            //implement test button
            //implement start button 

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
            SelectedArtMesh.Clear();
            foreach (string name in selected)
            {
                ArtMeshNameListView.SelectedItems.Add(name);
            }
            SelectedTags.Clear();
            foreach (string tag in selectedTags)
            {
                TagListView.SelectedItems.Add(tag);
            }
        }

        private void NameFilterChanged(object sender, TextChangedEventArgs e)
        {
            var filtered = _context.ArtMeshNames.Where(item => item.Contains(Filter));

            for (int i = FilteredList.Count - 1; i >= 0; i--)
            {
                if (!filtered.Contains(FilteredList.ElementAt<string>(i)))
                {
                    if (ArtMeshNameListView.SelectedItems.Contains(FilteredList.ElementAt<string>(i)))
                    {
                        SelectedArtMesh.Add(FilteredList.ElementAt<string>(i));
                    }
                    FilteredList.RemoveAt(i);
                }
            }
            int index = -1;
            foreach (var item in filtered)
            {
                // If item in filtered list is not currently in ListView's source collection, add it back in
                if (!FilteredList.Contains(item))
                {
                    index++;
                    FilteredList.Insert(index, item);
                    if (SelectedArtMesh.Contains(item))
                    {
                        ArtMeshNameListView.SelectedItems.Add(item);
                        SelectedArtMesh.Remove(item);
                    }
                }
                else
                {
                    index = FilteredList.IndexOf(item);
                }
            }
        }

        private void TagFilterChanged(object sender, TextChangedEventArgs e)
        {
            var filtered = _context.Tags.Where(item => item.Contains(TagFilter));

            for (int i = FilteredTagList.Count - 1; i >= 0; i--)
            {
                if (!filtered.Contains(FilteredTagList.ElementAt<string>(i)))
                {
                    if (TagListView.SelectedItems.Contains(FilteredTagList.ElementAt<string>(i)))
                    {
                        SelectedTags.Add(FilteredTagList.ElementAt<string>(i));
                    }
                    FilteredTagList.RemoveAt(i);
                }
            }
            int index = -1;
            foreach (var item in filtered)
            {
                // If item in filtered list is not currently in ListView's source collection, add it back in
                if (!FilteredTagList.Contains(item))
                {
                    index++;
                    FilteredTagList.Insert(index, item);
                    if (SelectedTags.Contains(item))
                    {
                        TagListView.SelectedItems.Add(item);
                        SelectedTags.Remove(item);
                    }
                }
                else
                {
                    index = FilteredTagList.IndexOf(item);
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
            double width = ColorPickerCanvas.ActualWidth / 100;
            double thickness = (ColorPickerCanvas.ActualWidth - 100) / 100;
            Line tmpLine;
            foreach (UIElement line in ColorPickerCanvas.Children)
            {
                if (line.GetType() == typeof(Line))
                {
                    tmpLine = (Line)line;
                    tmpLine.Y2 = ColorPickerCanvas.ActualHeight;
                    tmpLine.X1 = width * (int)tmpLine.Tag;
                    tmpLine.X2 = width * (int)tmpLine.Tag;
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
            if (_isInsideCanvas && e.GetCurrentPoint(ColorPickerCanvas).Properties.IsLeftButtonPressed)
            {
                ColorPickerCanvas_PointerPressed(sender, e);
            }
        }

        private void ColorPickerCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isInsideCanvas = false;
        }

        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (string tag in e.AddedItems)
            {
                _context.SelectedTag.Add(tag);
            }
            foreach (string tag in e.RemovedItems)
            {
                _context.SelectedTag.Remove(tag);
            }
        }

        private void ArtMeshNameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (string artmesh in e.AddedItems)
            {
                _context.SelectedArtMesh.Add(artmesh);
            }
            foreach (string artmesh in e.RemovedItems)
            {
                _context.SelectedArtMesh.Remove(artmesh);
            }
        }
    }
}
