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
using System.Windows.Input;
using MiitsuColorController.Models;

namespace MiitsuColorController.ViewModel
{
    public class ArtMeshTingtingViewModel : ObservableObject
    {
        private VTSSocket _vtsSocket = VTSSocket.Instance;
        private int _messageHandlingMethod = 0;
        public int MessageHandlingMethod
        {
            get { return _messageHandlingMethod; }
            set { _messageHandlingMethod = value; OnPropertyChanged("MessageHandlingMethod"); }
        }
        private ArtmeshColoringSetting _setting;
        public int Interpolation { get { return _setting.Interpolation; } set { _setting.Interpolation = value; OnPropertyChanged("Interpolation"); } }
        public int Duration { get { return _setting.Duration; } set { _setting.Duration = value; OnPropertyChanged("Duration"); } }
        public string GreenEmote { get { return _setting.GreenEmote; } set { _setting.GreenEmote = value; OnPropertyChanged("GreenEmote"); } }
        public string RedEmote { get { return _setting.RedEmote; } set { _setting.RedEmote = value; OnPropertyChanged("RedEmote"); } }
        public string BlueEmote { get { return _setting.BlueEmote; } set { _setting.BlueEmote = value; OnPropertyChanged("BlueEmote"); } }
        public bool Activated { get { return _setting.Activated; } set { _setting.Activated = value; OnPropertyChanged("Activated"); } }
        public RoutedEventHandler RefreshCommand { get { return LoadModel; } }
        public RoutedEventHandler SaveCommand { get { return SaveModelSetting; } }
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
                }
                OnPropertyChanged("MinimumS");
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
                }
                OnPropertyChanged("MaximumS");
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
                }
                OnPropertyChanged("MinimumV");
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
                }
                OnPropertyChanged("MaximumV");
            }
        }
        private Color _firstStopColor;
        public Color FirstStopColor
        {
            get { return _firstStopColor; }
            set
            {
                _firstStopColor = value;
                OnPropertyChanged("FirstStopColor");
            }
        }
        private Color _secondStopColor;
        public Color SecondStopColor
        {
            get { return _secondStopColor; }
            set
            {
                _secondStopColor = value;
                OnPropertyChanged("SecondStopColor");
            }
        }
        private Color _thirdStopColor;
        public Color ThirdStopColor
        {
            get { return _thirdStopColor; }
            set
            {
                _thirdStopColor = value;
                OnPropertyChanged("ThirdStopColor");
            }
        }
        private Canvas _colorPickerCanvas;
        public int MessageCount { get { return _setting.MessageCount; } set { _setting.MessageCount = value; OnPropertyChanged("MessageCount"); } }
        public List<string> ArtMeshNames = new();
        public List<string> Tags = new();
        public List<string> SelectedArtMesh;
        public List<string> SelectedTag;
        private Action<List<string>, List<string>, List<string>, List<string>> _loadModelCallback;
        private string _modelName = "載入中...";
        public string ModelName
        {
            get { return _modelName; }
            set
            {
                _modelName = value;
                OnPropertyChanged("ModelName");
            }
        }
        public ArtMeshTingtingViewModel(Canvas ColorPickerCanvas, Action<List<string>, List<string>, List<string>, List<string>> LoadModelCallback)
        {
            LoadModel();
            SelectedArtMesh = _setting.SelectedArtMesh;
            SelectedTag = _setting.SelectedTag;
            _colorPickerCanvas = ColorPickerCanvas;
            _loadModelCallback = LoadModelCallback;
            PaintCanvas();
            UpdateColor();
        }
        private float _lastS = 0f;
        private float _lastH = 0f;

        private void SaveModelSetting(object sender, RoutedEventArgs e)
        {
            _resourceManager.SaveModelSetting(_setting);
            _featureManager.ReAssembleConfig();
        }

        private void LoadModel(object sender, RoutedEventArgs e)
        {
            LoadModel();
        }
        public async void LoadModel()
        {
            if (_vtsSocket.IsConnected)
            {
                Microsoft.UI.Dispatching.DispatcherQueue queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

                await Task.Run(() =>
                {
                    _vtsSocket.GetModelInformation();
                    if (_resourceManager.CurrentModelInformation.ArtMeshNames != null)
                    {
                        foreach (string artmesh in _resourceManager.CurrentModelInformation.ArtMeshNames)
                        {
                            ArtMeshNames.Add(artmesh);
                        }
                        foreach (string tag in _resourceManager.CurrentModelInformation.ArtMeshTags)
                        {
                            Tags.Add(tag);
                        }
                        queue.TryEnqueue(() =>
                        {
                            ModelName = _resourceManager.CurrentModelInformation.ModelName;
                            OnPropertyChanged("ModelName");
                            _setting = _resourceManager.LoadModelSetting();
                            List<string> tmpNames = SelectedArtMesh;
                            List<string> tmpTags = SelectedTag;
                            SelectedTag = new();
                            SelectedArtMesh = new();
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
                lgb.GradientStops.Add(new GradientStop() { Color = ColorHelper.ConvertHSV2RGB(_circlePercent * i, MinimumS / 100f, 1) });
                lgb.GradientStops.Add(new GradientStop() { Color = ColorHelper.ConvertHSV2RGB(_circlePercent * i, MaximumS / 100f, 1), Offset = 1 });
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
                            Color = ColorHelper.ConvertHSV2RGB(_circlePercent * (int)tmpLine.Tag,
                                                               MinimumS / 100f, 1)
                        });
                        lgb.GradientStops.Add(new GradientStop()
                        {
                            Color = ColorHelper.ConvertHSV2RGB(_circlePercent * (int)tmpLine.Tag,
                                                               MaximumS / 100f, 1),
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
            FirstStopColor = ColorHelper.ConvertHSV2RGB(h, adjustedS, MinimumV / 100f);
            SecondStopColor = ColorHelper.ConvertHSV2RGB(h, adjustedS, (MinimumV + MaximumV) / 200f);
            ThirdStopColor = ColorHelper.ConvertHSV2RGB(h, adjustedS, MaximumV / 100f);
            _lastS = s;
            _lastH = h;
        }
    }
}
