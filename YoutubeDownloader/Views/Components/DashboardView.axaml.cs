using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PowerKit.Extensions;
using YoutubeDownloader.Framework;
using YoutubeDownloader.ViewModels.Components;

namespace YoutubeDownloader.Views.Components;

public partial class DashboardView : UserControl<DashboardViewModel>
{
    public DashboardView()
    {
        InitializeComponent();

        QueryTextBox.AddHandler(KeyDownEvent, QueryTextBox_OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, UserControl_OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(DragDrop.DropEvent, UserControl_OnDrop);
        DragDrop.SetAllowDrop(this, true);
    }

    private void UserControl_OnLoaded(object? sender, RoutedEventArgs args) => QueryTextBox.Focus();

    private void QueryTextBox_OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Enter && args.KeyModifiers != KeyModifiers.Shift)
        {
            args.Handled = true;
            ProcessQueryButton.Command?.ExecuteIfCan(ProcessQueryButton.CommandParameter);
        }
    }

    private void UserControl_OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Source is TextBox)
            return;

        if (args.Key == Key.Delete)
        {
            foreach (var card in DataContext.Downloads.Where(card => card.IsSelected).ToArray())
                DataContext.CloseCardCommand.Execute(card);
            args.Handled = true;
        }
        else if (args.Key == Key.Enter)
        {
            foreach (var card in DataContext.Downloads.Where(card => card.IsSelected).ToArray())
                DataContext.StartDownloadCommand.Execute(card);
            args.Handled = true;
        }
    }

    private async void UserControl_OnDrop(object? sender, DragEventArgs args)
    {
        var text = args.DataTransfer.TryGetText();
        if (string.IsNullOrWhiteSpace(text))
            return;

        DataContext.Query = text.Trim();
        await DataContext.ProcessQueryCommand.ExecuteAsync(null);
    }

    private void StatusTextBlock_OnPointerReleased(object sender, PointerReleasedEventArgs args)
    {
        if (sender is IDataContextProvider { DataContext: DownloadViewModel dataContext })
            dataContext.CopyErrorMessageCommand.ExecuteIfCan(null);
    }

    private void Card_OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (sender is not Border { DataContext: DownloadViewModel card })
            return;

        var ctrl = args.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = args.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (!ctrl && !shift)
        {
            foreach (var item in DataContext.Downloads)
                item.IsSelected = false;
            card.IsSelected = true;
            return;
        }

        if (ctrl)
            card.IsSelected = !card.IsSelected;
        else
            card.IsSelected = true;
    }

    private void Card_OnDoubleTapped(object? sender, TappedEventArgs args)
    {
        if (
            sender is Border
            {
                DataContext: DownloadViewModel { Status: DownloadStatus.Completed } card
            }
        )
            card.OpenFileCommand.ExecuteIfCan(null);
    }
}
