using System;
using System.Collections.Generic;

namespace Candy.Client.Utilities
{
    public static class DisposableExtensions
    {
        public static TDisposable AddTo<TDisposable>(this TDisposable disposable, ICollection<IDisposable> container)
            where TDisposable : IDisposable
        {
            container.Add(disposable);
            return disposable;
        }
    }
}