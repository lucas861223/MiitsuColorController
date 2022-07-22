using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;

namespace MiitsuColorController.Helper
{

    public abstract class AbstractSocket : ObservableObject
    {
        private CancellationTokenSource _cancelSend;
        private string _statusString = "未連結";
        protected bool AlreadyRetried = false;
        protected ClientWebSocket _socket = null;
        protected DispatcherQueue _dispathcerQueue;
        public bool AutoReconnect { get; set; }
        public bool ConnectOnStartup { get; set; }
        public bool IsAuthorized = false;
        public bool IsConnected { get { return _socket != null && _socket.State == WebSocketState.Open && IsAuthorized; } }
        public bool IsNotInUse { get { return _socket != null && (_socket.State == WebSocketState.Closed || _socket.State == WebSocketState.None); } }
        public string StatusString { get { return _statusString; } set { _statusString = value; OnPropertyChanged(nameof(StatusString)); } }

        public AbstractSocket()
        {
            _socket = new ClientWebSocket();
            _socket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);
            _dispathcerQueue = DispatcherQueue.GetForCurrentThread();
            _cancelSend = new CancellationTokenSource();
        }

        public async void Disconnect()
        {
            StatusString = "未連結";
            StopSending();
            IsAuthorized = false;
            if (_socket.State == WebSocketState.Open)
            {
                try
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", new CancellationTokenSource(500).Token);
                }
                catch (System.Net.WebSockets.WebSocketException)
                {

                }
            }
            _socket = new ClientWebSocket();
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsNotInUse));
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

        public void StopSending()
        {
            _cancelSend.Cancel();
            _cancelSend = new CancellationTokenSource();
        }


        protected async void StartSending(string ErrorMessage)
        {
            await new Task(() =>
            {
                CancellationToken token = _cancelSend.Token;
                string message = "";
                while (!token.IsCancellationRequested)
                {
                    if (!_sendQueue.IsEmpty && _socket.State == WebSocketState.Open && _sendQueue.TryDequeue(out message))
                    {
                        SendRequest(message, ErrorMessage);
                    }
                    Task.Delay(5).Wait();
                }
            });
        }

        protected async void SendRequest(string Message, string ErrorMessage)
        {
            if (_socket.State == WebSocketState.Open)
            {
                byte[] byteData = Encoding.UTF8.GetBytes(Message);
                System.ArraySegment<byte> sendBuff = new(byteData);
                try { await _socket.SendAsync(sendBuff, WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token); }
                catch (System.OperationCanceledException) { CheckConnection(ErrorMessage); }
            }
        }
    }

}
