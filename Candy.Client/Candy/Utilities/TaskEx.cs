using System.Threading.Tasks;
using NLog;

namespace Candy.Client.Utilities
{
    /// <summary>
    /// <see cref="Task"/> を拡張します。
    /// </summary>
    public static class TaskEx
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 完了したタスクを取得します。
        /// </summary>
        public static Task Completed
        {
            // .NET4.6 から追加された
            get { return Task.FromResult((object)null); }
        }
        /// <summary>
        /// タスクで発生する例外を <see cref="Logger"/> に記録するように継続を登録して、このタスクの完了を待機せずに放棄します。
        /// </summary>
        /// <param name="task">完了を待機せずに放置するタスク。</param>
        public static void FireAndForgot(this Task task)
        {
            task.ContinueWith(t => _logger.Error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
