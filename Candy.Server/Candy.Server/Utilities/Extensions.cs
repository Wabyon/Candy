using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Candy.Server.Utilities
{
    public static class Extensions
    {
        public static TResult Maybe<T, TResult>(this T self, Func<T, TResult> selector)
            where T : class
        {
            return self == null ? default(TResult) : selector(self);
        }
    }
}