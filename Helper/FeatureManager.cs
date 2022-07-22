using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MiitsuColorController.Models;

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
        private float _sRatio;
        private float _vRatio;
        private int[] _artmeshCurrentColor = { 0, 0, 0 };
        private JsonSerializerOptions _jsonSerializerOptions = new();
        private ConcurrentQueue<int[]> _artmeshEmoteHistory = new();
        private ResourceManager _resourceManager = ResourceManager.Instance;
        private TwitchSocket _twitchSocket = TwitchSocket.Instance;
        private VTSSocket _vtsSocket = VTSSocket.Instance;
        public bool ArtMeshTintingActivated = false;
        public bool Suspended = false;
        private string _formatString;
        private int _taskDelay;

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
            _formatString = JsonSerializer.Serialize(request, typeof(VTSColorTintData), _jsonSerializerOptions);
            //escape brackets
            _formatString = _formatString.Replace("{", "{{").Replace("}", "}}");
            _formatString = _formatString.Replace(":253", ":{0}").Replace(":254", ":{1}").Replace(":252", ":{2}");
            _sRatio = (_setting.MaximumS - _setting.MinimumS) / 100f;
            _vRatio = (_setting.MaximumV - _setting.MinimumV) / 100f;
            _taskDelay = _setting.Duration / (_setting.Interpolation + 1);
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
                ConcurrentQueue<string> queue = _twitchSocket.ReceiveQueue;
                queue.Clear();
                ColorTint colorTintTmp = new ColorTint() { colorA = 0 };
                int startIndex = ("PRIVMSG #" + _resourceManager.StringResourceDictionary[ResourceKey.TwitchUserName] + " :").Length;
                string message;
                string[] tokens;
                float[] rgb = { 0f, 0f, 0f };
                int[] temp;
                int rgbSum;
                float[] difference = { 0, 0, 0 };
                float[] currentAdjustedRGB = { 0, 0, 0 };
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
                            _artmeshCurrentColor[0] += emotes[0];
                            _artmeshCurrentColor[1] += emotes[1];
                            _artmeshCurrentColor[2] += emotes[2];
                            if (_artmeshEmoteHistory.Count > _setting.MessageCount)
                            {
                                _artmeshEmoteHistory.TryDequeue(out temp);
                                _artmeshCurrentColor[0] -= temp[0];
                                _artmeshCurrentColor[1] -= temp[1];
                                _artmeshCurrentColor[2] -= temp[2];
                            }
                            rgbSum = _artmeshCurrentColor[0] + _artmeshCurrentColor[1] + _artmeshCurrentColor[2];
                            rgb[0] = (255f * (_artmeshCurrentColor[0] / rgbSum));
                            rgb[1] = (255f * (_artmeshCurrentColor[1] / rgbSum));
                            rgb[2] = (255f * (_artmeshCurrentColor[2] / rgbSum));
                            ColorHelper.RBGToAdjustedColorTint(rgb, _sRatio, _setting.MinimumS, _vRatio, _setting.MinimumV, ref _colortintHolder);
                            if (_setting.MessageHandlingMethod == 0)
                            {
                                int leftStep = _vtsSocket.TaskQueue.Count;
                                currentAdjustedRGB[0] -= difference[0] * (_setting.Interpolation + 1 - leftStep);
                                currentAdjustedRGB[1] -= difference[1] * (_setting.Interpolation + 1 - leftStep);
                                currentAdjustedRGB[2] -= difference[2] * (_setting.Interpolation + 1 - leftStep);
                                _vtsSocket.TaskQueue.Clear();

                            }
                            difference[0] = _colortintHolder.colorR - currentAdjustedRGB[0] / (_setting.Interpolation + 1);
                            difference[1] = _colortintHolder.colorG - currentAdjustedRGB[1] / (_setting.Interpolation + 1);
                            difference[2] = _colortintHolder.colorB - currentAdjustedRGB[2] / (_setting.Interpolation + 1);
                            for (int i = 0; i <= _setting.Interpolation; i++)
                            {
                                currentAdjustedRGB[0] += difference[0];
                                currentAdjustedRGB[1] += difference[1];
                                currentAdjustedRGB[2] += difference[2];
                                _vtsSocket.TaskQueue.Enqueue(new Tuple<string, int>(String.Format(_formatString, Math.Round(currentAdjustedRGB[0]), Math.Round(currentAdjustedRGB[1]), Math.Round(currentAdjustedRGB[2])), _taskDelay));
                            }
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
