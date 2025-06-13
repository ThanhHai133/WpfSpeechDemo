using System;
using System.Configuration;
using System.Data;
using System.Windows;

namespace WpfSpeechDemo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);
            base.OnStartup(e);
        }
    }
}
