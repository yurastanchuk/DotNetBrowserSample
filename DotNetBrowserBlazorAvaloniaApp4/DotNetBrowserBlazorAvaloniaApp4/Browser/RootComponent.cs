using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebView;

namespace DotNetBrowserBlazorAvaloniaApp4.Browser {
    public class RootComponent {
        /// <summary>
        ///     Gets the type of the root component.
        /// </summary>
        public string ComponentType { get; set; }

        /// <summary>
        ///     Gets an optional dictionary of parameters to pass to the root component.
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }

        /// <summary>
        ///     Gets the CSS selector string that specifies where in the document the component should be placed.
        ///     This must be unique among the root components within the <see cref="BlazorBrowserView" />.
        /// </summary>
        public string Selector { get; set; }

        public Task AddToWebViewManagerAsync(WebViewManager webViewManager) {
            ParameterView parameterView = Parameters == null
            ? ParameterView.Empty
            : ParameterView.FromDictionary(Parameters);
            return webViewManager?.AddRootComponentAsync(Type.GetType(ComponentType)!, Selector,
            parameterView);
        }
    }
}