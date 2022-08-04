using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using MiitsuColorController.Models;
using MiitsuColorController.Helper;
using Microsoft.UI.Xaml;

namespace MiitsuColorController.ViewModel
{
    public class ArtMeshWidgetViewModel : ObservableObject
    {
        protected VTSSocket _vtsSocket = VTSSocket.Instance;
        protected ArtmeshColoringSetting _setting = new();
        public int MessageHandlingMethod
        {
            get { return _setting.MessageHandlingMethod; }
            set { _setting.MessageHandlingMethod = value; OnPropertyChanged(nameof(MessageHandlingMethod)); }
        }
        public virtual int Interpolation
        {
            get { return _setting.Interpolation; }
            set
            {
                _setting.Interpolation = value;
                OnPropertyChanged(nameof(Interpolation));
            }
        }

        public virtual int Duration
        {
            get { return _setting.Duration; }
            set
            {
                _setting.Duration = value;
                OnPropertyChanged(nameof(Duration));
            }
        }

        public string GreenEmote { get { return _setting.GreenEmote; } set { _setting.GreenEmote = value; OnPropertyChanged(nameof(GreenEmote)); } }
        public string RedEmote { get { return _setting.RedEmote; } set { _setting.RedEmote = value; OnPropertyChanged(nameof(RedEmote)); } }
        public string BlueEmote { get { return _setting.BlueEmote; } set { _setting.BlueEmote = value; OnPropertyChanged(nameof(BlueEmote)); } }
        public bool Activated { get { return _setting.Activated; } set { _setting.Activated = value; OnPropertyChanged(nameof(Activated)); } }
        public RoutedEventHandler RefreshCommand { get { return LoadModel; } }
        public virtual RoutedEventHandler ActivateCommand { get { return Activate; } }

        protected ResourceManager _resourceManager = ResourceManager.Instance;
        protected FeatureManager _featureManager = FeatureManager.Instance;
        protected Microsoft.UI.Dispatching.DispatcherQueue _uiThread;

        public virtual int MinimumS
        {
            get { return _setting.MinimumS; }
            set
            {
                _setting.MinimumS = value;
                OnPropertyChanged(nameof(MinimumS));
            }
        }

        public virtual int MaximumS
        {
            get { return _setting.MaximumS; }
            set
            {
                _setting.MaximumS = value;
                OnPropertyChanged(nameof(MaximumS));
            }
        }

        public virtual int MinimumV
        {
            get { return _setting.MinimumV; }
            set
            {
                _setting.MinimumV = value;
                OnPropertyChanged(nameof(MinimumV));
            }
        }

        public virtual int MaximumV
        {
            get { return _setting.MaximumV; }
            set
            {
                _setting.MaximumV = value;
                OnPropertyChanged(nameof(MaximumV));
            }
        }
        public int MessageCount { get { return _setting.MessageCount; } set { _setting.MessageCount = value; OnPropertyChanged(nameof(MessageCount)); } }
        protected string _modelName = "載入中...";

        public string ModelName
        {
            get { return _modelName; }
            set
            {
                _modelName = value;
                OnPropertyChanged(nameof(ModelName));
            }
        }

        private string _modelID = "載入中...";

        public string ModelID
        {
            get { return _modelID; }
            set
            {
                _modelID = value;
                OnPropertyChanged(nameof(ModelID));
            }
        }

        public ArtMeshWidgetViewModel()
        {
            _uiThread = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _featureManager.RegisterNewSettingEvent(new Action(NewModelEventHandler));
            NewModelEventHandler();
            LoadModel();
        }

        protected virtual void NewModelEventHandler()
        {
            _uiThread.TryEnqueue(() =>
            {
                _setting = _featureManager.GetSetting();
                ModelName = _resourceManager.CurrentModelInformation.ModelName;
                ModelID = _resourceManager.CurrentModelInformation.ID;
            });
        }

        private void LoadModel(object sender, RoutedEventArgs e)
        {
            LoadModel();
        }

        public void LoadModel()
        {
            if (_vtsSocket.IsConnected)
            {
                _vtsSocket.GetModelInformation();
            }
            else
            {
                _setting = new ArtmeshColoringSetting();
            }
        }
        protected virtual void Activate(object sender, RoutedEventArgs e)
        {
            Activated = !Activated;
            if (Activated)
            {
                _featureManager.StartTask();
            }
        }
    }
}
