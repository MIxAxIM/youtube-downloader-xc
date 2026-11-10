using Avalonia.Interactivity;
using YoutubeDownloader.XC.Auth;
using YoutubeDownloader.XC.Framework;

namespace YoutubeDownloader.XC.Views.Dialogs;

public partial class AuthWindow : Window<AuthSetupViewModel>
{
    public AuthWindow() => InitializeComponent();

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}
