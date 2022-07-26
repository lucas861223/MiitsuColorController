using MiitsuColorController.Models;
using Microsoft.UI.Dispatching;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MiitsuColorController.Helper
{
    public abstract class AbstractSocket : ObservableObject
    {
        public bool IsAuthorized = false;
        protected DispatcherQueue _dispathcerQueue;
        protected ClientWebSocket _socket = null;
        protected bool AlreadyRetried = false;
        private CancellationTokenSource _cancelSend;
        private string _statusString;
        protected bool _autoReconnect;
        protected CancellationTokenSource _cancelRecv;
        protected Windows.ApplicationModel.Resources.ResourceLoader _resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
        public bool ConnectOnStartup { get; set; }
        public bool IsConnected
        { get { return _socket != null && _socket.State == WebSocketState.Open && IsAuthorized; } }
        public bool IsNotInUse
        { get { return _socket != null && (_socket.State == WebSocketState.Closed || _socket.State == WebSocketState.None); } }
        public string StatusString
        { get { return _statusString; } set { _statusString = value; OnPropertyChanged(nameof(StatusString)); } }
        public bool AutoReconnect
        {
            get { return _autoReconnect; }
            set { _autoReconnect = value; OnPropertyChanged(nameof(AutoReconnect)); }
        }

        protected event Action LostConnectionEvent;

        public AbstractSocket()
        {
            _cancelRecv = new CancellationTokenSource();
            _socket = new ClientWebSocket();
            _socket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);
            _dispathcerQueue = DispatcherQueue.GetForCurrentThread();
            _cancelSend = new CancellationTokenSource();
            _statusString = _resourceLoader.GetString(StringEnum.NotConnected);
        }
        public async void Disconnect()
        {
            StatusString = _resourceLoader.GetString(StringEnum.NotConnected);
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

        protected void ResetConnectionStatus(string Message)
        {
            StopSending();
            _socket = new ClientWebSocket();
            _socket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);
            _dispathcerQueue.TryEnqueue(() =>
            {
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(IsNotInUse));
                if (StatusString != _resourceLoader.GetString(StringEnum.NotConnected))
                {
                    StatusString = Message;
                }
            });
            LostConnectionEvent?.Invoke();
        }
        protected async void SendRequest(string Message, string ErrorMessage)
        {
            if (_socket.State == WebSocketState.Open)
            {
                byte[] byteData = Encoding.UTF8.GetBytes(Message);
                ArraySegment<byte> sendBuff = new(byteData);
                try { await _socket.SendAsync(sendBuff, WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token); }
                catch (OperationCanceledException) { ResetConnectionStatus(ErrorMessage); }
                catch (System.Net.Sockets.SocketException) { ResetConnectionStatus(ErrorMessage); }
            }
        }
    }
}