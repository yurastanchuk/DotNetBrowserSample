using Microsoft.AspNetCore.Components;

namespace DotNetBrowserBlazorAvaloniaApp4.Browser {
    public sealed class AvaloniaDispatcher : Dispatcher {
        public static AvaloniaDispatcher Instance { get; } = new();

        private AvaloniaDispatcher() {
        }

        public override bool CheckAccess()
            => Avalonia.Threading.Dispatcher.UIThread.CheckAccess();

        public override Task InvokeAsync(Action workItem) => Avalonia.Threading.Dispatcher
        .UIThread.InvokeAsync(workItem)
        .GetTask();

        public override Task InvokeAsync(Func<Task> workItem)
            => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(workItem);

        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
            => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(workItem).GetTask();

        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
            => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(workItem);
    }
}