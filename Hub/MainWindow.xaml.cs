using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Hub.Helpers;
using Hub.Networking;

namespace Hub;
public partial class MainWindow : Window
{
    private bool serverRunning = false;
    private UdpClient udpListener;
    private DispatcherTimer udpCheckTimer;

    public MainWindow()
    {
        InitializeComponent();
        InitializeUdpCheckTimer();
    }

    private void InitializeUdpCheckTimer()
    {
        udpCheckTimer = new DispatcherTimer();
        udpCheckTimer.Interval = TimeSpan.FromSeconds(1);
        udpCheckTimer.Tick += UdpCheckTimer_Tick;
        udpCheckTimer.Start();
    }

    private void UdpCheckTimer_Tick(object sender, EventArgs e)
    {
        if (serverRunning || NetworkUtils.IsServerActive(7750))
        {
            ServerButton.IsEnabled = false;
            StatusText.Text = "Poslužitelj je pokrenut i radi!";
        }
        else
        {
            ServerButton.IsEnabled = true;
            StatusText.Text = "Potrebno je pokrenuti poslužitelja.";
        }
    }

    private void ServerButton_Click(object sender, RoutedEventArgs e)
    {
        if (serverRunning || NetworkUtils.IsServerActive(7750))
        {
            ServerButton.IsEnabled = false;
            StatusText.Text = "Poslužitelj već postoji.";
            return;
        }

        ServerButton.IsEnabled = false;
        serverRunning = true;

        StatusText.Text = "Pokretanje poslužitelja...";

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                // FileName = @"..\..\..\..\..\Server\bin\Debug\net8.0\win-x64\Server.exe",
                FileName = @"Server.exe",
                UseShellExecute = false
            };
            Process proc = Process.Start(startInfo);
            StatusText.Text = "Poslužitelj uspješno pokrenut!";
            proc.EnableRaisingEvents = true;
            proc.Exited += (s, ev) =>
            {
                serverRunning = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "Poslužitelj je prestao s radom.";
                });
            };
        }
        catch (Exception ex)
        {
            serverRunning = false;
            ServerButton.IsEnabled = true;
            StatusText.Text = "Neuspješno pokretanje poslužitelja.";
        }
    }    
    
    private object _originalContent;

    private void ClientButton_Click(object sender, RoutedEventArgs e)
    {
        _originalContent = this.Content;

        Frame connectFrame = new Frame();
        connectFrame.NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;
        connectFrame.Navigate(new ConnectPage());
        this.Content = connectFrame;
    }


    private void AboutButton_Click(object sender, RoutedEventArgs e) 
    {
        _originalContent = this.Content;

        Frame aboutFrame = new Frame();
        aboutFrame.NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;
        aboutFrame.Navigate(new About());
        this.Content = aboutFrame;
    }

    public void RestoreContent()
    {
        this.Content = _originalContent;
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        WindowControls.ExitApplication(sender, e);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowControls.MinimizeWindow(sender, e);
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        WindowControls.DragWindow(sender, e);
    }
}
