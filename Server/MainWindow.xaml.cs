using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using Server;
using System.Windows.Controls;

namespace WpfTcpServer
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<ClientInfo> _clients = new ObservableCollection<ClientInfo>();
        private Thread? _serverThread;
        private TcpListener? _server;

        public MainWindow()
        {
            InitializeComponent();
            ClientsListView.ItemsSource = _clients;
            StartServer();
        }




        private void AddOrUpdateClientInfo(string data) 
        {
            var newClientInfo = ClientInfo.ParseClientInfo(data);

            if (newClientInfo != null)
            {
                Dispatcher.Invoke(() =>
                {
                    var existingClient = _clients.FirstOrDefault(client => client.IP == newClientInfo.IP);

                    if (existingClient != null)
                    {
                        existingClient.UpdateLastActiveTime(newClientInfo.LastActiveTime);
                    }
                    else
                    {
                        newClientInfo.IsConnected = true;
                        _clients.Add(newClientInfo);
                    }
                });
            }
        }

        private void StartServer()
        {
            int port = 1337; //ВЫБИРАЕМ НЕОБХОДИМЫЙ ПОРТ
             
            _server = new TcpListener(IPAddress.Any, port);

            _server.Start();

            _serverThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        TcpClient client = _server.AcceptTcpClient();
                        Thread clientThread = new Thread(() => HandleClient(client));
                        clientThread.Start();
                    }
                    catch (SocketException ex)
                    {
                        MessageBox.Show($"Ошибка сокета: {ex.Message}");
                        break;
                    }
                }
            });
            _serverThread.IsBackground = true; 

            _serverThread.Start();
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            {
                string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(); 
               
                while (true)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        AddOrUpdateClientInfo(dataReceived);
                    }
                    catch (Exception ex)
                    {
                            MessageBox.Show($"{ex}");

                            break;
                    }
                }
            }
        }




        private void RequestScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var clientIP = button?.Tag as string;
           
            if (!string.IsNullOrEmpty(clientIP))
            {
                //ДАЛЬНЕЙШАЯ РЕАЛИЗАЦИЯ ЗАПРОСА СКРИНА
            }
        }
    }
}


