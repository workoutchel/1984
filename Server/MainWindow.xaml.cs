using Server;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;



namespace WpfTcpServer
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            }

            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }



    public partial class MainWindow : Window
    {
        private ObservableCollection<ClientInfo> _clients = new ObservableCollection<ClientInfo>();

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private TcpListener? ListenerData;
        private TcpListener? ListenerScreen;

        public MainWindow()
        {
            InitializeComponent();
            ClientsListView.ItemsSource = _clients;

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            //
            //	Вставить сюда необходимые порты(один для отслеживания активности, второй для передачи скриншота)
            //			     |						  
            //			     ↓							  
            StartServer(1337, 1338);
        }



        private void StartServer(int portData, int portScreen)
        {
            ListenerData = new TcpListener(IPAddress.Any, portData);
            ListenerData.Start();

            ListenerScreen = new TcpListener(IPAddress.Any, portScreen);
            ListenerScreen.Start();

            Task.Run(() => ListenForClients(_cancellationTokenSource.Token));
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
                        _clients.Add(newClientInfo);
                    }
                });
            }
        }

        private async Task ListenForClients(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcpClientData = await ListenerData.AcceptTcpClientAsync(cancellationToken);
                    TcpClient tcpClientScreen = await ListenerScreen.AcceptTcpClientAsync(cancellationToken);

                    string clientData = await ReadClientDataAsync(tcpClientData);

                    ClientInfo clientInfo = ClientInfo.ParseClientInfo(clientData);

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var existingClient = _clients.FirstOrDefault(c => c.IP == clientInfo.IP);

                        if (existingClient != null)
                        {
                            existingClient.Connect(tcpClientData, tcpClientScreen);
                        }
                        else
                        {
                            clientInfo.Connect(tcpClientData, tcpClientScreen);
                            _clients.Add(clientInfo);
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());

                    if (cancellationToken.IsCancellationRequested) break;
                }
            }
        }

        private static async Task<string> ReadClientDataAsync(TcpClient tcpClient)
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            return System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }



        private void RequestScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var clientIP = button?.Tag as string;

            ClientInfo client = _clients.FirstOrDefault(x => x.IP == clientIP);

            if (client != null && client.IsConnected)
            {
                Task.Run(() => client.WaitScreenshotAsync());

                client.SendScreenshotRequest();
            }
        }
    }
}


