using System.Collections.Generic;
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
        public bool ArtMeshTintingActivated = false;
        private ResourceManager _resourceManager = ResourceManager.Instance;
        private VTSSocket _vtsSocket = VTSSocket.Instance;
        private TwitchSocket _twitchSocket = TwitchSocket.Instance;
        private bool _artmeshColoringFeature = false;
        private bool _hasQueue = false;
        public bool Suspended = false;
        private List<int[]> _artmeshEmoteHistory = new();

        private ArtmeshColoringSetting _setting;

        public bool ArtmeshColoringFeature { get { return _artmeshColoringFeature; } }

        private FeatureManager()
        {
            ReAssembleConfig();
        }

        public void ReAssembleConfig()
        {
            _setting = _resourceManager.LoadModelSetting();
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
                int startIndex = ("PRIVMSG #" + _resourceManager.StringResourceDictionary[ResourceKey.TwitchUserName] + " :").Length;
                string message;
                string[] tokens;
                while (ArtMeshTintingActivated)
                {
                    if (queue.TryDequeue(out message))
                    {
                        tokens = message.Substring(startIndex).Trim('\r', '\n').Split(" ");
                        int[] emotes = { 0, 0, 0 };
                        foreach (string token in tokens)
                        {
                            if (string.CompareOrdinal(token, _setting.BlueEmote) == 0)
                            {
                                emotes[2] += 1;
                            }
                            if (string.CompareOrdinal(token, _setting.RedEmote) == 0)
                            {
                                emotes[02] += 1;
                            }
                            if (string.CompareOrdinal(token, _setting.GreenEmote) == 0)
                            {
                                emotes[1] += 1;
                            }
                            _artmeshEmoteHistory.Add(emotes);
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
