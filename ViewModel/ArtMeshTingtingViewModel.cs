using System;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml;
using MiitsuColorController.Helper;
using System.Threading.Tasks;
using MiitsuColorController.Models;

namespace MiitsuColorController.ViewModel
{
    public class ArtMeshTingtingViewModel : ObservableObject
    {
        private VTSSocket _vtsSocket = VTSSocket.Instance;
        private int _messageHandlingMethod = 0;
        public int MessageHandlingMethod { get { return _messageHandlingMethod; } set { _messageHandlingMethod = value; OnPropertyChanged(nameof(MessageHandlingMethod)); } }
        private ArtmeshColoringSetting _setting = new();
        public int Interpolation
        {
            get { return _setting.Interpolation; }
            set
            {
                _setting.Interpolation = value;
                OnPropertyChanged(nameof(Interpolation));
                if (IsTesting)
                {
                    _featureManager.UpdateTestingParameters(_setting);
                }
            }
        }
        public int Duration
        {
            get { return _setting.Duration; }
            set
            {
                _setting.Duration = value;
                OnPropertyChanged(nameof(Duration));
                if (IsTesting)
                {
                    _featureManager.UpdateTestingParameters(_setting);
                }
            }
        }
        public string GreenEmote { get { return _setting.GreenEmote; } set { _setting.GreenEmote = value; OnPropertyChanged(nameof(GreenEmote)); } }
        public string RedEmote { get { return _setting.RedEmote; } set { _setting.RedEmote = value; OnPropertyChanged(nameof(RedEmote)); } }
        public string BlueEmote { get { return _setting.BlueEmote; } set { _setting.BlueEmote = value; OnPropertyChanged(nameof(BlueEmote)); } }
        public bool Activated { get { return _setting.Activated; } set { _setting.Activated = value; OnPropertyChanged(nameof(Activated)); } }
        private bool _isTesting = false;
        public bool IsTesting { get { return _isTesting; } set { _isTesting = value; OnPropertyChanged(nameof(IsTesting)); } }
        public RoutedEventHandler RefreshCommand { get { return LoadModel; } }
        public RoutedEventHandler SaveCommand { get { return SaveModelSetting; } }
        public RoutedEventHandler TestCommand { get { return Test; } }
        public RoutedEventHandler ActivateCommand { get { return Activate; } }
        public SelectionChangedEventHandler NameSelectionCommand { get { return ArtMeshNameListView_SelectionChanged; } }
        public SelectionChangedEventHandler TagSelectionCommand { get { return TagListView_SelectionChanged; } }
        private ResourceManager _resourceManager = ResourceManager.Instance;
        private FeatureManager _featureManager = FeatureManager.Instance;
        public int MinimumS
        {
            get { return _setting.MinimumS; }
            set
            {
                if (IsInt(value) && (int)value <= MaximumS)
                {
                    _setting.MinimumS = value;
                    UpdateCanvas();
                    if (IsTesting)
                    {
                        _featureManager.UpdateTestingParameters(_setting);
                    }
                }
                OnPropertyChanged(nameof(MinimumS));
            }
        }
        public int MaximumS
        {
            get { return _setting.MaximumS; }
            set
            {
                if (IsInt(value) && (int)value >= MinimumS)
                {
                    _setting.MaximumS = value;
                    UpdateCanvas();
                    if (IsTesting)
                    {
                        _featureManager.UpdateTestingParameters(_setting);
                    }
                }
                OnPropertyChanged(nameof(MaximumS));
            }
        }
        public int MinimumV
        {
            get { return _setting.MinimumV; }
            set
            {
                if (IsInt(value) && (int)value <= MaximumV)
                {
                    _setting.MinimumV = value;
                    UpdateColor();
                    if (IsTesting)
                    {
                        _featureManager.UpdateTestingParameters(_setting);
                    }
                }
                OnPropertyChanged(nameof(MinimumV));
            }
        }
        public int MaximumV
        {
            get { return _setting.MaximumV; }
            set
            {
                if (IsInt(value) && (int)value >= MinimumV)
                {
                    _setting.MaximumV = value;
                    UpdateColor();
                    if (IsTesting)
                    {
                        _featureManager.UpdateTestingParameters(_setting);
                    }
                }
                OnPropertyChanged(nameof(MaximumV));
            }
        }
        private Color _firstStopColor;
        public Color FirstStopColor
        {
            get { return _firstStopColor; }
            set
            {
                _firstStopColor = value;
                OnPropertyChanged(nameof(FirstStopColor));
            }
        }
        private Color _secondStopColor;
        public Color SecondStopColor
        {
            get { return _secondStopColor; }
            set
            {
                _secondStopColor = value;
                OnPropertyChanged(nameof(SecondStopColor));
            }
        }
        private Color _thirdStopColor;
        public Color ThirdStopColor
        {
            get { return _thirdStopColor; }
            set
            {
                _thirdStopColor = value;
                OnPropertyChanged(nameof(ThirdStopColor));
            }
        }
        private Canvas _colorPickerCanvas;
        public int MessageCount { get { return _setting.MessageCount; } set { _setting.MessageCount = value; OnPropertyChanged(nameof(MessageCount)); } }
        public List<string> ArtMeshNames = new();
        public List<string> Tags = new();
        public List<string> SelectedArtMesh { get { return _setting.SelectedArtMesh; } }
        public List<string> SelectedTag { get { return _setting.SelectedArtMesh; } }
        public List<string> SelectedButFilteredName = new();
        public List<string> SelectedButFilteredTag = new();
        private Action<List<string>, List<string>, List<string>, List<string>> _loadModelCallback;
        private string _modelName = "載入中...";
        public string ModelName
        {
            get { return _modelName; }
            set
            {
                _modelName = value;
                OnPropertyChanged(nameof(ModelName));
            }
        }

