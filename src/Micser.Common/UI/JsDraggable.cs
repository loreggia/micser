using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Micser.Common.UI
{
    public class JsDraggable : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

        public JsDraggable(IJSRuntime jsRuntime)
        {
            _moduleTask = new Lazy<Task<IJSObjectReference>>(() => jsRuntime.InvokeAsync<IJSObjectReference>(
               "import", "./_content/Micser.Common/js/draggable.js").AsTask());
        }

        public async ValueTask DisposeAsync()
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }

        public async ValueTask MakeDraggableAsync(ElementReference element, double left, double top)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("draggable", element, left, top).ConfigureAwait(false);
        }
    }
}