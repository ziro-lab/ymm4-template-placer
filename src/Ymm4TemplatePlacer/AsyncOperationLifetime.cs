namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private CancellationTokenSource asyncOperationLifetime = new();
    private long asyncOperationEpoch;
    private bool asyncOperationLifetimeDisposed;

    private sealed class AsyncOperationLease : IDisposable
    {
        private readonly PlacerViewModel owner;
        private readonly long epoch;
        private readonly string task;
        private readonly CancellationTokenSource? linked;

        public CancellationToken Token { get; }

        public AsyncOperationLease(PlacerViewModel owner, CancellationToken external)
        {
            this.owner = owner;
            epoch = owner.asyncOperationEpoch;
            task = owner.activeTask;
            if (external.CanBeCanceled)
            {
                linked = CancellationTokenSource.CreateLinkedTokenSource(
                    owner.asyncOperationLifetime.Token, external);
                Token = linked.Token;
            }
            else
            {
                Token = owner.asyncOperationLifetime.Token;
            }
        }

        public void Validate()
        {
            Token.ThrowIfCancellationRequested();
            if (owner.asyncOperationLifetimeDisposed ||
                owner.asyncOperationEpoch != epoch ||
                owner.activeTask != task)
                throw new OperationCanceledException(
                    "操作中にToolの表示・作業タブまたは対象シーンが変わりました。配置・再同期は確定していません。",
                    Token);
        }

        public void Dispose() => linked?.Dispose();
    }

    private AsyncOperationLease BeginAsyncOperation(CancellationToken external = default)
    {
        if (asyncOperationLifetimeDisposed)
            throw new ObjectDisposedException(nameof(PlacerViewModel));
        return new AsyncOperationLease(this, external);
    }

    private void InvalidateAsyncOperationLifetime()
    {
        if (asyncOperationLifetimeDisposed) return;
        asyncOperationEpoch++;
        var previous = asyncOperationLifetime;
        asyncOperationLifetime = new CancellationTokenSource();
        try { previous.Cancel(); }
        finally { previous.Dispose(); }
    }

    private void DisposeAsyncOperationLifetime()
    {
        if (asyncOperationLifetimeDisposed) return;
        asyncOperationLifetimeDisposed = true;
        asyncOperationEpoch++;
        try { asyncOperationLifetime.Cancel(); }
        finally { asyncOperationLifetime.Dispose(); }
    }
}
