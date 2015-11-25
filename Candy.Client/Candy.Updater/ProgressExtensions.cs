using System;

namespace Candy.Updater
{
    public static class ProgressExtensions
    {
        public static void Report(this IProgress<ProgressStatus> progress, int percentage, string message)
        {
            progress.Report(new ProgressStatus(percentage, message));
        }
    }
}