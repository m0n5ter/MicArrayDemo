using System.Windows;

namespace TwoMicTest;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var vm = new MainViewModel();
        _ = vm.Initialize();

        new MainWindow {DataContext = vm}.Show();
    }
}