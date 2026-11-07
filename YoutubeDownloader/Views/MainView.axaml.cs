using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Material.Icons;
using Material.Icons.Avalonia;
using YoutubeDownloader.Framework;
using YoutubeDownloader.ViewModels;
using YoutubeDownloader.ViewModels.Components;

namespace YoutubeDownloader.Views;

public partial class MainView : Window<MainViewModel>
{
    private bool _forceClose;

    public MainView()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
        PropertyChanged += OnWindowPropertyChanged;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        var settings = DataContext.Settings;
        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
        }

        if (settings.WindowX != 0 || settings.WindowY != 0)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint((int)settings.WindowX, (int)settings.WindowY);
        }

        UpdateMaximizeIcon();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            UpdateMaximizeIcon();

            if (WindowState == WindowState.Minimized && DataContext.Settings.MinimizeToTray)
                Hide();
        }
    }

    private void MinimizeButton_OnClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeButton_OnClick(object? sender, RoutedEventArgs e) =>
        WindowState =
            WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();

    private void UpdateMaximizeIcon()
    {
        if (MaximizeIcon is null)
            return;

        MaximizeIcon.Kind =
            WindowState == WindowState.Maximized
                ? MaterialIconKind.WindowRestore
                : MaterialIconKind.WindowMaximize;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        var settings = DataContext.Settings;
        settings.WindowWidth = Width;
        settings.WindowHeight = Height;
        settings.WindowX = Position.X;
        settings.WindowY = Position.Y;
        DataContext.Dashboard.PersistQueueForShutdown();

        if (_forceClose)
            return;

        var downloading = DataContext.Dashboard.Downloads.Any(card =>
            card.Status is DownloadStatus.Started or DownloadStatus.Enqueued
        );
        if (!downloading)
            return;

        e.Cancel = true;
        var confirmed = await DataContext.ConfirmQuitWhileDownloadingAsync();
        if (!confirmed)
            return;

        DataContext.Dashboard.CancelAllDownloadsCommand.Execute(null);
        _forceClose = true;
        Close();
    }
}
