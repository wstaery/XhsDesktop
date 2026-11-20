using System;
using System.Threading;
using System.Windows;
using XhsDesktop.Services;

namespace XhsDesktop;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private EventWaitHandle? _activateEvent;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, @"Local\XhsDesktop.SingleInstance", out var createdNew);
        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\XhsDesktop.Activate");

        if (!createdNew)
        {
            // 已有实例在运行：唤醒它并退出当前进程，避免两个进程争用 WebView2 用户数据目录。
            try { _activateEvent.Set(); } catch { }
            Shutdown();
            return;
        }

        var settings = AppSettings.Load();
        var window = new MainWindow(settings);
        MainWindow = window;
        window.Show();

        var waitThread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    _activateEvent.WaitOne();
                    Dispatcher.Invoke(() => (MainWindow as MainWindow)?.ShowFromTray());
                }
                catch
                {
                    return;
                }
            }
        })
        {
            IsBackground = true,
            Name = "SingleInstanceActivation"
        };
        waitThread.Start();
    }
}
