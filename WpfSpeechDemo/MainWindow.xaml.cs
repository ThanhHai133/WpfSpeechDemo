using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace WpfSpeechDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WebView.NavigationCompleted += WebView_NavigationCompleted;

            var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "speech.html");
            WebView.Source = new Uri(htmlPath);
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

            WebView.CoreWebView2.PermissionRequested += (s, args) =>
            {
                if (args.PermissionKind == CoreWebView2PermissionKind.Microphone)
                    args.State = CoreWebView2PermissionState.Allow;
            };

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var jsonText = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(jsonText);
            var safeJson = JsonSerializer.Serialize(doc.RootElement); // ✅ escape an toàn

            var injectScript = $"window.ENV = {safeJson};";
            await WebView.CoreWebView2.ExecuteScriptAsync(injectScript);

            WebView.CoreWebView2.OpenDevToolsWindow();
        }

    }
}
