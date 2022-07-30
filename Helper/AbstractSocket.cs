using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace MiitsuColorController.Helper
{
    public abstract class AbstractSocket : ObservableObject
    {
        public bool IsAuthorized = false;
        protected DispatcherQueue _dispathcerQueue;
        protected ClientWebSocket _socket = null;
        protected bool AlreadyRetried = false;
        private CancellationTokenSource _cancelSend;
        private string _statusString = "未連結";
        protected bool _autoReconnect;
        protected CancellationTokenSource _cancelRecv;
        public bool ConnectOnStartup { get; set; }
        public bool IsConnected
        { get { return _socket != null && _socket.State == WebSocketState.Open && IsAuthorized; } }

        public bool IsNotInUse
        { get { return _socket != null && (_socket.State == WebSocketState.Closed || _socket.State == WebSocketState.None); } }

        public string StatusString
        { get { return _statusString; } set { _statusString = value; OnPropertyChanged(nameof(StatusString)); } }
        public AbstractSocket()
        {
            _cancelRecv = new CancellationTokenSource();
            _socket = new ClientWebSocket();
            _socket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);
            _dispathcerQueue = DispatcherQueue.GetForCurrentThread();
            _cancelSend = new CancellationTokenSource();
        }
        public async void Disconnect()
        {
            StatusString = "未連結";
            _cancelRecv.Cancel();
            _cancelRecv = new CancellationTokenSource();
            StopSending();
            IsAuthorized = false;
            if (_socket.State == WebSocketState.Open)
            {
                try
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", new CancellationTokenSource(500).Token);
                }
                catch (WebSocketException)
                {
                }
            }
            _socket = new ClientWebSocket();
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsNotInUse));
        }

        public void StopSending()
        {
            _cancelSend.Cancel();
            _cancelSend = new CancellationTokenSource();
        }

        protected void CheckConnection(string Message)
        {
            StopSending();
            _socket = new ClientWebSocket();
            _socket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);
            _dispathcerQueue.TryEnqueue(() =>
            {
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(IsNotInUse));
                if (StatusString != "未連結")
                {
                    StatusString = Message;
                }
            });
            return;
        }
        protected async void SendRequest(string Message, string ErrorMessage)
        {
            if (_socket.State == WebSocketState.Open)
            {
                byte[] byteData = Encoding.UTF8.GetBytes(Message);
                ArraySegment<byte> sendBuff = new(byteData);
                try { await _socket.SendAsync(sendBuff, WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token); }
                catch (OperationCanceledException) { CheckConnection(ErrorMessage); }
                catch (System.Net.Sockets.SocketException) { CheckConnection(ErrorMessage); }
            }
        }
    }
}