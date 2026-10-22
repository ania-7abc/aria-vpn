namespace v2rayN.Desktop.Views;

public partial class AutoSelectView : ReactiveUserControl<AutoSelectViewModel>
{
    public AutoSelectView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.Enabled, v => v.togEnabled.IsChecked).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.StatusText, v => v.txtStatus.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.CurrentServerText, v => v.txtCurrentServer.Text).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SelectBestCmd, v => v.btnSelectBest).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SettingCmd, v => v.btnSetting).DisposeWith(disposables);
        });
    }
}