        public ArtMeshTingtingViewModel(Canvas ColorPickerCanvas, Action<List<string>, List<string>, List<string>, List<string>> LoadModelCallback)
        {
            LoadModelAsync();
            _colorPickerCanvas = ColorPickerCanvas;
            _loadModelCallback = LoadModelCallback;
            PaintCanvas();
            UpdateColor();
        }
        private float _lastS = 0f;
        private float _lastH = 0f;

        private void SaveModelSetting(object sender, RoutedEventArgs e)
        {
            foreach (string name in SelectedButFilteredName)
            {
                SelectedArtMesh.Add(name);
            }
            foreach (string tag in SelectedButFilteredTag)
            {
                SelectedTag.Add(tag);
            }
            _resourceManager.SaveModelSetting(_setting);
            _featureManager.ReAssembleConfig();
            foreach (string name in SelectedButFilteredName)
            {
                SelectedArtMesh.Remove(name);
            }
            foreach (string tag in SelectedButFilteredTag)
            {
                SelectedTag.Remove(tag);
            }
        }
        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (string tag in e.AddedItems)
            {
                SelectedTag.Add(tag);
            }
            foreach (string tag in e.RemovedItems)
            {
                SelectedTag.Remove(tag);
            }
            if (IsTesting)
            {
                _featureManager.UpdateSelections(_setting);
            }
        }

