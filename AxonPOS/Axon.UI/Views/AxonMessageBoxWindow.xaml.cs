using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace Axon.UI.Views
{
    public partial class AxonMessageBoxWindow : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public AxonMessageBoxWindow(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();

            var main = System.Windows.Application.Current?.MainWindow;
            if (main != null && main != this)
            {
                this.Owner = main;
            }

            TxtTitle.Text = string.IsNullOrWhiteSpace(caption) ? "تنبيه النظام" : caption;
            TxtMessage.Text = message;

            ConfigureIcon(icon);
            ConfigureButtons(button);
        }

        private void ConfigureIcon(MessageBoxImage icon)
        {
            switch (icon)
            {
                case MessageBoxImage.Information:
                    HeaderIcon.Kind = PackIconKind.CheckCircle;
                    HeaderIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4AE176"));
                    BodyIcon.Kind = PackIconKind.CheckCircle;
                    BodyIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4AE176"));
                    IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#224AE176"));
                    break;
                case MessageBoxImage.Warning:
                    HeaderIcon.Kind = PackIconKind.AlertCircle;
                    HeaderIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                    BodyIcon.Kind = PackIconKind.AlertCircle;
                    BodyIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                    IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22FBBF24"));
                    break;
                case MessageBoxImage.Error:
                    HeaderIcon.Kind = PackIconKind.AlertOctagon;
                    HeaderIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    BodyIcon.Kind = PackIconKind.AlertOctagon;
                    BodyIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22EF4444"));
                    break;
                case MessageBoxImage.Question:
                    HeaderIcon.Kind = PackIconKind.HelpCircle;
                    HeaderIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
                    BodyIcon.Kind = PackIconKind.HelpCircle;
                    BodyIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
                    IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2260A5FA"));
                    break;
                default:
                    HeaderIcon.Kind = PackIconKind.Information;
                    HeaderIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
                    BodyIcon.Kind = PackIconKind.Information;
                    BodyIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
                    IconBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2260A5FA"));
                    break;
            }
        }

        private void ConfigureButtons(MessageBoxButton button)
        {
            BtnOk.Visibility = Visibility.Collapsed;
            BtnYes.Visibility = Visibility.Collapsed;
            BtnNo.Visibility = Visibility.Collapsed;
            BtnCancel.Visibility = Visibility.Collapsed;

            switch (button)
            {
                case MessageBoxButton.OK:
                    BtnOk.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.OKCancel:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNo:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNoCancel:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            DialogResult = true;
            Close();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            DialogResult = true;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = false;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }
    }

    public static class AxonMessageBox
    {
        public static MessageBoxResult Show(string messageBoxText)
        {
            return Show(messageBoxText, "تنبيه النظام", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return Show(messageBoxText, caption, button, MessageBoxImage.Information);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var win = new AxonMessageBoxWindow(messageBoxText, caption, button, icon);
            win.ShowDialog();
            return win.Result;
        }
    }
}
