using MiitsuColorController.Models;
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
        private CancellationTokenSource _cancelSend = new();
        private JsonSerializerOptions _jsonSerializerOptions = new();
        private static VTSSocket _instance = null;
        public ConcurrentQueue<string> SendQueue = new();
        public string VTS_Websocket_URL { get; set; }
        public ConcurrentQueue<Tuple<string, int>> TaskQueue = new();
        public event Action ConnectionEstablishedEvent;
        public event Action NewModelEvent;


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
                ConnectAndAuthorize();
            }
        }

        public void GetModelInformation()
        {
            if (_socket.State == WebSocketState.Open)
            {
                VTSCurrentModelData currentModelData = new();
                SendRequest(JsonSerializer.Serialize(currentModelData, typeof(VTSCurrentModelData), _jsonSerializerOptions),
                    "失去與VTube Studio的連結");
                if (!ReceiveResponse(currentModelData.requestID, typeof(VTSCurrentModelData), currentModelData))
                {
                    return;
                }
                if (ResourceManager.Instance.CurrentModelInformation.ID != currentModelData.data.modelID)
                {
                    VTSArtMeshListData currentModelArtmesh = new();
                    SendRequest(JsonSerializer.Serialize(currentModelArtmesh, typeof(VTSArtMeshListData), _jsonSerializerOptions),
                        "失去與VTube Studio的連結");
                    if (!ReceiveResponse(currentModelArtmesh.requestID, typeof(VTSArtMeshListData), currentModelArtmesh))
                    {
                        return;
                    }
                    ResourceManager.Instance.UpdateCurrentModelInformation(currentModelData.data, currentModelArtmesh.data);
                }
                NewModelEvent();
            }
        }
        public async void ConnectAndAuthorize()
        {
            StatusString = "連結中...";
            Microsoft.UI.Dispatching.DispatcherQueue queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            await Task.Run(() =>
            {
                if (_socket.State != WebSocketState.Open && _socket.State != WebSocketState.Connecting)
                {
                    EstablishConnection();
                }
                if (_socket.State == WebSocketState.Open && !IsAuthorized)
                {
                    IsAuthorized = Authorize();
                }
                _dispathcerQueue.TryEnqueue(() =>
                {
                    OnPropertyChanged("IsConnected");
                    if (IsConnected)
                    {
                        StatusString = "連結成功";
                    }
                    OnPropertyChanged("IsNotInUse");
                });
                if (IsConnected)
                {
                    GetModelInformation();
                    StartSending();
                }
            });
        }

        public void StartSending()
        {
            Tuple<string, int> task;
            CancellationToken token = _cancelSend.Token;
            while (!token.IsCancellationRequested && IsConnected)
            {
                if (TaskQueue.TryDequeue(out task))
                {
                    SendRequest(task.Item1, "失去與VTube Studio的連結");
                    Task.Delay(task.Item2).Wait();
                }
                else
                {
                    Task.Delay(100).Wait();
                }
            }
        }

        public void EstablishConnection()
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
            VTSStateData stateRequest = new();
            SendRequest(JsonSerializer.Serialize(stateRequest, typeof(VTSStateData), _jsonSerializerOptions),
                "連結失敗- 找不到Vtube Studio\n有開Vtube Studio嗎?\t有開啟API嗎?\n網址和埠號有打對嗎?");
            //receive error
            try { ReceiveResponse(stateRequest.requestID, typeof(VTSStateData), stateRequest); }
            catch (RequestError e) { Console.Write(e); }
            IsAuthorized = stateRequest.data.currentSessionAuthenticated;
        }

        public bool Authorize()
        {
            return Authorize(false);
        }

        public bool Authorize(bool ForceNewToken)
        {
            VTSAuthData AuthRequest = new();
            AuthRequest.data.pluginName = "Miitsuba Coloring Controller";
            AuthRequest.data.pluginDeveloper = "weichichi";
            ResourceManager manager = ResourceManager.Instance;
            AuthRequest.data.pluginIcon = ResourceManager.GetAppIcon();
            VTSAuthData response = new();
            if (ForceNewToken || !manager.StringResourceDictionary.ContainsKey(ResourceKey.VTSAuthToken))
            {
                _dispathcerQueue.TryEnqueue(() =>
                {
                    StatusString = "等候授權...";
                });
                SendRequest(JsonSerializer.Serialize(AuthRequest, typeof(VTSAuthData), _jsonSerializerOptions),
                    "連結失敗- 找不到Vtube Studio\n有開Vtube Studio嗎?\t有開啟API嗎?\n網址和埠號有打對嗎?");
                try
                {
                    if (!ReceiveResponse(AuthRequest.requestID, typeof(VTSAuthData), response, new CancellationToken()).Result)
                    {
                        return false;
                    }
                }
                catch (AggregateException e) when (e.InnerException is RequestError)
                {
                    _dispathcerQueue.TryEnqueue(() =>
                    {
                        StatusString = "授權失敗";
                    });
                    return false;
                }
                manager.StringResourceDictionary[ResourceKey.VTSAuthToken] = response.data.authenticationToken;
            }
            manager.StringResourceDictionary.TryGetValue(ResourceKey.VTSAuthToken, out AuthRequest.data.authenticationToken);
            AuthRequest.messageType = "AuthenticationRequest";
            SendRequest(JsonSerializer.Serialize(AuthRequest, typeof(VTSAuthData), _jsonSerializerOptions),
                "連結失敗- 找不到Vtube Studio\n有開Vtube Studio嗎?\t有開啟API嗎?\n網址和埠號有打對嗎?");
            try
            {
                if (!ReceiveResponse(AuthRequest.requestID, typeof(VTSAuthData), AuthRequest))
                {
                    return false;
                }
            }
            catch (AggregateException e) when (e.InnerException is RequestError) { return false; }
            if (!AuthRequest.data.authenticated && !ForceNewToken)
            {
                return Authorize(true);
            }
            return AuthRequest.data.authenticated;
        }


        public async Task<bool> ReceiveResponse(string RequestID, Type type, VTSMessageData result, CancellationToken token)
        {
            VTSMessageData response = null;
            WebSocketReceiveResult receiveFlags;
            string receivedString = "";
            byte[] receiveData = new byte[4096];
            ArraySegment<byte> recvBuff = new(receiveData);
            do
            {
                do
                {
                    try { receiveFlags = await _socket.ReceiveAsync(recvBuff, token); }
                    catch (OperationCanceledException)
                    {
                        CheckConnection("失去與Vtube Studio的連結");
                        return false;
                    }
                    receivedString += Encoding.UTF8.GetString(receiveData, 0, receiveFlags.Count);
                } while (!receiveFlags.EndOfMessage);
                try
                {
                    response = JsonSerializer.Deserialize<VTSMessageData>(receivedString, _jsonSerializerOptions);
                    if (response.messageType == "APIError")
                    {
                        VTSErrorData error = JsonSerializer.Deserialize<VTSErrorData>(receivedString, _jsonSerializerOptions);
                        throw new RequestError(error.data.errorID, error.data.message);
                    }
                }
                catch (NotSupportedException)
                {
                    receivedString = "";
                }
            } while (response.requestID != RequestID);
            switch (response.messageType)
            {
                case "AuthenticationResponse":
                case "AuthenticationTokenResponse":
                    VTSAuthData VTSAuthDataObject = Newtonsoft.Json.JsonConvert.DeserializeObject<VTSAuthData>(receivedString);
                    ((VTSAuthData)result).Copy(VTSAuthDataObject);
                    break;
                case "APIStateResponse":
                    VTSStateData VTSStateDataObject = Newtonsoft.Json.JsonConvert.DeserializeObject<VTSStateData>(receivedString);
                    ((VTSStateData)result).Copy(VTSStateDataObject);
                    break;
                case "ArtMeshListResponse":
                    VTSArtMeshListData VTSArtMeshListDataObject = Newtonsoft.Json.JsonConvert.DeserializeObject<VTSArtMeshListData>(receivedString);
                    ((VTSArtMeshListData)result).Copy(VTSArtMeshListDataObject);
                    break;
                case "CurrentModelResponse":
                    VTSCurrentModelData VTSCurrentModelDataObject = Newtonsoft.Json.JsonConvert.DeserializeObject<VTSCurrentModelData>(receivedString);
                    ((VTSCurrentModelData)result).Copy(VTSCurrentModelDataObject);
                    break;
            }
            return true;
        }

        public bool ReceiveResponse(string RequestID, Type type, VTSMessageData result)
        {
            return ReceiveResponse(RequestID, type, result, new CancellationTokenSource(5000).Token).Result;
        }

        class RequestError : Exception
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

        public void Exit()
        {
            Disconnect();
            ResourceManager manager = ResourceManager.Instance;
            manager.StringResourceDictionary[ResourceKey.VTSWebsocketURI] = VTS_Websocket_URL;
            manager.BoolResourceDictionary[ResourceKey.ConnectVTSOnStart] = ConnectOnStartup;
        }
    }
}
