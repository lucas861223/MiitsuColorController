using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MiitsuColorController.Helper;
using System;
using System.Collections.Generic;
using Windows.UI;

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
        //change to use resource sheet
        private string _defaultDescription = "設定結束後存檔，即可在下一次開啟時自動讀取\n更換模組後需要先刷新才會讀取新模組信息\n啟用聊天室控制後更改設定需要存檔才會套用";

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
            Description = _defaultDescription;
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
                        Description = "啟用Twitch聊天室控制，需先關閉運行中的測試";
                        break;
                    case "ArtMeshNameListView":
                        Description = "勾選欲改變顏色的部件(artmesh)\n測試時更改設定可馬上套用，不需要另外儲存\n因Winui 3問題(issue #7230)，滾動列表時會造成程式閃退，請善用搜尋功能";
                        break;
                    case "SaveButton":
                        Description = "儲存現在的設定，並套用至正在運行的功能上\n";
                        break;
                    case "RefreshButton":
                        Description = "讀取新的模型，並讀取相應設定\n或是捨棄修改，恢復儲存的設定";
                        break;
                    case "AutoTestButton":
                        Description = "自動變色測試\n需先停用聊天室控制\n程式將把選取的部件以範圍內的顏色染色\n啟用時更改設定可馬上套用，不需要另外儲存\n可以藉此機會看設定的速度、漸變、顏色範圍等是否滿意";
                        break;
                    case "ClickTestButton":
                        Description = "手動選色測試\n需先停用聊天室控制\n程式將把選取的部件染上右下角色表選擇的顏色\n啟用時更改設定可馬上套用，不需要另外儲存\n使用者可以點擊右下角的色表來指定要變成的顏色\n可以藉此機會看設定的速度、漸變等是否滿意，以及測試理想的顏色範圍";
                        break;
                    case "Filter":
                        Description = "篩選部件和標籤";
                        break;
                    case "TagListView":
                        Description = "勾選欲改變顏色的標籤(Tag)\n測試時更改設定可馬上套用，不需要另外儲存\n因Winui 3問題(issue #7230)，滾動列表時會造成程式閃退，請善用搜尋功能";
                        break;
                    case "MessageHandling":
                        Description = "變色方針\n設定此功能在當前顏色還未完全變化時該如何處理新的變色指令\n累積變色- 中斷當前指令，並將目標替換成新顏色\n排程變色- 將當前目標變色完成，再處理下一個顏色";
                        break;
                    case "MessageCount":
                        Description = "參考訊息數量\n要參考多少訊息的顏色比例來決定目標顏色";
                        break;
                    case "GreenEmote":
                        Description = "增加綠色元素的聊天室關鍵詞";
                        break;
                    case "RedEmote":
                        Description = "增加紅色元素的聊天室關鍵詞";
                        break;
                    case "BlueEmote":
                        Description = "增加藍色元素的聊天室關鍵詞";
                        break;
                    case "Interpolation":
                        Description = "漸變數量\n由顏色A變成顏色B時中間過渡顏色的數量\n數量越高，顏色變化越順，對程式和Vtube Studio的負擔越重\n例:由黑色(0,0,0)變成紅色(255,0,0)時，中間應有多少如(25,0,0)、(50,0,0)等過渡色\n建議善用測試功能來測試可以承受的數量";
                        break;
                    case "Duration":
                        Description = "時長\n由顏色A變成顏色B時應花費的時間\n數字越高，變色越慢，對程式和vtube studio的負擔越輕\n建議善用測試功能來測試可以承受的數量";
                        break;
                    case "Ranges":
                        Description = "可接受的顏色範圍\n上方色表將會反應範圍更變";
                        break;
                    case "ColorPickerCanvas":
                        Description = "H-S色表\nx軸為色調(hue)，從左至右由0到360，y軸為飽和度(saturation)，從上至下由0到100\n手動選色測試時可以在此選擇顏色";
                        break;
                    case "RectColor":
                        Description = "V色表\ny軸為明亮度(value)，從上至下由100到0\n點擊右方色表即可挑選色調和飽和度\n手動選色測試時可以在此選擇顏色";
                        break;
                }
            }
            else
            {
                Description = _defaultDescription;
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