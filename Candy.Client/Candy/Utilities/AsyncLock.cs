using System;
using System.Threading;
using System.Threading.Tasks;

namespace Candy.Client.Utilities
{
    /// <summary>
    /// セマフォベースの非同期ロックを提供します。
    /// </summary>
    public sealed class AsyncLock
    {
        // http://www.atmarkit.co.jp/ait/articles/1411/18/news135.html

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Task<IDisposable> _releaser;

        /// <summary>
        /// <see cref="AsyncLock"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public AsyncLock()
        {
            _releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        /// <summary>
        /// ロックを確保するまで非同期に待機します。
        /// </summary>
        /// <returns>ロックを解放するためのインスタンスを非同期に返却する <see cref="Task{T}"/> 。</returns>
        public Task<IDisposable> LockAsync()
        {
            var wait = _semaphore.WaitAsync();
            if (wait.IsCompleted)
            {
                return _releaser;
            }
            else
            {
                return wait.ContinueWith(
                    (_, state) => (IDisposable)state,
                    _releaser.Result,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default
                    );
            }
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock _toRelease;
            
            internal Releaser(AsyncLock toRelease)
            {
                _toRelease = toRelease;
            }

            public void Dispose()
            {
                _toRelease._semaphore.Release();
            }
        }
    }
}