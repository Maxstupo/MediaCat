namespace MediaCat.Core.Utility {
    using System.Threading;
    using System.Threading.Tasks;
    using System;

    // https://devblogs.microsoft.com/pfxteam/cooperatively-pausing-async-methods/
    public sealed class PauseTokenSource {
        internal static readonly Task CompletedTask = Task.FromResult(true);

        public PauseToken Token => new PauseToken(this);

        private volatile TaskCompletionSource<bool> paused;

        public bool IsPaused {
            get => paused != null;
            set {
                if (value) {
                    Interlocked.CompareExchange(ref paused, new TaskCompletionSource<bool>(), null);
                } else {
                    while (true) {
                        TaskCompletionSource<bool> tcs = paused;
                        if (tcs == null)
                            return;
                        if (Interlocked.CompareExchange(ref paused, null, tcs) == tcs) {
                            tcs.SetResult(true);
                            break;
                        }
                    }
                }
            }
        }

        internal Task WaitWhilePausedAsync() {
            TaskCompletionSource<bool> current = paused;
            return current != null ? current.Task : CompletedTask;
        }

    }

    public struct PauseToken {
        private readonly PauseTokenSource source;

        public bool IsPaused => source.IsPaused;

        internal PauseToken(PauseTokenSource source) {
            this.source = source ?? throw new ArgumentNullException();
        }

        public Task WaitWhilePausedAsync() {
            return IsPaused ? source.WaitWhilePausedAsync() : PauseTokenSource.CompletedTask;
        }

        public Task WaitWhilePausedAsync(CancellationToken ct) {
            return (!IsPaused || ct.IsCancellationRequested) ? PauseTokenSource.CompletedTask : source.WaitWhilePausedAsync();
        }

    }

}
