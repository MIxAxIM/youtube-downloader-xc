using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using DialogHostAvalonia;
using YoutubeDownloader.Utils.Extensions;
using YoutubeDownloader.ViewModels.Dialogs;
using YoutubeDownloader.Views.Dialogs;

namespace YoutubeDownloader.Framework;

public class DialogManager : IDisposable
{
    private readonly SemaphoreSlim _dialogLock = new(1, 1);
    private SettingsWindow? _settingsWindow;

    public async Task<T?> ShowDialogAsync<T>(DialogViewModelBase<T> dialog)
    {
        await _dialogLock.WaitAsync();
        try
        {
            await DialogHost.Show(
                dialog,
                // It's fine to await in a void method here because it's an event handler
                // ReSharper disable once AsyncVoidLambda
                async (object _, DialogOpenedEventArgs args) =>
                {
                    await dialog.WaitForCloseAsync();

                    try
                    {
                        args.Session.Close();
                    }
                    catch (InvalidOperationException)
                    {
                        // Dialog host is already processing a close operation
                    }
                }
            );

            // Yield to allow DialogHost to fully reset its state before
            // another dialog is shown (e.g., when dialogs are shown sequentially)
            await Task.Yield();

            return dialog.DialogResult;
        }
        finally
        {
            _dialogLock.Release();
        }
    }

    public async Task<bool?> ShowSettingsWindowAsync(SettingsViewModel viewModel)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return _settingsWindow.DataContext.DialogResult;
        }

        var owner =
            (
                Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime
            )?.MainWindow
            ?? throw new ApplicationException("Could not find the main window.");

        var window = new SettingsWindow { DataContext = viewModel };
        _settingsWindow = window;

        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_settingsWindow, window))
                _settingsWindow = null;
        };

        // Close the window when OK / Cancel finish the dialog view-model.
        _ = viewModel
            .WaitForCloseAsync()
            .ContinueWith(
                _ =>
                {
                    if (window.IsVisible)
                        Avalonia.Threading.Dispatcher.UIThread.Post(window.Close);
                },
                TaskScheduler.Default
            );

        await window.ShowDialog(owner);
        return viewModel.DialogResult;
    }

    public async Task<string?> PromptOpenFilePathAsync(
        IReadOnlyList<FilePickerFileType>? fileTypes = null
    )
    {
        var topLevel =
            Application.Current?.ApplicationLifetime?.TryGetTopLevel()
            ?? throw new ApplicationException("Could not find the top-level visual element.");

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { FileTypeFilter = fileTypes, AllowMultiple = false }
        );

        var file = result.FirstOrDefault();
        return file?.TryGetLocalPath() ?? file?.Path.ToString();
    }

    public async Task<string?> PromptSaveFilePathAsync(
        IReadOnlyList<FilePickerFileType>? fileTypes = null,
        string defaultFilePath = ""
    )
    {
        var topLevel =
            Application.Current?.ApplicationLifetime?.TryGetTopLevel()
            ?? throw new ApplicationException("Could not find the top-level visual element.");

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                FileTypeChoices = fileTypes,
                SuggestedFileName = defaultFilePath,
                DefaultExtension = Path.GetExtension(defaultFilePath).TrimStart('.'),
            }
        );

        return file?.TryGetLocalPath() ?? file?.Path.ToString();
    }

    public async Task<string?> PromptDirectoryPathAsync(string defaultDirPath = "")
    {
        var topLevel =
            Application.Current?.ApplicationLifetime?.TryGetTopLevel()
            ?? throw new ApplicationException("Could not find the top-level visual element.");

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(
                    defaultDirPath
                ),
            }
        );

        var directory = result.FirstOrDefault();
        if (directory is null)
            return null;

        return directory.TryGetLocalPath() ?? directory.Path.ToString();
    }

    public void Dispose() => _dialogLock.Dispose();
}
