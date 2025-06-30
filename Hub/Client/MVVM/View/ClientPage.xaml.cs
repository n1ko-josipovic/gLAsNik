using Hub.Helpers;
using Hub.Client.MVVM.ViewModel;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Hub.Client.Net;
using static Hub.Client.MVVM.ViewModel.ClientViewModel;
using Hub.Client.Net.IO;

namespace Hub;

/// <summary>
/// Interaction logic for Client.xaml
/// </summary>

public partial class ClientPage : Page
{
    public ClientPage()
    {
        InitializeComponent();
        this.DataContext = AppState.SharedClientViewModel;

        MessageInput.Focus();
        Keyboard.Focus(MessageInput);
    }

    private void PageKeyDown(object sender, KeyEventArgs e)
    {
        MessageInput.Focus();
        Keyboard.Focus(MessageInput);
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
        var vm = this.DataContext as ClientViewModel;
        if (vm != null)
        {
            vm.StartDisconnect();
        }

        NavigationService?.Navigate(new ConnectPage());
    }

    private void CopyTextMessage(object sender, MouseButtonEventArgs e)
    {
        if (sender is StackPanel panel && panel.DataContext is ChatBlockModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.Text))
            {
                Clipboard.SetText(model.Text);
            }
        }
    }

    private void EnterDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            e.Handled = true;
            var vm = DataContext as ClientViewModel;
            if (vm?.SendMessageCommand?.CanExecute(null) == true)
            {
                vm.SendMessageCommand.Execute(null);
            }
        }
    }
}
