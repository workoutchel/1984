using System.ComponentModel;
using System.Net.Sockets;

namespace Server
{
    public class ClientInfo : INotifyPropertyChanged
    {
        public string? IP { get; }
        public string? UserName { get; }
        public string? DomainName { get; }
        public string? HostName { get; }
        public string? LastActiveTime { get; private set; }

        public bool IsConnected { get;  set; }

        private TcpClient? _dataClient;
        private NetworkStream? _dataStream;

        private ClientInfo(string? iP, string? userName, string? domainName, string? hostName, string? lastActiveTime)
        {
            IP = iP;
            UserName = userName;
            DomainName = domainName;
            HostName = hostName;
            LastActiveTime = lastActiveTime;
        }

        public static ClientInfo? ParseClientInfo(string data)
        {
            string[] parts = data.Split('|');
            if (parts.Length == 5)
            {
                return new ClientInfo(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), parts[3].Trim(), parts[4].Trim());
            }
            return null;
        }

        public void UpdateLastActiveTime(string? lastActiveTime)
        {
            LastActiveTime = lastActiveTime;
            OnPropertyChanged(nameof(LastActiveTime));
        }

        public void Disconnect()
        {
            _dataStream?.Close();
            _dataClient?.Close();
            IsConnected = false;
            OnPropertyChanged(nameof(IsConnected));
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
