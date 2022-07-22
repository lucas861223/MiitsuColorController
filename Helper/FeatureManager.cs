using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MiitsuColorController.Models;
using System.Text.Json.Serialization;
using System.Text;
using System;

namespace MiitsuColorController.Helper
{
    public class FeatureManager
    {
        private static FeatureManager _instance = null;
        public static FeatureManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FeatureManager();
                }
                return _instance;
            }
        }
        private ArtMeshColorTint _colortintHolder = new();
        private bool _artmeshColoringFeature = false;
        private bool _hasQueue = false;
        private float _sRatio;
        private float _vRatio;
        private int[] _artmeshCurrentColor = { 0, 0, 0 };
        private JsonSerializerOptions _jsonSerializerOptions = new();
        private Queue<int[]> _artmeshEmoteHistory = new();
        private ResourceManager _resourceManager = ResourceManager.Instance;
        private TwitchSocket _twitchSocket = TwitchSocket.Instance;
        private VTSSocket _vtsSocket = VTSSocket.Instance;
        public bool ArtMeshTintingActivated = false;
        public bool Suspended = false;


        private ArtmeshColoringSetting _setting;

        public bool ArtmeshColoringFeature { get { return _artmeshColoringFeature; } }

        private FeatureManager()
        {
            _jsonSerializerOptions.IncludeFields = true;
            //_jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        }

        public void ReAssembleConfig()
        {
            Suspended = true;
            _setting = _resourceManager.LoadModelSetting();
            VTSColorTintData request = new VTSColorTintData();
            request.data.artMeshMatcher = new ArtMeshMatcher();
            if (_setting.SelectedArtMesh.Count > 0)
            {
                request.data.artMeshMatcher.nameExact = _setting.SelectedArtMesh.ToArray();
            }
            if (_setting.SelectedTag.Count > 0)
            {
                request.data.artMeshMatcher.tagExact = _setting.SelectedTag.ToArray();
            }
            request.data.colorTint = new ArtMeshColorTint();
            request.data.colorTint.colorB = 252;
            request.data.colorTint.colorR = 253;
            request.data.colorTint.colorG = 254;
            request.data.colorTint.colorA = 255;
            string json = JsonSerializer.Serialize(request, typeof(VTSColorTintData), _jsonSerializerOptions);
            //escape brackets
            json = json.Replace("{", "{{").Replace("}", "}}");
            //change to use {0}, {1} for formatting purpose
            json = json.Replace(":253", ":{0}").Replace(":254", ":{1}").Replace(":252", ":{2}");
            _sRatio = (_setting.MaximumS - _setting.MinimumS) / 100f;
            _vRatio = (_setting.MaximumV - _setting.MinimumV) / 100f;
            _vtsSocket.SetArtmeshColoringParameters(_colortintHolder, _setting.Interpolation, _setting.Duration, json);
            Suspended = false;
        }

        public void SuspendFeatures()
        {
            Suspended = true;
        }

        public void ResumeFeatures()
        {
            Suspended = false;
            _twitchSocket.ReceiveQueue.Clear();
        }

        public async void StartTask()
        {
            await Task.Run(() =>
            {
                System.Collections.Concurrent.ConcurrentQueue<string> queue = _twitchSocket.ReceiveQueue;
                queue.Clear();
                ColorTint colorTintTmp = new ColorTint() { colorA = 0 };
                float[] rgb = { 0f, 0f, 0f };
                int rgbSum = 0;
                int startIndex = ("PRIVMSG #" + _resourceManager.StringResourceDictionary[ResourceKey.TwitchUserName] + " :").Length;
                int[] temp;
                string message;
                string[] tokens;
                ReAssembleConfig();
                while (_setting.Activated)
                {
                    if (queue.TryDequeue(out message))
                    {
                        tokens = message.Substring(startIndex).Trim('\r', '\n').Split(" ");
                        if (_setting.Activated)
                        {
                            int[] emotes = { 0, 0, 0 };
                            foreach (string token in tokens)
                            {
                                if (string.CompareOrdinal(token, _setting.RedEmote) == 0)
                                {
                                    emotes[0] += 1;
                                }
                                if (string.CompareOrdinal(token, _setting.GreenEmote) == 0)
                                {
                                    emotes[1] += 1;
                                }
                                if (string.CompareOrdinal(token, _setting.BlueEmote) == 0)
                                {
                                    emotes[2] += 1;
                                }
                            }
                            if (emotes[0] + emotes[1] + emotes[2] == 0)
                            {
                                continue;
                            }
                            _artmeshEmoteHistory.Enqueue(emotes);
                            if (_artmeshEmoteHistory.Count > _setting.MessageCount)
                            {
                                temp = _artmeshEmoteHistory.Dequeue();
                                _artmeshCurrentColor[0] -= temp[0];
                                _artmeshCurrentColor[1] -= temp[1];
                                _artmeshCurrentColor[2] -= temp[2];
                            }
                            _artmeshCurrentColor[0] += emotes[0];
                            _artmeshCurrentColor[1] += emotes[1];
                            _artmeshCurrentColor[2] += emotes[2];
                            rgbSum = _artmeshCurrentColor[0] + _artmeshCurrentColor[1] + _artmeshCurrentColor[2];
                            rgb[0] = (255f * (_artmeshCurrentColor[0] / rgbSum));
                            rgb[1] = (255f * (_artmeshCurrentColor[1] / rgbSum));
                            rgb[2] = (255f * (_artmeshCurrentColor[2] / rgbSum));
                            ColorHelper.RBGToAdjustedColorTint(rgb, _sRatio, _setting.MinimumS, _vRatio, _setting.MinimumV, ref _colortintHolder);
                            //change to use strinigbuilder to format
                            _vtsSocket.UpdateTargetColor();
                        }
                    }
                    else
                    {
                        Task.Delay(100).Wait();
                    }
                    while (Suspended)
                    {
                        Task.Delay(100).Wait();
                    }
                }
            });
        }



        public void ActivateArtmeshColoring()
        {
            if (!ArtMeshTintingActivated)
            {
                ArtMeshTintingActivated = true;
                ReAssembleConfig();
            }
        }

        public void DeactivateArtmeshColoring()
        {

        }
    }
}
