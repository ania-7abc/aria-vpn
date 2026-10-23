using CliWrap.Buffered;
using DialogHostAvalonia;

namespace v2rayN.Desktop.Views;

public partial class SudoPasswordInputView : UserControl
{
    public SudoPasswordInputView()
    {
        InitializeComponent();

        Loaded += (s, e) => txtPassword.Focus();

        txtPassword.TextChanged += (_, _) => HideError();

        btnSave.Click += async (_, _) => await SavePasswordAsync();

        btnCancel.Click += (_, _) =>
        {
            DialogHost.Close(null);
        };
    }

    private void ShowError(string message)
    {
        txtError.Text = message;
        txtError.IsVisible = true;
    }

    private void HideError()
    {
        txtError.IsVisible = false;
    }

    private async Task SavePasswordAsync()
    {
        if (txtPassword.Text.IsNullOrEmpty())
        {
            ShowError(ResUI.SudoPasswordRequired);
            txtPassword.Focus();
            return;
        }

        var password = txtPassword.Text;
        HideError();
        btnSave.IsEnabled = false;

        try
        {
            // Verify if the password is correct
            if (await CheckSudoPasswordAsync(password))
            {
                // Password verification successful, return password and close dialog
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    DialogHost.Close(null, password);
                });
            }
            else
            {
                // Password verification failed, display inline error and let user try again
                ShowError(ResUI.SudoIncorrectPasswordTip);
                txtPassword.SelectAll();
                txtPassword.Focus();
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog("SudoPassword", ex);
        }
        finally
        {
            btnSave.IsEnabled = true;
        }
    }

    private async Task<bool> CheckSudoPasswordAsync(string password)
    {
        try
        {
            // Use sudo echo command to verify password
            var arg = new List<string>() { "-c", "sudo -S echo SUDO_CHECK" };
            var result = await CliWrap.Cli.Wrap(Global.LinuxBash)
                .WithArguments(arg)
                .WithStandardInputPipe(CliWrap.PipeSource.FromString(password))
                .ExecuteBufferedAsync();

            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Logging.SaveLog("CheckSudoPassword", ex);
            return false;
        }
    }
}
