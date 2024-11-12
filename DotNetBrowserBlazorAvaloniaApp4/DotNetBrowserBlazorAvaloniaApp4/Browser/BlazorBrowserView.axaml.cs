using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DotNetBrowser.Browser;
using DotNetBrowser.Engine;
using DotNetBrowser.Handlers;
using DotNetBrowser.Input;
using DotNetBrowser.Input.Keyboard.Events;
using DotNetBrowser.Net;
using DotNetBrowser.Net.Handlers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;

#pragma warning disable CS4014

namespace DotNetBrowserBlazorAvaloniaApp4.Browser {
    public partial class BlazorBrowserView : UserControl {
        public static readonly
        DirectProperty<BlazorBrowserView, ObservableCollection<RootComponent>>
        RootComponentsProperty
        = AvaloniaProperty
        .RegisterDirect<BlazorBrowserView, ObservableCollection<RootComponent>>(
        nameof(RootComponents),
        x => x.RootComponents,
        (x, y) => x.RootComponents = y);

        private IBrowser browser;

        private BrowserManager browserManager;
        private IEngine engine;

        public string HostPage { get; set; }

        public ObservableCollection<RootComponent> RootComponents { get; set; } = new();

        public IServiceProvider Services { get; set; }

        public BlazorBrowserView() {
            InitializeComponent();
        }

        public virtual IFileProvider CreateFileProvider(string contentRootDir) {
            if (Directory.Exists(contentRootDir)) {
                // This is used after publishing, or if you're copying "wwwroot" content to the "bin" directory in development.
                return new PhysicalFileProvider(contentRootDir);
            }

            // This is used in development. The files come from Microsoft.AspNetCore.Components.WebView.StaticContentProvider.
            return new NullFileProvider();
        }

        public async Task Initialize() {
            if (Design.IsDesignMode) {
                // Don't perform any initialization when in designer.
                return;
            }

            // Create and initialize the IEngine instance.
            EngineOptions engineOptions = new EngineOptions.Builder {
                RenderingMode = RenderingMode.HardwareAccelerated,
                LicenseKey = "",
                Schemes = {
                    {
                        Scheme.Https,
                        new Handler<InterceptRequestParameters,
                        InterceptRequestResponse>(OnHandleRequest)
                    }
                }
            }.Build();
            engine = await EngineFactory.CreateAsync(engineOptions);

            browser = engine.CreateBrowser();

            string assemblyLocation = Assembly.GetEntryAssembly()?.Location;
            string applicationLocation = !string.IsNullOrEmpty(assemblyLocation)
            ? Path.GetDirectoryName(assemblyLocation)!
            : Environment.CurrentDirectory;
            string hostPageFullPath =
            Path.GetFullPath(Path.Combine(applicationLocation,
            HostPage!));
            string contentRootFullPath = Path.GetDirectoryName(hostPageFullPath)!;
            string hostPageRelativePath =
            Path.GetRelativePath(contentRootFullPath, hostPageFullPath);

            // Create and initialize BrowserManager that will manage activities for Razor components.
            browserManager = new BrowserManager(
            browser,
            Services,
            AvaloniaDispatcher.Instance,
            CreateFileProvider(contentRootFullPath),
            new JSComponentConfigurationStore(),
            hostPageRelativePath);

            // Add root components to BrowserManager.
            foreach (RootComponent rootComponent in RootComponents) {
                await rootComponent.AddToWebViewManagerAsync(browserManager);
            }

            // Perform initial navigation.
            browserManager.Navigate("/");

            // Show BrowserView asynchronously in UI thread.
            Dispatcher.UIThread.InvokeAsync(ShowView);
        }

        public InterceptRequestResponse OnHandleRequest(
        InterceptRequestParameters interceptRequestParameters) =>
        // Let BrowserManager handle the request.
        browserManager?.OnHandleRequest(interceptRequestParameters);

        public void Shutdown() {
            engine?.Dispose();
        }

        private InputEventResponse OnKeyPressed(IKeyPressedEventArgs args) {
            if (args.VirtualKey == KeyCode.F12) {
                browser?.DevTools.Show();
            }

            return InputEventResponse.Proceed;
        }

        private void ShowView() {
            BrowserView.InitializeFrom(browser);
            BrowserView.IsVisible = true;
            browser?.Focus();
        }
    }
}