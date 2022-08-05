using MiitsuColorController.Models;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiitsuColorController.Helper
{
    public class TwitchSocket : AbstractSocket
    {
        public static TwitchSocket Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TwitchSocket();
                }
                return _instance;
            }
        }

        public string TwitchAuthToken { set; get; }
        public string Username { set; get; }
        public async void Connect()
        {
            StatusString = _resourceLoader.GetString(StringEnum.Connecting);
            if (!IsConnected)
            {
                IsAuthorized = false;
                await Task.Run(() =>
                {
                    try { _ = _socket.ConnectAsync(new Uri(_TWITCH_IRC_URL), new CancellationTokenSource(5000).Token); }
                    catch (Exception) { }
                    while (_socket.State == WebSocketState.Connecting)
                    {
                        _dispathcerQueue.TryEnqueue(() =>
                        {
                            OnPropertyChanged("IsNotInUse");
                        });
                        Task.Delay(500).Wait();
                    }
                    if (_socket.State == WebSocketState.Closed)
                    {
                        _dispathcerQueue.TryEnqueue(() =>
                        {
                            StatusString = _resourceLoader.GetString(StringEnum.TwitchCannotConnect);
                        });
                        _socket = new ClientWebSocket();
                        return;
                    }
                    SendRequest("PASS oauth:" + TwitchAuthToken, _resourceLoader.GetString(StringEnum.TwitchAuthTokenProblem));
                    SendRequest("NICK " + Username, _resourceLoader.GetString(StringEnum.TwitchLoginFailed));
                    SendRequest("JOIN #" + Username, _resourceLoader.GetString(StringEnum.TwitchLoginFailed));
                    string result;
                    byte[] receiveData = new byte[4096];
                    ArraySegment<byte> recvBuff = new(receiveData);
                    while (_socket.State == WebSocketState.Open && !_cancelRecv.Token.IsCancellationRequested)
                    {
                        result = Receive(recvBuff, _cancelRecv.Token).Result;
                        if (result.Contains(":tmi.twitch.tv NOTICE * :Login authentication failed"))
                        {
                            _dispathcerQueue.TryEnqueue(() =>
                            {
                                ResetConnectionStatus(_resourceLoader.GetString(StringEnum.TwitchAuthTokenProblem));
                            });
                            break;
                        }
                        else if (result.Contains(":tmi.twitch.tv NOTICE * :Improperly formatted auth"))
                        {
                            _dispathcerQueue.TryEnqueue(() =>
                            {
                                ResetConnectionStatus(_resourceLoader.GetString(StringEnum.TwitchAuthTokenProblem));
                            });
                            break;
                        }
                        if (result.Contains(":tmi.twitch.tv NOTICE * :Invalid NICK"))
                        {
                            _dispathcerQueue.TryEnqueue(() =>
                            {
                                ResetConnectionStatus(_resourceLoader.GetString(StringEnum.TwitchLoginFailed));
                            });
                            break;
                        }
                        else if (result.Contains(":tmi.twitch.tv 001"))
                        {
                            _dispathcerQueue.TryEnqueue(() =>
                            {
                                IsAuthorized = true;
                                StatusString = _resourceLoader.GetString(StringEnum.SuccessfullyConnected);
                                OnPropertyChanged("IsConnected");
                            });
                            StartReceiving();
                            ResourceManager manager = ResourceManager.Instance;
                            manager.StringResourceDictionary[ResourceKey.TwitchAuthToken] = TwitchAuthToken;
                            manager.StringResourceDictionary[ResourceKey.TwitchUserName] = Username;
                            break;
                        }
                    }
                });
            }
        }

        internal void Exit()
        {
            Disconnect();
            ResourceManager manager = ResourceManager.Instance;
            manager.BoolResourceDictionary[ResourceKey.ConnectTwitchOnStart] = ConnectOnStartup;
            manager.BoolResourceDictionary[ResourceKey.ReconnectTwitchOnError] = AutoReconnect;
        }

        private static TwitchSocket _instance = null;
        private string _TWITCH_IRC_URL = "ws://irc-ws.chat.twitch.tv:80";
        private TwitchSocket() : base()
        {
            ResourceManager manager = ResourceManager.Instance;
            if (manager.StringResourceDictionary.TryGetValue(ResourceKey.TwitchAuthToken, out string tmp))
            {
                TwitchAuthToken = tmp;
            }
            else
            {
                TwitchAuthToken = "";
            }
            if (manager.StringResourceDictionary.TryGetValue(ResourceKey.TwitchUserName, out tmp))
            {
                Username = tmp;
            }
            else
            {
                Username = "";
            }
            ConnectOnStartup = manager.BoolResourceDictionary.TryGetValue(ResourceKey.ConnectTwitchOnStart, out bool boolTmp) && boolTmp;
            if (ConnectOnStartup)
            {
                Connect();
            }
        }
        private async Task<string> Receive(ArraySegment<byte> recvBuff, CancellationToken token)
        {
            WebSocketReceiveResult receiveFlags;
            string result = "";
            do
            {
                try { receiveFlags = await _socket.ReceiveAsync(recvBuff, token); }
                catch (OperationCanceledException)
                {
                    ResetConnectionStatus(_resourceLoader.GetString(StringEnum.TwitchCannotConnect));
                    return "";
                }
                catch (WebSocketException)
                {
                    ResetConnectionStatus(_resourceLoader.GetString(StringEnum.TwitchLoginFailed));
                    return "";
                }
                catch (InvalidOperationException)
                {
                    ResetConnectionStatus(_resourceLoader.GetString(StringEnum.TwitchLoginFailed));
                    return "";
                }
                result += Encoding.UTF8.GetString(recvBuff.Array, 0, receiveFlags.Count);
            } while (!receiveFlags.EndOfMessage);
            return result;
        }

        private void StartReceiving()
        {
            string result;
            byte[] receiveData = new byte[4096];
            ArraySegment<byte> recvBuff = new(receiveData);
            int startIndex = ("PRIVMSG #" + Username + " :").Length;
            CancellationToken token = _cancelRecv.Token;
            FeatureManager featureManager = FeatureManager.Instance;
            while (!token.IsCancellationRequested && IsConnected)
            {
                result = Receive(recvBuff, token).Result;
                if (result.StartsWith("PING "))
                {
                    SendRequest("PONG " + result[5..], _resourceLoader.GetString(StringEnum.TwitchLoginFailed));
                    continue;
                }
                else if (result.Contains("PRIVMSG"))
                {
                    featureManager.EnqueueTwitchMessage(result[(result.IndexOf("PRIVMSG") + startIndex)..].Trim('\r', '\n'));
                }
            }
        }
    }
}