using System.Diagnostics;
using System.Net;
using DotNetBrowser.Browser;
using DotNetBrowser.Browser.Handlers;
using DotNetBrowser.Handlers;
using DotNetBrowser.Js;
using DotNetBrowser.Net;
using DotNetBrowser.Net.Handlers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.FileProviders;

namespace DotNetBrowserBlazorAvaloniaApp4.Browser {
    internal class BrowserManager : WebViewManager {
        // The address used for request interception.
        private static readonly string AppHostAddress = "0.0.0.0";
        private static readonly string AppOrigin = $"https://{AppHostAddress}/";

        /// <summary>
        ///     The application's base URI.
        /// </summary>
        private static readonly Uri AppOriginUri = new(AppOrigin);

        private IJsFunction callbackFunction;
        private IBrowser Browser { get; }

        public BrowserManager(IBrowser browser, IServiceProvider provider,
        Dispatcher dispatcher,
        IFileProvider fileProvider,
        JSComponentConfigurationStore jsComponents,
        string hostPageRelativePath)
        : base(provider, dispatcher, AppOriginUri, fileProvider, jsComponents,
        hostPageRelativePath) {
            Browser = browser;
            Browser.InjectJsHandler = new Handler<InjectJsParameters>(OnInjectJs);
        }

        public InterceptRequestResponse OnHandleRequest(InterceptRequestParameters p) {
            if (!p.UrlRequest.Url.StartsWith(AppOrigin)) {
                return InterceptRequestResponse.Proceed();
            }

            ResourceType resourceType = p.UrlRequest.ResourceType;
            bool allowFallbackOnHostPage = resourceType is ResourceType.MainFrame
            or ResourceType.Favicon
            or ResourceType.SubResource;

            if (TryGetResponseContent(p.UrlRequest.Url, allowFallbackOnHostPage,
                out int statusCode, out string _,
                out Stream content,
                out IDictionary<string, string> headers)) {
                Trace.WriteLine($"HTTP {statusCode} for {resourceType} : {p.UrlRequest.Url}");
                UrlRequestJob urlRequestJob = p.Network.CreateUrlRequestJob(p.UrlRequest,
                new UrlRequestJobOptions {
                    HttpStatusCode = (HttpStatusCode)statusCode,
                    Headers = headers
                    .Select(pair => new HttpHeader(pair.Key, pair.Value))
                    .ToList()
                });
                Task.Run(() => {
                    using (MemoryStream memoryStream = new()) {
                        content.CopyTo(memoryStream);
                        urlRequestJob.Write(memoryStream.ToArray());
                    }

                    urlRequestJob.Complete();
                });
                return InterceptRequestResponse.Intercept(urlRequestJob);
            }

            return InterceptRequestResponse.Proceed();
        }

        protected override void NavigateCore(Uri absoluteUri) {
            Browser.Navigation.LoadUrl(absoluteUri.AbsoluteUri);
        }

        protected override void SendMessage(string message) {
            Trace.WriteLine($"SendMessage: {message}");
            callbackFunction?.Invoke(null, message);
        }

        private void OnInjectJs(InjectJsParameters p) {
            if (!p.Frame.IsMain) {
                return;
            }

            dynamic window = p.Frame.ExecuteJavaScript("window").Result;
            window.external = p.Frame.ParseJsonString("{}");
            window.external.sendMessage = (Action<dynamic>)OnMessageReceived;
            window.external.receiveMessage = (Action<dynamic>)SetupCallback;
        }

        private void OnMessageReceived(dynamic obj) {
            Trace.WriteLine($"OnMessageReceived: {obj}");
            this.MessageReceived(new Uri(Browser.Url), obj.ToString());
        }

        private void SetupCallback(dynamic callback) {
            callbackFunction = callback as IJsFunction;
        }
    }
}