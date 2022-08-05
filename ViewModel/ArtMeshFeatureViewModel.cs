using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MiitsuColorController.Helper;
using System;
using System.Collections.Generic;
using Windows.UI;
using MiitsuColorController.Models;

namespace MiitsuColorController.ViewModel
{
    public class ArtMeshFeatureViewModel : ArtMeshWidgetViewModel
    {
        public override int Interpolation
        {
            get { return _setting.Interpolation; }
            set
            {
                _setting.Interpolation = value;
                OnPropertyChanged(nameof(Interpolation));
                if (IsAutoTesting || IsClickTesting)
                {
                    _featureManager.UpdateTestingParameters(_setting);
                }
            }
        }

        public override int Duration
        {
            get { return _setting.Duration; }
            set
            {
                _setting.Duration = value;
                OnPropertyChanged(nameof(Duration));
                if (IsAutoTesting || IsClickTesting)
                {
                    _featureManager.UpdateTestingParameters(_setting);
                }
            }
        }


        private bool _isAutoTesting = false;
        public bool IsAutoTesting
        {
            get { return _isAutoTesting; }
            set { _isAutoTesting = value; OnPropertyChanged(nameof(IsAutoTesting)); OnPropertyChanged(nameof(IsTesting)); }
        }
        public bool IsClickTesting
        {
            get { return _isClickTesting; }
            set { _isClickTesting = value; OnPropertyChanged(nameof(IsClickTesting)); OnPropertyChanged(nameof(IsTesting)); }
        }
        public bool IsTesting { get { return IsClickTesting || IsAutoTesting; } }
        public RoutedEventHandler SaveCommand { get { return SaveModelSetting; } }
        public RoutedEventHandler TestCommand { get { return Test; } }
        public PointerEventHandler PointerEnteredCommand { get { return PointerEntered; } }
        public PointerEventHandler PointerLeftCommand { get { return PointerLeft; } }
        public SelectionChangedEventHandler NameSelectionCommand { get { return ArtMeshNameListView_SelectionChanged; } }
        public SelectionChangedEventHandler TagSelectionCommand { get { return TagListView_SelectionChanged; } }
        public string Description { get; set; }

        public override int MinimumS
        {
            get { return _setting.MinimumS; }
            set
            {
                if (IsInt(value) && (int)value <= MaximumS)
                {
                    _setting.MinimumS = value;
                    OnPropertyChanged(nameof(S));
                    UpdateCanvas();
                    if (IsAutoTesting || IsClickTesting)
                    {
                        _featureManager.UpdateTestingParameters(_setting);
                    }
                }
                OnPropertyChanged(nameof(MinimumS));
            }
        }

        public override int MaximumS
        {
            get { return _setting.MaximumS; }
            set
            {
                if (IsInt(value) && (int)value >= MinimumS)
                {
                    _setting.MaximumS = value;
                    OnPropertyChanged(nameof(S));
                    UpdateCanvas();
                    if (IsAutoTesting || IsClickTesting)
                    {
                        _featureManager.UpdateTestingParameters(_setting);
                    }
                }
                OnPropertyChanged(nameof(MaximumS));
            }
        }

        public override int MinimumV
        {
            get { return _setting.MinimumV; }
            set
            {
                if (IsInt(value) && (int)value <= MaximumV)
                {
                    _setting.MinimumV = value;
                    OnPropertyChanged(nameof(V));
                    UpdateColor();
                    if (IsAutoTesting || IsClickTesting)
                    {
                        _featureManager.UpdateTestingParameters(_setting);
                    }
                }
                OnPropertyChanged(nameof(MinimumV));
            }
        }

