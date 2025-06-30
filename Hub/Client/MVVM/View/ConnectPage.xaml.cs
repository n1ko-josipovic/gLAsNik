using Hub.Helpers;
using Hub.Client.MVVM.ViewModel;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Hub.Client.Net;
using static Hub.Client.MVVM.ViewModel.ClientViewModel;

namespace Hub;

/// <summary>
/// Interaction logic for Client.xaml
/// </summary>

public partial class ConnectPage : Page
{
    public ConnectPage()
    {
        InitializeComponent();
        this.DataContext = AppState.SharedClientViewModel;

        UsernameInput.Focus();
        Keyboard.Focus(UsernameInput);
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

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Application.Current.MainWindow as MainWindow;

        if (mainWindow != null)
        {
            mainWindow.RestoreContent();
        }
    }
    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = this.DataContext as ClientViewModel;
        if (vm != null)
        {
            vm.StartConnect();
        }

        Frame clientFrame = new Frame();
        clientFrame.NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;
        clientFrame.Navigate(new ClientPage());
        this.Content = clientFrame;
    }
}
