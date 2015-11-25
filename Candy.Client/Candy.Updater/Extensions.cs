using System;

namespace Candy.Updater
{
    internal static class Extensions
    {
        public static TResult Maybe<T, TResult>(this T self, Func<T, TResult> selector)
            where T : class
        {
            return self == null ? default(TResult) : selector(self);
        }
    }
}