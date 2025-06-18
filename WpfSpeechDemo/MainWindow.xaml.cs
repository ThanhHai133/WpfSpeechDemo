using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;

namespace WpfSpeechDemo
{
    public partial class MainWindow : Window
    {
        private bool isFullscreen = true;
        private bool isTopmost = false;
        public MainWindow()
        {
            InitializeComponent();

            WebView.Loaded += (_, _) =>
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var hwndSource = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
                hwndSource.CompositionTarget.BackgroundColor = System.Windows.Media.Colors.Transparent;
            };

            WebView.NavigationCompleted += WebView_NavigationCompleted;
            WebView.CoreWebView2InitializationCompleted += (_, _) =>
            {
                WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            };

            this.Topmost = true;
            var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "speech.html");
            var htmlUri = new Uri("file:///" + htmlPath.Replace("\\", "/"));
            WebView.Source = htmlUri;

        }
        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var json = e.WebMessageAsJson;
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (data is { } && data.TryGetValue("action", out var action))
            {
                switch (action)
                {
                    case "toggleFullscreen":
                        ToggleFullscreen();
                        break;
                    case "toggleTopmost":
                        ToggleTopmost();
                        break;
                    case "exitApp":
                        if (!Application.Current.Dispatcher.CheckAccess())
                        {
                            Application.Current.Dispatcher.Invoke(() => ExitAppConfirmed());
                        }
                        else
                        {
                            ExitAppConfirmed();
                        }
                        break;

                    case "log":
                        if (data.TryGetValue("data", out var logContent))
                        {
                            try
                            {
                                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.txt");
                                var logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {logContent}{Environment.NewLine}";
                                File.AppendAllText(logPath, logLine);
                            }
                            catch (Exception ex)
                            {
                                // Ghi lỗi log luôn nếu cần
                                File.AppendAllText("logs.txt", $"[ERROR] {ex.Message}{Environment.NewLine}");
                            }
                        }
                        break;


                }
            }
        }
        private void ExitAppConfirmed()
        {
            var result = MessageBox.Show("Bạn có chắc muốn thoát?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
        //private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        //{
        //    await WebView.EnsureCoreWebView2Async();

        //    // Cho phép mic
        //    WebView.CoreWebView2.PermissionRequested += (s, args) =>
        //    {
        //        if (args.PermissionKind == CoreWebView2PermissionKind.Microphone)
        //            args.State = CoreWebView2PermissionState.Allow;
        //    };

        //    // Đọc config và inject vào JS
        //    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        //    var json = File.ReadAllText(configPath);
        //    var injectScript = $"window.ENV = {json};";
        //    await WebView.CoreWebView2.ExecuteScriptAsync(injectScript);

        //    // Bắt đầu
        //    await WebView.CoreWebView2.ExecuteScriptAsync("window.ENV = " + json + ";");
        //    await WebView.CoreWebView2.ExecuteScriptAsync("applyConfigFromWPF();");

        //    await WebView.CoreWebView2.ExecuteScriptAsync("detectAndStart()");
        //}
        private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            await WebView.EnsureCoreWebView2Async();
            #if DEBUG
                        WebView.CoreWebView2.OpenDevToolsWindow();
#endif
            WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

                

            WebView.CoreWebView2.PermissionRequested += (s, args) =>
            {
                if (args.PermissionKind == CoreWebView2PermissionKind.Microphone)
                    args.State = CoreWebView2PermissionState.Allow;
            };

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var jsonText = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(jsonText);
            var safeJson = JsonSerializer.Serialize(doc.RootElement);

            var injectScript = $"window.ENV = {safeJson};";
            await WebView.CoreWebView2.ExecuteScriptAsync(injectScript);

            // ✅ THÊM DÒNG NÀY để JS biết khi nào ENV đã sẵn sàng
            await WebView.CoreWebView2.ExecuteScriptAsync("if (typeof startRecognizer === 'function') startRecognizer();");
        }


        private void ToggleFullscreen()
        {
            if (isFullscreen)
            {
                // ✅ Không được trả về SingleBorderWindow nếu AllowsTransparency = true
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.CanResize;
                this.WindowState = WindowState.Normal;
                this.Topmost = false;
            }
            else
            {
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                this.Topmost = true;
            }

            isFullscreen = !isFullscreen;
        }

        private async void ToggleTopmost()
        {
            isTopmost = !isTopmost;
            this.Topmost = isTopmost;

            // ✅ Gửi trạng thái ngược lại cho JS để đổi màu
            string js = $"document.querySelector('[onclick=\"toggleTopmost()\"].topmost-toggle')?.classList.{(isTopmost ? "add" : "remove")}('active')";
            await WebView.ExecuteScriptAsync(js);
        }
        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }


    }
}
