using Avalonia.Interactivity;
using YoutubeDownloader.XC.Framework;
using YoutubeDownloader.XC.ViewModels.Dialogs;

namespace YoutubeDownloader.XC.Views.Dialogs;

public partial class SettingsWindow : Window<SettingsViewModel>
{
    public SettingsWindow() => InitializeComponent();

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}
