﻿using MiitsuColorController.Models;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MiitsuColorController.Helper
{
    public class VTSSocket : AbstractSocket
    {
        public static VTSSocket Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VTSSocket();
                }
                return _instance;
            }
        }
        public string VTS_Websocket_URL { get; set; }
        private ConcurrentQueue<Tuple<string, int>> _taskQueue = new();
        private static VTSSocket _instance = null;
        private CancellationTokenSource _cancelSend = new();
        private JsonSerializerOptions _jsonSerializerOptions = new();
        private EventWaitHandle _sendEWH = new EventWaitHandle(true, EventResetMode.ManualReset);
        private ResourceManager _resourceManager = ResourceManager.Instance;
        //maybe when I actually need the requestid
        //private ConcurrentDictionary<string, bool> _responseNeeded = new();
        public bool AutoReconnect
        {
            get { return _autoReconnect; }
            set { _autoReconnect = value; OnPropertyChanged(nameof(AutoReconnect)); }
        }

        public event Action NewModelEvent;

        private VTSSocket() : base()
        {
            _jsonSerializerOptions.IncludeFields = true;
            _jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            ResourceManager manager = ResourceManager.Instance;
            if (manager.StringResourceDictionary.TryGetValue(ResourceKey.VTSWebsocketURI, out string tmp))
            {
                VTS_Websocket_URL = tmp;
            }
            else
            {
                VTS_Websocket_URL = "";
            }
            ConnectOnStartup = manager.BoolResourceDictionary.TryGetValue(ResourceKey.ConnectVTSOnStart, out bool boolTmp) && boolTmp;
            if (ConnectOnStartup)
            {
                Connect();
            }
        }
        public void Authorize()
        {
            Authorize(false);
        }

        public void Authorize(bool ForceNewToken)
        {
            VTSAuthData AuthRequest = new();
            AuthRequest.data.pluginName = "Miitsuba Coloring Controller";
            AuthRequest.data.pluginDeveloper = "weichichi";
            AuthRequest.data.pluginIcon = ResourceManager.GetAppIcon();
            VTSAuthData response = new();
            if (ForceNewToken || !_resourceManager.StringResourceDictionary.ContainsKey(ResourceKey.VTSAuthToken))
            {
                _dispathcerQueue.TryEnqueue(() =>
                {
                    StatusString = "等候授權...";
                });
                SendMessage(JsonSerializer.Serialize(AuthRequest, typeof(VTSAuthData), _jsonSerializerOptions));
                //SendRequest(JsonSerializer.Serialize(AuthRequest, typeof(VTSAuthData), _jsonSerializerOptions),
                //    "連結失敗- 找不到Vtube Studio\n有開Vtube Studio嗎?\t有開啟API嗎?\n網址和埠號有打對嗎?");
            }
            else
            {
                UserAuthenticated(AuthRequest, true);
                //SendRequest(JsonSerializer.Serialize(AuthRequest, typeof(VTSAuthData), _jsonSerializerOptions),
                //    "連結失敗- 找不到Vtube Studio\n有開Vtube Studio嗎?\t有開啟API嗎?\n網址和埠號有打對嗎?");
            }
        }

        public void AuthenticationResultReceived(VTSAuthData AuthResponse)
        {
            IsAuthorized = AuthResponse.data.authenticated;
            if (!AuthResponse.data.authenticated)
            {
                Authorize(true);
            }
            else
            {
                _dispathcerQueue.TryEnqueue(() =>
                {
                    OnPropertyChanged("IsConnected");
                    if (IsConnected)
                    {
                        StatusString = "連結成功";
                    }
                    OnPropertyChanged("IsNotInUse");
                    GetModelInformation();
                });
            }
        }

        public async void Connect()
        {
            StatusString = "連結中...";
            await Task.Run(() =>
            {
                //connection timeout
                try { _ = _socket.ConnectAsync(new Uri(VTS_Websocket_URL), new CancellationTokenSource(5000).Token); }
                catch (Exception) { }
                while (_socket.State == WebSocketState.Connecting)
                {
                    _dispathcerQueue.TryEnqueue(() =>
                    {
                        OnPropertyChanged("IsNotInUse");
                    });
                    Task.Delay(500).Wait();
                }
                if (_socket.State != WebSocketState.Open)
                {
                    //hacky solution- trying to see if it's cancelled
                    if (StatusString != "未連結")
                    {
                        _dispathcerQueue.TryEnqueue(() =>
                        {
                            StatusString = "連結失敗- 找不到Vtube Studio\n有開Vtube Studio嗎?\t有開啟API嗎?\n網址和埠號有打對嗎?\t0.0.0.0不行的話試試看localhost";
                        });
                    }
                    _socket = new ClientWebSocket();
                    return;
                }
                StartReceiving();
                StartSending();
                VTSStateData stateRequest = new();
                SendMessage(JsonSerializer.Serialize(stateRequest, typeof(VTSStateData), _jsonSerializerOptions));
                System.Diagnostics.Debug.WriteLine("opened connection, sent state request");
            });
        }

        public int GetQueueSize()
        {
            return _taskQueue.Count;
        }

        private void UserAuthenticated(VTSAuthData authData, bool success)
        {
            if (!success)
            {
                _dispathcerQueue.TryEnqueue(() =>
                {
                    StatusString = "授權失敗";
                });
            }
            else
            {
                _resourceManager.StringResourceDictionary.TryGetValue(ResourceKey.VTSAuthToken, out authData.data.authenticationToken);
                authData.messageType = "AuthenticationRequest";
                SendMessage(JsonSerializer.Serialize(authData, typeof(VTSAuthData), _jsonSerializerOptions));
            }
        }

        private void ReceivedStateRequest(VTSStateData stateResposne)
        {
            IsAuthorized = stateResposne.data.currentSessionAuthenticated;
            System.Diagnostics.Debug.WriteLine("state request received, autorization status \t" + IsAuthorized);
            if (!IsAuthorized)
            {
                Authorize();
            }
        }

        public async void StartReceiving()
        {
            await Task.Run(async () =>
            {
                CancellationToken token = _cancelRecv.Token;
                VTSMessageData response = null;
                WebSocketReceiveResult receiveFlags = null;
                string receivedString = "";
                byte[] receiveData = new byte[4096];
                ArraySegment<byte> recvBuff = new(receiveData);
                string[] jsonObjects; //in case there's two json objects in a single message
                while (!token.IsCancellationRequested)
                {
                    if (_socket.State != WebSocketState.Open)
                    {
                        CheckConnection("失去與VTube Studio的連結");
                    }
                    receivedString = "";
                    do
                    {
                        try { receiveFlags = await _socket.ReceiveAsync(recvBuff, token); }
                        catch (OperationCanceledException)
                        {
                            CheckConnection("失去與Vtube Studio的連結");
                        }
                        receivedString += Encoding.UTF8.GetString(receiveData, 0, receiveFlags.Count);
                    } while (!receiveFlags.EndOfMessage);
                    System.Diagnostics.Debug.WriteLine("!!" + receivedString);

                    jsonObjects = receivedString.Replace("}{", "}|{").Split("|");
                    foreach (string jsonString in jsonObjects)
                    {
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        try
                        {
                            response = JsonSerializer.Deserialize<VTSMessageData>(jsonString, _jsonSerializerOptions);
                        }
                        catch (JsonException)
                        {
                            continue;
                        }
                        switch (response.messageType)
                        {
                            case "AuthenticationResponse":
                                VTSAuthData VTSAuthDataObject = Newtonsoft.Json.JsonConvert.DeserializeObject<VTSAuthData>(jsonString);
                                AuthenticationResultReceived(VTSAuthDataObject);
                                break;
                            case "AuthenticationTokenResponse":
                                VTSAuthData VTSAuthenticationTokenObject = Newtonsoft.Json.JsonConvert.DeserializeObject<VTSAuthData>(jsonString);
                                _resourceManager.StringResourceDictionary[ResourceKey.VTSAuthToken] = VTSAuthenticationTokenObject.data.authenticationToken;
                                UserAuthenticated(VTSAuthenticationTokenObject, false);
                                break;
                            case "APIStateResponse":
                                VTSStateData VTSStateDataObject = Newtonsoft.Json.JsonConvert.DeserializeObject<VTSStateData>(jsonString);
                                ReceivedStateRequest(VTSStateDataObject);
                                break;
                            case "ArtMeshListResponse":
                                VTSArtMeshListData VTSArtMeshListDataObject = Newtonsoft.Json.JsonConvert.DeserializeObject<VTSArtMeshListData>(jsonString);
                                ReceivedModelMeshes(VTSArtMeshListDataObject);
                                break;
                            case "CurrentModelResponse":
                                VTSCurrentModelData VTSCurrentModelDataObject = Newtonsoft.Json.JsonConvert.DeserializeObject<VTSCurrentModelData>(jsonString);
                                ReceivedModelInformation(VTSCurrentModelDataObject);
                                break;
                            case "APIError":
                                VTSErrorData error = JsonSerializer.Deserialize<VTSErrorData>(jsonString, _jsonSerializerOptions);
                                switch (Enum.ToObject(typeof(ErrorID), error.data.errorID))
                                {
                                    case ErrorID.TokenRequestDenied:
                                        UserAuthenticated(null, false);
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            });
        }

        public void ClearQueue()
        {
            _taskQueue.Clear();
        }

        public void Exit()
        {
            Disconnect();
            ResourceManager manager = ResourceManager.Instance;
            manager.StringResourceDictionary[ResourceKey.VTSWebsocketURI] = VTS_Websocket_URL;
            manager.BoolResourceDictionary[ResourceKey.ConnectVTSOnStart] = ConnectOnStartup;
        }

        public void GetModelInformation()
        {
            SendMessage(JsonSerializer.Serialize(new VTSCurrentModelData(), typeof(VTSCurrentModelData), _jsonSerializerOptions));
            //SendRequest(JsonSerializer.Serialize(currentModelData, typeof(VTSCurrentModelData), _jsonSerializerOptions),
            //    "失去與VTube Studio的連結");
        }

        private void ReceivedModelInformation(VTSCurrentModelData currentModelData)
        {
            if (ResourceManager.Instance.CurrentModelInformation.ID != currentModelData.data.modelID)
            {
                VTSArtMeshListData currentModelArtmesh = new();
                SendMessage(JsonSerializer.Serialize(currentModelArtmesh, typeof(VTSArtMeshListData), _jsonSerializerOptions));
                //SendRequest(JsonSerializer.Serialize(currentModelArtmesh, typeof(VTSArtMeshListData), _jsonSerializerOptions),
                //    "失去與VTube Studio的連結");
                ResourceManager.Instance.UpdateCurrentModelInformation(currentModelData.data);
            }
        }

        private void ReceivedModelMeshes(VTSArtMeshListData meshesData)
        {
            //SendRequest(JsonSerializer.Serialize(currentModelArtmesh, typeof(VTSArtMeshListData), _jsonSerializerOptions),
            //    "失去與VTube Studio的連結");
            ResourceManager.Instance.UpdateCurrentModelMeshes(meshesData.data);
            NewModelEvent();
        }


        public void SendMessage(string message)
        {
            SendMessage(message, 0);
        }

        public void SendMessage(string message, int delay)
        {
            if (_socket.State == WebSocketState.Open)
            {
                _taskQueue.Enqueue(new Tuple<string, int>(message, delay));
                _sendEWH.Set();
            }
        }

        public new void StopSending()
        {
            base.StopSending();
            _sendEWH.Set();
        }

        public new void Disconnect()
        {
            StopSending();
            base.Disconnect();
        }

        public async void StartSending()
        {
            await Task.Run(() =>
            {
                Tuple<string, int> task;
                CancellationToken token = _cancelSend.Token;
                while (!token.IsCancellationRequested)
                {
                    if (_taskQueue.TryDequeue(out task))
                    {
                        System.Diagnostics.Debug.WriteLine("sending " + task.Item1);
                        SendRequest(task.Item1, "失去與VTube Studio的連結");
                        Task.Delay(task.Item2).Wait();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("nothing to send, going to sleep");
                        _sendEWH.WaitOne();
                        System.Diagnostics.Debug.WriteLine("I woke up");
                        _sendEWH.Reset();
                        System.Diagnostics.Debug.WriteLine("Reset");
                    }
                }
            });
        }
        public class RequestError : Exception
        {
            public int ErrorID;

            public string ErrorMessage;

            public RequestError(int ErrorCode, string ErrorText)
                                    : base(string.Format("VTS returned with Error Code: {0}, {1}", ErrorCode, ErrorText))
            {
                ErrorID = ErrorCode;
                ErrorMessage = ErrorText;
            }
        }
    }
}