        public override int MaximumV
        {
            get { return _setting.MaximumV; }
            set
            {
                if (IsInt(value) && (int)value >= MinimumV)
                {
                    _setting.MaximumV = value;
                    OnPropertyChanged(nameof(V));
                    UpdateColor();
                    if (IsAutoTesting || IsClickTesting)
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
        public List<string> ArtMeshNames = new();
        public List<string> Tags = new();
        public List<string> SelectedArtMesh { get { return _setting.SelectedArtMesh; } }
        public List<string> SelectedTag { get { return _setting.SelectedTag; } }
        public List<string> SelectedButFilteredName = new();
        public List<string> SelectedButFilteredTag = new();
        private Action<List<string>, List<string>, List<string>, List<string>> _loadModelCallback;
        private int _h = 0;
        private float _s = 0;
        private float _v = 0.5f;
        public override RoutedEventHandler ActivateCommand { get { return Activate; } }
        public int H { get { return _h; } set { _h = value; OnPropertyChanged(nameof(H)); } }
        public int S { get { return (int)Math.Round(_s * (MaximumS - MinimumS) + MinimumS); } }
        public int V { get { return (int)Math.Round(_v * (MaximumV - MinimumV) + MinimumV); } }


        public ArtMeshFeatureViewModel(Action<List<string>, List<string>, List<string>, List<string>> LoadModelCallback)
        {
            _uiThread = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            //VTSSocket.Instance.ConnectionEstablishedEvent += new Action(LoadModelAsync);
            _featureManager.RegisterNewSettingEvent(new Action(NewModelEventHandler));
            _loadModelCallback = LoadModelCallback;
            Description = _resourceLoader.GetString(StringEnum.DefaultDescription);
            OnPropertyChanged(nameof(Description));
        }

        private float _lastS = 0f;
        private float _lastH = 0f;
        private bool _isClickTesting;

        public void StartLoadingModel(Canvas ColorPickerCanvas)
        {
            LoadModel();
            _colorPickerCanvas = ColorPickerCanvas;
            PaintCanvas();
            UpdateColor();
        }

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
        protected override void Activate(object sender, RoutedEventArgs e)
        {
            Activated = !Activated;
            if (Activated)
            {
                SaveModelSetting(null, null);
                _featureManager.StartTask();
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
            if (IsAutoTesting || IsClickTesting)
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
            if (IsAutoTesting || IsClickTesting)
            {
                _featureManager.UpdateSelections(_setting);
            }
        }

        public void Test(object sender, RoutedEventArgs e)
        {
            IsAutoTesting = !IsAutoTesting;
            if (IsAutoTesting)
            {
                if (IsClickTesting)
                {
                    Clicktest();
                }
                _featureManager.StartAutoTesting(_setting);
            }
            else
            {
                _featureManager.StopTesting();
            }
        }

        public void Clicktest()
        {
            IsClickTesting = !IsClickTesting;
            if (IsClickTesting)
            {
                if (IsAutoTesting)
                {
                    Test(null, null);
                }
                _featureManager.StartClickTesting(_setting);
            }
            else
            {
                _featureManager.StopClickTesting();
            }
        }


        protected override void NewModelEventHandler()
        {
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
                _uiThread.TryEnqueue(() =>
                {
                    _setting = _featureManager.GetSetting();
                    ModelName = _resourceManager.CurrentModelInformation.ModelName;
                    ModelID = _resourceManager.CurrentModelInformation.ID;
                    OnPropertyChanged((string)null);
                    List<string> tmpNames = _setting.SelectedArtMesh;
                    _setting.SelectedArtMesh = new();
                    List<string> tmpTags = _setting.SelectedTag;
                    _setting.SelectedTag = new();
                    _loadModelCallback?.Invoke(ArtMeshNames, Tags, tmpNames, tmpTags);
                });
            }
        }


        private void PaintCanvas()
        {
            float _circlePercent = 360.0F / 100F;
            double thickness = _colorPickerCanvas.ActualWidth / 200d;
            for (int i = 0; i < 100; i++)
            {
                LinearGradientBrush lgb = new();
                lgb.GradientStops.Add(new GradientStop() { Color = ColorHelper.ConvertHSV2RGBColor(_circlePercent * i, MinimumS / 100f, 1) });
                lgb.GradientStops.Add(new GradientStop() { Color = ColorHelper.ConvertHSV2RGBColor(_circlePercent * i, MaximumS / 100f, 1), Offset = 1 });
                Line line = new()
                {
                    X1 = thickness * (i * 2 + 1),
                    Y1 = 0,
                    X2 = thickness * (i * 2 + 1),
                    Y2 = _colorPickerCanvas.ActualHeight,
                    StrokeThickness = thickness * 2.5,
                    Stroke = lgb,
                    Tag = i
                };
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

        public void UpdateHS(double width, float height)
        {
            H = (int)Math.Round(width);
            _s = height;
            OnPropertyChanged(nameof(S));
            if (_isClickTesting)
            {
                _featureManager.AddClickTestQueue(H, S / 100f, V / 100f);
            }
        }

        public void UpdateV(float height)
        {
            _v = height;
            OnPropertyChanged(nameof(V));
            if (_isClickTesting)
            {
                _featureManager.AddClickTestQueue(H, S / 100f, V / 100f);
            }
        }

        public void SetDescription(bool IsEntering, string Tag)
        {
            if (IsEntering)
            {
                switch (Tag)
                {
                    case "ActivateButton":
                        Description = _resourceLoader.GetString(StringEnum.ActivateButtonDescription);
                        break;
                    case "ArtMeshNameListView":
                        Description = _resourceLoader.GetString(StringEnum.ArtmeshNameListviewDescription);
                        break;
                    case "SaveButton":
                        Description = _resourceLoader.GetString(StringEnum.SaveButtonDescription);
                        break;
                    case "RefreshButton":
                        Description = _resourceLoader.GetString(StringEnum.RefreshButtonDescription);
                        break;
                    case "AutoTestButton":
                        Description = _resourceLoader.GetString(StringEnum.AutoTestButtonDescription);
                        break;
                    case "ClickTestButton":
                        Description = _resourceLoader.GetString(StringEnum.ManualTestButtonDescription);
                        break;
                    case "Filter":
                        Description = _resourceLoader.GetString(StringEnum.FilterDescription); ;
                        break;
                    case "TagListView":
                        Description = _resourceLoader.GetString(StringEnum.TagListViewDescription);
                        break;
                    case "MessageHandling":
                        Description = _resourceLoader.GetString(StringEnum.MessageHandlingMethodDescription);
                        break;
                    case "MessageCount":
                        Description = _resourceLoader.GetString(StringEnum.MessageCountDescription);
                        break;
                    case "GreenEmote":
                        Description = _resourceLoader.GetString(StringEnum.GreenDescription);
                        break;
                    case "RedEmote":
                        Description = _resourceLoader.GetString(StringEnum.RedDescription);
                        break;
                    case "BlueEmote":
                        Description = _resourceLoader.GetString(StringEnum.BlueDescription);
                        break;
                    case "Interpolation":
                        Description = _resourceLoader.GetString(StringEnum.InterpolationDescription);
                        break;
                    case "Duration":
                        Description = _resourceLoader.GetString(StringEnum.DurationDescription);
                        break;
                    case "Ranges":
                        Description = _resourceLoader.GetString(StringEnum.RangesDescription);
                        break;
                    case "ColorPickerCanvas":
                        Description = _resourceLoader.GetString(StringEnum.ColorPickerCanvasDescription);
                        break;
                    case "RectColor":
                        Description = _resourceLoader.GetString(StringEnum.RectColorDescription);
                        break;
                }
            }
            else
            {
                Description = _resourceLoader.GetString(StringEnum.DefaultDescription);
            }
            OnPropertyChanged(nameof(Description));
        }

        private void PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            SetDescription(true, (string)((FrameworkElement)sender).Tag);
        }

        private void PointerLeft(object sender, PointerRoutedEventArgs e)
        {
            SetDescription(false, null);
        }
    }
}