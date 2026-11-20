using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using TiebaDesktop.Services;
using WinForms = System.Windows.Forms;

namespace TiebaDesktop;

public partial class MainWindow : Window
{
    private const string HomeUrl = "https://tieba.baidu.com/";
    private const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private readonly AppSettings _settings;

    private CoreWebView2Environment? _environment;
    private AdBlocker? _adBlocker;
    private WinForms.NotifyIcon? _tray;
    private WinForms.ToolStripMenuItem? _trayTopmostItem;

    private bool _exiting;
    private bool _trayTipShown;
    private bool _webViewReady;
    private bool _suppressToggleEvents;

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        _suppressToggleEvents = true;
        RestoreWindowState();
        Topmost = _settings.AlwaysOnTop;
        TopmostToggle.IsChecked = _settings.AlwaysOnTop;
        AdBlockToggle.IsChecked = _settings.BlockAds;
        _suppressToggleEvents = false;

        BackButton.IsEnabled = false;
        ForwardButton.IsEnabled = false;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        CreateTray();
    }

    // ───────────────────────── 初始化 ─────────────────────────

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_webViewReady) return;
        _webViewReady = true;

        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TiebaDesktop",
                "UserData");
            Directory.CreateDirectory(userDataFolder);

            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments =
                    "--autoplay-policy=no-user-gesture-required " +
                    "--disable-blink-features=AutomationControlled " +
                    "--disable-features=msWebOOUI,msPdfOOUI"
            };

            _environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            await WebView.EnsureCoreWebView2Async(_environment);

            var core = WebView.CoreWebView2;

            core.Settings.UserAgent = ChromeUserAgent;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = true;
            core.Settings.IsGeneralAutofillEnabled = true;

            core.NewWindowRequested += Core_NewWindowRequested;
            core.DownloadStarting += Core_DownloadStarting;
            core.PermissionRequested += Core_PermissionRequested;
            core.HistoryChanged += (_, _) => UpdateNavigationButtons();
            core.NavigationCompleted += Core_NavigationCompleted;

            _adBlocker = new AdBlocker(core, _environment, _settings);
            await InstallExtraCssAsync(core);

            core.Navigate(HomeUrl);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                this,
                "初始化浏览器内核失败：\n" + ex.Message +
                "\n\n请确认系统已安装 Microsoft Edge WebView2 Runtime。",
                "百度贴吧",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task InstallExtraCssAsync(CoreWebView2 core)
    {
        if (string.IsNullOrWhiteSpace(_settings.ExtraCss)) return;

        var cssLiteral = JsonSerializer.Serialize(_settings.ExtraCss);
        var script =
            "(function(){var css=" + cssLiteral + ";" +
            "function apply(){if(document.getElementById('tieba-extra-css'))return;" +
            "var el=document.createElement('style');el.id='tieba-extra-css';el.textContent=css;" +
            "(document.head||document.documentElement).appendChild(el);}" +
            "if(document.readyState==='loading'){document.addEventListener('DOMContentLoaded',apply);}else{apply();}})();";

        await core.AddScriptToExecuteOnDocumentCreatedAsync(script);
    }

    // ───────────────────────── WebView2 事件 ─────────────────────────

    private void Core_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;

        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
            return;

        if (uri.Host.EndsWith("baidu.com", StringComparison.OrdinalIgnoreCase))
        {
            WebView.CoreWebView2?.Navigate(e.Uri);
            return;
        }

        if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch
            {
                // 系统浏览器无法启动时忽略。
            }
        }
    }

    private void Core_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        var suggestedName = string.IsNullOrWhiteSpace(e.ResultFilePath)
            ? "download"
            : Path.GetFileName(e.ResultFilePath);

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "保存文件",
            FileName = suggestedName,
            Filter = "所有文件|*.*",
            InitialDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        };

        if (dialog.ShowDialog(this) == true)
            e.ResultFilePath = dialog.FileName;
        else
            e.Cancel = true;
    }

    private static void Core_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        // 复制帖子链接等需要剪贴板读取；通知/定位等一律拒绝。
        e.State = e.PermissionKind == CoreWebView2PermissionKind.ClipboardRead
            ? CoreWebView2PermissionState.Allow
            : CoreWebView2PermissionState.Deny;
    }

    private void Core_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        var core = WebView.CoreWebView2;
        if (core is null) return;

        BackButton.IsEnabled = core.CanGoBack;
        ForwardButton.IsEnabled = core.CanGoForward;
    }

    // ───────────────────────── 工具栏 ─────────────────────────

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2?.CanGoBack == true)
            WebView.CoreWebView2.GoBack();
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2?.CanGoForward == true)
            WebView.CoreWebView2.GoForward();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.CoreWebView2?.Reload();
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.CoreWebView2?.Navigate(HomeUrl);
    }

    private void TopmostToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressToggleEvents) return;
        ApplyTopmost(TopmostToggle.IsChecked == true);
    }

    private void AdBlockToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressToggleEvents) return;
        _settings.BlockAds = AdBlockToggle.IsChecked == true;
        _settings.Save();
    }

    private void TrayButton_Click(object sender, RoutedEventArgs e)
    {
        HideToTray();
    }

    // ───────────────────────── 自定义标题栏 ─────────────────────────

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        // WindowChrome 最大化时窗口会外扩一个不可见边框，用外边距抵消避免内容被裁切。
        if (WindowState == WindowState.Maximized)
        {
            var border = SystemParameters.WindowResizeBorderThickness;
            RootBorder.Margin = new Thickness(border.Left + 4, border.Top + 4, border.Right + 4, border.Bottom + 4);
            MaximizeButton.Content = "\uE923";
        }
        else
        {
            RootBorder.Margin = new Thickness(0);
            MaximizeButton.Content = "\uE922";
        }
    }

    // ───────────────────────── 置顶 / 托盘 ─────────────────────────

    private void ApplyTopmost(bool value)
    {
        _settings.AlwaysOnTop = value;
        Topmost = value;

        _suppressToggleEvents = true;
        TopmostToggle.IsChecked = value;
        if (_trayTopmostItem != null && _trayTopmostItem.Checked != value)
            _trayTopmostItem.Checked = value;
        _suppressToggleEvents = false;

        _settings.Save();
    }

    private void CreateTray()
    {
        _trayTopmostItem = new WinForms.ToolStripMenuItem("窗口置顶")
        {
            CheckOnClick = true,
            Checked = _settings.AlwaysOnTop
        };
        _trayTopmostItem.CheckedChanged += (_, _) =>
        {
            if (_suppressToggleEvents) return;
            ApplyTopmost(_trayTopmostItem.Checked);
        };

        var showItem = new WinForms.ToolStripMenuItem("显示主窗口");
        showItem.Click += (_, _) => ShowFromTray();

        var exitItem = new WinForms.ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitApplication();

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add(showItem);
        menu.Items.Add(_trayTopmostItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _tray = new WinForms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "百度贴吧",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowFromTray();
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var resource = System.Windows.Application.GetResourceStream(
                new Uri("Assets/app.ico", UriKind.Relative));
            if (resource?.Stream != null)
            {
                using var stream = resource.Stream;
                return (System.Drawing.Icon)new System.Drawing.Icon(stream).Clone();
            }
        }
        catch
        {
            // 资源不可用时回退系统图标。
        }

        return System.Drawing.SystemIcons.Application;
    }

    public void ShowFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
    }

    private void HideToTray()
    {
        Hide();

        if (_trayTipShown || _tray is null) return;
        _trayTipShown = true;
        try
        {
            _tray.ShowBalloonTip(2500, "百度贴吧", "已最小化到托盘，右键托盘图标可显示或退出。", WinForms.ToolTipIcon.Info);
        }
        catch
        {
            // 气泡提示被系统禁用时忽略。
        }
    }

    private void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;
        SaveWindowState();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    // ───────────────────────── 窗口状态 ─────────────────────────

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        SaveWindowState();

        if (!_exiting)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }
    }

    private void RestoreWindowState()
    {
        var geometry = _settings.Window;

        Width = geometry.Width >= MinWidth ? geometry.Width : 1200;
        Height = geometry.Height >= MinHeight ? geometry.Height : 800;

        if (IsVisibleOnScreen(geometry.Left, geometry.Top))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = geometry.Left;
            Top = geometry.Top;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (geometry.Maximized)
            WindowState = WindowState.Maximized;
    }

    private static bool IsVisibleOnScreen(double left, double top)
    {
        const double margin = 80;
        var x = left + margin;
        var y = top + margin;

        return x >= SystemParameters.VirtualScreenLeft &&
               x < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth &&
               y >= SystemParameters.VirtualScreenTop &&
               y < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
    }

    private void SaveWindowState()
    {
        var geometry = _settings.Window;
        geometry.Maximized = WindowState == WindowState.Maximized;

        if (WindowState == WindowState.Normal)
        {
            geometry.Left = Left;
            geometry.Top = Top;
            geometry.Width = Width;
            geometry.Height = Height;
        }

        _settings.Save();
    }
}
