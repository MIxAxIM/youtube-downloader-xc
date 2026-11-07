using Avalonia.Controls;
using Avalonia.Controls.Templates;
using YoutubeDownloader.XC.Auth;
using YoutubeDownloader.XC.ViewModels;
using YoutubeDownloader.XC.ViewModels.Components;
using YoutubeDownloader.XC.ViewModels.Dialogs;
using YoutubeDownloader.XC.Views;
using YoutubeDownloader.XC.Views.Components;
using YoutubeDownloader.XC.Views.Dialogs;

namespace YoutubeDownloader.XC.Framework;

public partial class ViewManager
{
    private Control? TryCreateView(ViewModelBase viewModel) =>
        viewModel switch
        {
            MainViewModel => new MainView(),
            DashboardViewModel => new DashboardView(),
            AuthSetupViewModel => new AuthSetupView(),
            DownloadMultipleSetupViewModel => new DownloadMultipleSetupView(),
            DownloadSingleSetupViewModel => new DownloadSingleSetupView(),
            MessageBoxViewModel => new MessageBoxView(),
            HistoryViewModel => new HistoryView(),
            SettingsViewModel => new SettingsView(),
            _ => null,
        };

    public Control? TryBindView(ViewModelBase viewModel)
    {
        var view = TryCreateView(viewModel);
        if (view is null)
            return null;

        view.DataContext ??= viewModel;
        view.Loaded += async (_, _) => await viewModel.InitializeAsync();

        return view;
    }

    public UserControl<T>? TryBindUserControl<T>(T viewModel)
        where T : ViewModelBase => TryBindView(viewModel) as UserControl<T>;

    public Window<T>? TryBindWindow<T>(T viewModel)
        where T : ViewModelBase => TryBindView(viewModel) as Window<T>;
}

public partial class ViewManager : IDataTemplate
{
    bool IDataTemplate.Match(object? data) => data is ViewModelBase;

    Control? ITemplate<object?, Control?>.Build(object? data) =>
        data is ViewModelBase viewModel ? TryBindView(viewModel) : null;
}
