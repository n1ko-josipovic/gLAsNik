using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Hub.Helpers;

namespace Hub;

public partial class About : Page
{
    public About()
    {
        InitializeComponent();
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

}
