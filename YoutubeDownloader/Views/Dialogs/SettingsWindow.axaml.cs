using Avalonia.Interactivity;
using YoutubeDownloader.Framework;
using YoutubeDownloader.ViewModels.Dialogs;

namespace YoutubeDownloader.Views.Dialogs;

public partial class SettingsWindow : Window<SettingsViewModel>
{
    public SettingsWindow() => InitializeComponent();

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}
