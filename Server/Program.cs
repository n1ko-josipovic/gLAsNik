using System.Net.Sockets;
using System.Text;

namespace Server;
public partial class Program
{
    static List<Client>? _users;
    static TcpListener? _tcpListener;
    static UdpClient? _udpListener;

    static bool _isRunning = true;
    static bool _isMenuVisible = false;

    static List<string> _chatHistory = new List<string>();

    static int _consoleHeight = Console.WindowHeight;
    static readonly object _consoleLock = new object();

    private static bool _isStopping = false;

    static void Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        Console.Title = "gLAsNik - poslužitelj";
        _users = new List<Client>();

        Client.OnClientConnected = client =>
        {
            _users?.Add(client);
            BroadcastMessage($"{client.Username} se povezao!");
            BroadcastConnection();
        };

        Task.Run(() => StartUdpListener());
        Task.Run(() => StartTcpListener());

        Console.CancelKeyPress += (sender, e) =>
        {
            StopServer();
            e.Cancel = true;
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            StopServer();
        };

        RunAdminConsole();
    }

    public static void WriteToConsole(string message)
    {
        _chatHistory.Add(message + "\n");

        if (!_isMenuVisible)
        {
            lock (_consoleLock)
            {
                Console.Clear();
                int maxHistoryLines = Console.WindowHeight - 3;

                var recentMessages = _chatHistory.TakeLast(maxHistoryLines).ToList();

                int line = 0;
                Console.SetCursorPosition(0, 0);

                foreach (var chatMessage in recentMessages)
                {
                    string cleanedMessage = chatMessage.Replace("\n", " ").Replace("\r", " ");

                    string displayMessage = cleanedMessage.Length > Console.WindowWidth
                        ? cleanedMessage.Substring(0, Console.WindowWidth - 3) + "..."
                        : cleanedMessage;

                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, line);
                    Console.WriteLine(displayMessage);
                    line++;
                }

                Console.SetCursorPosition(0, Math.Max(0, Console.WindowHeight - 3));
                Console.WriteLine();
                Console.WriteLine("Server is running. Press 'M' to show menu...");
                Console.Write(">");

                Console.SetCursorPosition(1, Console.WindowHeight - 1);
            }
        }
    }


    static void RunAdminConsole()
    {
        while (_isRunning)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.M)
                {
                    ShowMenu();
                }
            }
            Thread.Sleep(50);
        }
    }

    static void ShowMenu()
    {
        _isMenuVisible = !_isMenuVisible;
        WriteToConsole("Ušli ste u izbornik.");

        Console.Clear();
        Console.WriteLine("Naredbe:\n");
        Console.WriteLine("1. Popis aktivnih klijenata");
        Console.WriteLine("2. Zaustavi rad poslužitelja");
        Console.WriteLine("3. Povratak na praćenje poruka");
        
        while(true) {

            Console.Write("\nUnesite broj naredbe: ");
            string? input = Console.ReadLine();
        
            switch (input)
            {
                case "1":
                    ListConnectedUsers();
                    break;
                case "2":
                    StopServer();
                    return;
                case "3":
                default:
                    Console.Clear();
                    _isMenuVisible = false;
                    WriteToConsole("Napustili ste izbornik.");
                    return;
            }
        }
    }

    static void ListConnectedUsers()
    {
        if (_users == null || _users.Count == 0)
        {
            Console.WriteLine("Nema aktivnih klijenata.");
            return;
        }

        Console.WriteLine("\nAktivni klijenti:");
        Console.WriteLine("----------------");
        Console.WriteLine("{0,-5} {1,-10}", "ID", "Ime");
        foreach (var user in _users)
        {
            Console.WriteLine("{0,-5} {1,-10}", user.UID, user.Username);
        }
    }

    static void StopServer()
    {
        if (_isStopping) return;
        _isStopping = true;

        Console.Clear();

        _isRunning = false;
        WriteToConsole("Zaustavljanje u tijeku...");

        BroadcastEnd();

        _tcpListener?.Stop();
        _udpListener?.Close();

        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "gLAsNik");
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        WriteToConsole("Poslužitelj je uspješno zaustavljen!");
        Environment.Exit(0);
    }
}