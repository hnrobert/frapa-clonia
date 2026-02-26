using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class DeploymentView : UserControl
{
    public DeploymentView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DeploymentViewModel viewModel)
        {
            viewModel.Initialize();
        }
    }

    private void DockerContainerNameTextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DeploymentViewModel viewModel)
        {
            viewModel.ValidateDockerContainerNameCommand.Execute(null);
        }
    }

    private void DockerImageNameTextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DeploymentViewModel viewModel)
        {
            viewModel.ValidateDockerImageCommand.Execute(null);
        }
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void DockerComposeBrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DeploymentViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null)
        {
            return;
        }

        var fileTypes = new[]
        {
            new FilePickerFileType("Docker Compose")
            {
                Patterns = ["docker-compose.yml", "docker-compose.yaml", "*.yml", "*.yaml"]
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        });

        var localPath = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            viewModel.DockerComposePath = localPath;
        }
    }
}