        private void ArtMeshNameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (string artmesh in e.AddedItems)
            {
                SelectedArtMesh.Add(artmesh);
            }
            foreach (string artmesh in e.RemovedItems)
            {
                SelectedArtMesh.Remove(artmesh);
            }
            if (IsTesting)
            {
                _featureManager.UpdateSelections(_setting);
            }
        }

        private void Test(object sender, RoutedEventArgs e)
        {
            IsTesting = !IsTesting;
            if (IsTesting)
            {
                _featureManager.StartTesting(_setting);
            }
            else
            {
                _featureManager.StopTesting();
            }
        }

        private void Activate(object sender, RoutedEventArgs e)
        {
            Activated = !Activated;
            if (Activated)
            {
                SaveModelSetting(null, null);
                _featureManager.StartTask();
            }
        }

        private void LoadModel(object sender, RoutedEventArgs e)
        {
            LoadModelAsync();
        }

        public async void LoadModelAsync()
        {
            if (_vtsSocket.IsConnected)
            {
                Microsoft.UI.Dispatching.DispatcherQueue queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

                await Task.Run(() =>
                {
                    _vtsSocket.GetModelInformation();
                    if (_resourceManager.CurrentModelInformation.ArtMeshNames != null)
                    {
                        ArtMeshNames.Clear();
                        foreach (string artmesh in _resourceManager.CurrentModelInformation.ArtMeshNames)
                        {
                            ArtMeshNames.Add(artmesh);
                        }
                        Tags.Clear();
                        foreach (string tag in _resourceManager.CurrentModelInformation.ArtMeshTags)
                        {
                            Tags.Add(tag);
                        }
                        queue.TryEnqueue(() =>
                        {
                            ModelName = _resourceManager.CurrentModelInformation.ModelName;
                            _setting = _featureManager.GetSetting();
                            OnPropertyChanged((string)null);
                            List<string> tmpNames = _setting.SelectedArtMesh;
                            _setting.SelectedArtMesh = new();
                            List<string> tmpTags = _setting.SelectedTag;
                            _setting.SelectedTag = new();
                            _loadModelCallback(ArtMeshNames, Tags, tmpNames, tmpTags);
                        });
                    }
                });
            }
            else
            {
                _setting = new ArtmeshColoringSetting();
            }
        }

        private void PaintCanvas()
        {
            float _circlePercent = 360.0F / 100F;
            for (int i = 0; i < 100; i++)
            {
                LinearGradientBrush lgb = new();
                lgb.GradientStops.Add(new GradientStop() { Color = ColorHelper.ConvertHSV2RGBColor(_circlePercent * i, MinimumS / 100f, 1) });
                lgb.GradientStops.Add(new GradientStop() { Color = ColorHelper.ConvertHSV2RGBColor(_circlePercent * i, MaximumS / 100f, 1), Offset = 1 });
                Line line = new()
                {
                    X1 = _colorPickerCanvas.ActualWidth / 100 * i,
                    Y1 = 0,
                    X2 = _colorPickerCanvas.ActualWidth / 100 * i,
                    Y2 = _colorPickerCanvas.ActualHeight,
                    StrokeThickness = (_colorPickerCanvas.ActualWidth - 100) / 100 * 2.5,
                    Stroke = lgb
                };
                line.Tag = i;
                _colorPickerCanvas.Children.Add(line);
            }
        }

        private void UpdateCanvas()
        {
            float _circlePercent = 360.0F / 100F;
            Line tmpLine;
            if (_colorPickerCanvas != null)
            {
                foreach (UIElement line in _colorPickerCanvas.Children)
                {
                    if (line.GetType() == typeof(Line))
                    {
                        tmpLine = (Line)line;
                        LinearGradientBrush lgb = new();
                        lgb.GradientStops.Add(new GradientStop()
                        {
                            Color = ColorHelper.ConvertHSV2RGBColor(_circlePercent * (int)tmpLine.Tag, MinimumS / 100f, 1)
                        });
                        lgb.GradientStops.Add(new GradientStop()
                        {
                            Color = ColorHelper.ConvertHSV2RGBColor(_circlePercent * (int)tmpLine.Tag, MaximumS / 100f, 1),
                            Offset = 1
                        });
                        tmpLine.Stroke = lgb;
                    }
                }
                UpdateColor();
            }
        }

        private bool IsInt(object value)
        {
            return value.GetType() == typeof(int) || Math.Floor((double)value) == Math.Ceiling((double)value);
        }

        public void UpdateColor()
        {
            UpdateColor(_lastH, _lastS);
        }

        public void UpdateColor(float h, float s)
        {
            float adjustedS = (MinimumS + (MaximumS - MinimumS) * s) / 100f;
            FirstStopColor = ColorHelper.ConvertHSV2RGBColor(h, adjustedS, MinimumV / 100f);
            SecondStopColor = ColorHelper.ConvertHSV2RGBColor(h, adjustedS, (MinimumV + MaximumV) / 200f);
            ThirdStopColor = ColorHelper.ConvertHSV2RGBColor(h, adjustedS, MaximumV / 100f);
            _lastS = s;
            _lastH = h;
        }
    }
}
