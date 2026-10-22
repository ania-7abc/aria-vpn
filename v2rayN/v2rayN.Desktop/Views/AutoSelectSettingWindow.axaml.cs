using v2rayN.Desktop.Base;

namespace v2rayN.Desktop.Views;

public partial class AutoSelectSettingWindow : WindowBase<AutoSelectSettingViewModel>
{
    public AutoSelectSettingWindow()
    {
        InitializeComponent();

        btnCancel.Click += (s, e) => Close();

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.Enabled, v => v.togEnabled.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.OnlySwitchIfBetter, v => v.togOnlySwitchIfBetter.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AllSubscriptions, v => v.togAllSubscriptions.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectOnStartup, v => v.togSelectOnStartup.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.MaxDelay, v => v.numMaxDelay.Value).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SwitchTolerance, v => v.numSwitchTolerance.Value).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedInterval, v => v.cmbInterval.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedStrategy, v => v.cmbStrategy.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedTestType, v => v.cmbTestType.SelectedItem).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SaveCmd, v => v.btnSave).DisposeWith(disposables);
        });
    }
}
