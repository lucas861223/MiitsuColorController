﻿using MiitsuColorController.Models;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiitsuColorController.Helper
{
    public class FeatureManager
    {
        public bool ArtMeshTintingActivated = false;
        public bool Suspended = false;
        private static FeatureManager _instance = null;
        private int[] _artmeshCurrentColor = { 0, 0, 0 };
        private ConcurrentQueue<int[]> _artmeshEmoteHistory = new();
        private ArtMeshColorTint _colortintHolder = new();
        private string _formatString;
        private bool _isInUse = false;
        private bool _isTesting = false;
        private JsonSerializerOptions _jsonSerializerOptions = new();
        private ResourceManager _resourceManager = ResourceManager.Instance;
        private ArtmeshColoringSetting _setting;
        private float _sRatio;
        private int _taskDelay;
        private TwitchSocket _twitchSocket = TwitchSocket.Instance;
        private float _vRatio;
        private VTSSocket _vtsSocket = VTSSocket.Instance;

        private FeatureManager()
        {
            _jsonSerializerOptions.IncludeFields = true;
            if (_vtsSocket.IsConnected)
            {
                ReAssembleConfig();
                if (_setting.Activated)
                {
                    StartTask();
                }
            }
            //_jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        }

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

        public void ActivateArtmeshColoring()
        {
            if (!ArtMeshTintingActivated)
            {
                ArtMeshTintingActivated = true;
                ReAssembleConfig();
            }
        }

        public ArtmeshColoringSetting GetSetting()
        {
            return _setting;
        }

        public void MakeFormatString(ArtmeshColoringSetting setting)
        {
            VTSColorTintData request = new VTSColorTintData();
            request.data.artMeshMatcher = new ArtMeshMatcher();
            if (setting.SelectedArtMesh.Count > 0)
            {
                request.data.artMeshMatcher.nameExact = setting.SelectedArtMesh.ToArray();
            }
            if (setting.SelectedTag.Count > 0)
            {
                request.data.artMeshMatcher.tagExact = setting.SelectedTag.ToArray();
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
        }

        public void ReAssembleConfig(ArtmeshColoringSetting setting)
        {
            Suspended = true;
            MakeFormatString(setting);
            UpdateTestingParameters(setting);
            Suspended = false;
        }

        public void ReAssembleConfig()
        {
            _setting = _resourceManager.LoadModelSetting();
            ReAssembleConfig(_setting);
        }

        public void ResumeFeatures()
        {
            Suspended = false;
            _twitchSocket.ReceiveQueue.Clear();
        }

        public async void StartTask()
        {
            if (!_isInUse)
            {
                await Task.Run(() =>
                {
                    _isInUse = true;
                    ConcurrentQueue<string> queue = _twitchSocket.ReceiveQueue;
                    queue.Clear();
                    ColorTint colorTintTmp = new ColorTint() { colorA = 0 };
                    string message;
                    string[] tokens;
                    float[] rgb = { 0f, 0f, 0f };
                    int rgbSum;
                    float[] difference = { 0, 0, 0 };
                    float[] currentAdjustedRGB = { 255, 255, 255 };
                    ReAssembleConfig();
                    while (_setting.Activated)
                    {
                        if (queue.TryDequeue(out message))
                        {
                            tokens = message.Split(" ");
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
                                    _artmeshEmoteHistory.TryDequeue(out emotes);
                                    _artmeshCurrentColor[0] -= emotes[0];
                                    _artmeshCurrentColor[1] -= emotes[1];
                                    _artmeshCurrentColor[2] -= emotes[2];
                                }
                                rgbSum = _artmeshCurrentColor[0] + _artmeshCurrentColor[1] + _artmeshCurrentColor[2];
                                rgb[0] = (255f * (_artmeshCurrentColor[0] / rgbSum));
                                rgb[1] = (255f * (_artmeshCurrentColor[1] / rgbSum));
                                rgb[2] = (255f * (_artmeshCurrentColor[2] / rgbSum));
                                ColorHelper.RBGToAdjustedColorTint(rgb, _sRatio, _setting.MinimumS, _vRatio, _setting.MinimumV, ref _colortintHolder);
                                if (_setting.MessageHandlingMethod == 0)
                                {
                                    int leftStep = _vtsSocket.GetQueueSize();
                                    currentAdjustedRGB[0] -= difference[0] * (_setting.Interpolation + 1 - leftStep);
                                    currentAdjustedRGB[1] -= difference[1] * (_setting.Interpolation + 1 - leftStep);
                                    currentAdjustedRGB[2] -= difference[2] * (_setting.Interpolation + 1 - leftStep);
                                    _vtsSocket.ClearQueue();
                                }
                                difference[0] = _colortintHolder.colorR - currentAdjustedRGB[0] / (_setting.Interpolation + 1);
                                difference[1] = _colortintHolder.colorG - currentAdjustedRGB[1] / (_setting.Interpolation + 1);
                                difference[2] = _colortintHolder.colorB - currentAdjustedRGB[2] / (_setting.Interpolation + 1);
                                for (int i = 0; i <= _setting.Interpolation; i++)
                                {
                                    currentAdjustedRGB[0] += difference[0];
                                    currentAdjustedRGB[1] += difference[1];
                                    currentAdjustedRGB[2] += difference[2];
                                    _vtsSocket.SendMessage(String.Format(_formatString, Math.Round(currentAdjustedRGB[0]), Math.Round(currentAdjustedRGB[1]), Math.Round(currentAdjustedRGB[2])), _taskDelay);
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
                    _isInUse = false;
                });
            }
        }

        public async void StartTesting(ArtmeshColoringSetting setting)
        {
            if (!_isTesting)
            {
                await Task.Run(() =>
                {
                    _isTesting = true;
                    SuspendFeatures();
                    ReAssembleConfig(setting);
                    int difference = 30;
                    ColorTint colorTintTmp = new ColorTint() { colorA = 0 };
                    float[] rgb = { 0f, 0f, 0f };
                    bool iRed = false, iBlue = false, iGreen = false;
                    while (_isTesting)
                    {
                        if (!iRed && !iGreen && !iBlue)
                        {
                            rgb[0] += difference;
                            if (rgb[0] >= 255)
                            {
                                rgb[0] = 255;
                                iRed = true;
                            }
                        }
                        else if (iRed && !iGreen && !iBlue)
                        {
                            rgb[1] += difference;
                            if (rgb[1] >= 255)
                            {
                                rgb[1] = 255;
                                iGreen = true;
                            }
                        }
                        else if (iRed && iGreen && !iBlue)
                        {
                            rgb[2] += difference;
                            if (rgb[2] >= 255)
                            {
                                rgb[2] = 255;
                                iBlue = true;
                            }
                        }
                        else if (iRed && iGreen && iBlue)
                        {
                            rgb[0] -= difference;
                            if (rgb[0] <= 0)
                            {
                                rgb[0] = 0;
                                iRed = false;
                            }
                        }
                        else if (!iRed && iGreen && iBlue)
                        {
                            rgb[1] -= difference;
                            if (rgb[1] <= 0)
                            {
                                rgb[1] = 0;
                                iGreen = false;
                            }
                        }
                        else if (!iRed && !iGreen && iBlue)
                        {
                            rgb[2] -= difference;
                            if (rgb[2] <= 0)
                            {
                                rgb[2] = 0;
                                iBlue = false;
                            }
                        }
                        ColorHelper.RBGToAdjustedColorTint(rgb, _sRatio, _setting.MinimumS, _vRatio, _setting.MinimumV, ref _colortintHolder);
                        _vtsSocket.SendMessage(String.Format(_formatString, _colortintHolder.colorR, _colortintHolder.colorG, _colortintHolder.colorB));
                        Task.Delay(_taskDelay).Wait();
                    }
                });
            }
        }

        public void StopTesting()
        {
            _isTesting = false;
            _vtsSocket.SendMessage(String.Format(_formatString, 255, 255, 255));
            ReAssembleConfig(_setting);
            ResumeFeatures();
        }

        public void SuspendFeatures()
        {
            Suspended = true;
        }

        public void UpdateSelections(ArtmeshColoringSetting setting)
        {
            //reset tinted parts
            _vtsSocket.SendMessage(String.Format(_formatString, 255, 255, 255), 0);
            MakeFormatString(setting);
        }

        public void UpdateTestingParameters(ArtmeshColoringSetting setting)
        {
            _sRatio = (setting.MaximumS - setting.MinimumS) / 100f;
            _vRatio = (setting.MaximumV - setting.MinimumV) / 100f;
            _taskDelay = setting.Duration / (setting.Interpolation + 1);
        }
    }
}