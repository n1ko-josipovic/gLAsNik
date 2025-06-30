using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Hub.Client.MVVM.Core;
using Hub.Client.MVVM.Model;
using Hub.Client.Net;

using Hub.Networking;
using Microsoft.Win32;

namespace Hub.Client.MVVM.ViewModel;
class ClientViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ObservableCollection<UserModel> Users { get; set; }
    public ObservableCollection<ChatBlockModel> Messages { get; set; }

    public RelayCommand ToggleConnectionCommand { get; set; }
    public RelayCommand SendMessageCommand { get; set; }
    public ICommand PickFileCommand { get; }

    private string _statusMessage;
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
            UpdateStatusColor();
        }
    }
    
    private Brush _statusColor;
    public Brush StatusColor
    {
        get => _statusColor;
        set
        {
            if (_statusColor != value)
            {
                _statusColor = value;
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }
    private void UpdateStatusColor()
    {
        if (StatusMessage != "gLAsNik - klijent" && StatusMessage != "Moguće je povezati se.")
            StatusColor = new SolidColorBrush(Colors.Red);
        else
            StatusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));
    }

    private bool _isCheckingServer;

    private string _username;
    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged(nameof(Username));
            ToggleConnectionCommand?.RaiseCanExecuteChanged();

            if (string.IsNullOrWhiteSpace(_username))
            {
                StatusMessage = "Ime je obavezno.";
            }
            else if (_username.Length > 10)
            {
                StatusMessage = "Ime je predugačko.";
            }
            else
            {
                StatusMessage = "Moguće je povezati se.";

                if (!_isCheckingServer)
                {
                    _isCheckingServer = true;
                    CheckServerAvailability();
                }
            }
        }
    }

    private string _message;
    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            OnPropertyChanged(nameof(Message));
            SendMessageCommand?.RaiseCanExecuteChanged();
        }
    }

    private Server _server = new Server();

    public ClientViewModel()
    {
        Users = new ObservableCollection<UserModel>();
        Messages = new ObservableCollection<ChatBlockModel>();

        _server.connectedEvent += async () => await UserConnected();
        _server.messageReceivedEvent += async () => await MessageReceived();
        _server.fileReceivedEvent += async () => await FileReceived();
        _server.fileDownloadEvent += async () => await FileDownload();
        _server.userDisconnectEvent += async () => await RemoveUser();
        _server.disconnectedEvent += HandleDisconnect;

        StatusMessage = "gLAsNik - klijent";

        ToggleConnectionCommand = new RelayCommand(
            (obj) => ToggleConnectionAsync(),
            (obj) => !string.IsNullOrEmpty(Username) && !(Username.Length > 10) && NetworkUtils.IsServerActive(7750)
        );

        PickFileCommand = new RelayCommand(async _ => await PickFile());

        SendMessageCommand = new RelayCommand(
            async (obj) =>
            {
                await _server.SendMessageToServerAsync(Message);
                Message = string.Empty;
            },
            (obj) => !string.IsNullOrEmpty(Message)
        );
    }
    public static class AppState
    {
        public static ClientViewModel SharedClientViewModel { get; } = new ClientViewModel();
    }

    public void StartConnect()
    {
        _server.ConnectToServer(Username);
    }

    public void StartDisconnect()
    {
        _server.DisconnectFromServer();
    }

    private async Task ToggleConnectionAsync()
    {
        _server.ConnectToServer(Username);
    }

    private async Task UserConnected()
    {
        var user = new UserModel
        {
            Username = await _server.PacketReader.ReadMessageAsync(),
            UID = await _server.PacketReader.ReadMessageAsync(),
        };

        if (!Users.Any(x => x.UID == user.UID))
        {
            Application.Current.Dispatcher.Invoke(() => Users.Add(user));
        }
    }

    private async Task MessageReceived()
    {
        var fullMessage = await _server.PacketReader.ReadMessageAsync();
        var lines = fullMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        var title = lines.Length > 0 ? lines[0] : string.Empty;
        var text = lines.Length > 1 ? string.Join(Environment.NewLine, lines.Skip(1)) : string.Empty;

        var message = new ChatBlockModel
        {
            IsDisconnected = title.Contains("se isključio!"),
            Title = title,
            ContainsText = !string.IsNullOrWhiteSpace(text),
            Text = text.TrimEnd(),
            
            ContainsFile = false
        };

        Application.Current.Dispatcher.Invoke(() => Messages.Add(message));
    }
    private async Task PickFile()
    {
        var dlg = new OpenFileDialog();
        bool? result = dlg.ShowDialog();

        if (result == true)
        {
            string filePath = dlg.FileName;
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            string fileName = Path.GetFileName(filePath);

            await _server.SendFileToServerAsync(fileName, fileBytes);
        }
    }

    private async Task FileReceived()
    {
        var fileInfo = await _server.PacketReader.ReadMessageAsync();
        var lines = fileInfo.Split('\n');
        string username = lines.Length > 0 ? lines[0] : "Nepoznat";
        string fileName = lines.Length > 1 ? lines[1] : "file.nepoznato";

        var message = new ChatBlockModel
        {
            Title = $"📄 {username} je poslao datoteku:",
            ContainsText = false,
            Text = "",
            ContainsFile = true,
            FileName = fileName,
            DownloadFile = new RelayCommand(async obj =>
            {
                await _server.RequestFileAsync(fileName);
            })
        };

        Application.Current.Dispatcher.Invoke(() => Messages.Add(message));
    }

    private async Task FileDownload()
    {
        var (fileName, fileBytes) = await _server.PacketReader.ReadFileAsync();

        string downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads"
        );

        if (!Directory.Exists(downloadsPath))
            Directory.CreateDirectory(downloadsPath);

        string filePath = Path.Combine(downloadsPath, fileName);
        await File.WriteAllBytesAsync(filePath, fileBytes);

        var message = new ChatBlockModel
        {
            Title = "✅ Datoteka je uspješno preuzeta.",
            ContainsText = false,
            Text = ""
        };

        Application.Current.Dispatcher.Invoke(() =>
        {
            Messages.Add(message);
        });
    }

    private async Task RemoveUser()
    {
        var uid = await _server.PacketReader.ReadMessageAsync();
        var user = Users.Where(x => x.UID == uid).FirstOrDefault();
        if (user != null)
        {
            Application.Current.Dispatcher.Invoke(() => Users.Remove(user));
        }
    }

    private void HandleDisconnect()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Users.Clear();
            StatusMessage = "Isključen si.";
        });
    }

    private async void CheckServerAvailability()
    {
        while (StatusMessage == "Moguće je povezati se." || StatusMessage == "Potrebno je pokrenuti poslužitelja.")
        {
            if (StatusMessage == "Moguće je povezati se." && !NetworkUtils.IsServerActive(7750)) 
                StatusMessage = "Potrebno je pokrenuti poslužitelja.";
            else if (StatusMessage == "Potrebno je pokrenuti poslužitelja." && NetworkUtils.IsServerActive(7750)) 
                StatusMessage = "Moguće je povezati se.";

            await Task.Delay(500);
        }

        _isCheckingServer = false;
    }

    public class ChatBlockModel
    {
        public bool IsDisconnected { get; set; }
        public string Title { get; set; }
        public bool ContainsText { get; set; }
        public string Text { get; set; }
        
        public bool ContainsFile { get; set; }
        public string FileName { get; set; }
        public ICommand DownloadFile { get; set; }
    }
}