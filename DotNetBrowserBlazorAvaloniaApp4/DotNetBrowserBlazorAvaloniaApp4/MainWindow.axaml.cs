using Avalonia;
using Avalonia.Controls;

#pragma warning disable CS4014

namespace DotNetBrowserBlazorAvaloniaApp4 {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            BrowserView.Services = (Application.Current as App)?.Services;
            BrowserView.Initialize();
        }

        private void Window_Closed(object sender, EventArgs e) {
            BrowserView.Shutdown();
        }
    }
}