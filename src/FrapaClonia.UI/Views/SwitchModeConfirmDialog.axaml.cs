using Avalonia.Controls;
using FrapaClonia.UI.Services;

namespace FrapaClonia.UI.Views;

public partial class SwitchModeConfirmDialog : Window
{
    public enum Result { Cancel, Continue, StopAndContinue }

    private Result _result = Result.Cancel;

    public SwitchModeConfirmDialog()
    {
        InitializeComponent();

        CancelButton.Click += (_, _) => { _result = Result.Cancel; Close(); };
        ContinueButton.Click += (_, _) => { _result = Result.Continue; Close(); };
        StopAndContinueButton.Click += (_, _) => { _result = Result.StopAndContinue; Close(); };
    }

    public static async Task<Result> ShowAsync(
        Window? owner,
        string runningMode,
        bool isRunning,
        Func<string, string> localize)
    {
        var dialog = new SwitchModeConfirmDialog();

        var modeName = runningMode == "native"
            ? localize("DeploymentMode_Native")
            : localize("DeploymentMode_Docker");

        dialog.MessageText.Text = localize(isRunning
            ? "SwitchMode_RunningMessage"
            : "SwitchMode_InstalledMessage")
            .Replace("{mode}", modeName);

        dialog.SubMessageText.Text = localize(isRunning
            ? "SwitchMode_RunningSubMessage"
            : "SwitchMode_InstalledSubMessage");

        // Hide "Stop and Continue" if not running (only installed)
        dialog.StopAndContinueButton.IsVisible = isRunning;
        // Hide "Continue" if running (risky to leave running)
        dialog.ContinueButton.IsVisible = !isRunning;

        ToastService.Instance?.PushChildWindow();
        dialog.Closed += (_, _) => ToastService.Instance?.PopChildWindow();

        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        return dialog._result;
    }
}
