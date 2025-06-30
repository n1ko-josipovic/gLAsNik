using System.Windows.Input;
using System.Windows;

namespace Hub.Helpers
{
    public static class WindowControls
    {
        public static void ExitApplication(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public static void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            Window window = GetWindowFromSender(sender);
            if (window != null)
                window.WindowState = WindowState.Minimized;
        }

        public static void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Window window = GetWindowFromSender(sender);
                if (window != null)
                    window.DragMove();
            }
        }

        private static Window GetWindowFromSender(object sender)
        {
            if (sender is FrameworkElement fe)
                return Window.GetWindow(fe);
            return null;
        }
    }
}