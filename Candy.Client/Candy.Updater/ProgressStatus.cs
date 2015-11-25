namespace Candy.Updater
{
    /// <summary>
    /// 進捗の状況を表します。
    /// </summary>
    public struct ProgressStatus
    {
        private readonly int _percentage;
        private readonly string _message;

        /// <summary>
        /// 進捗率を取得します。
        /// </summary>
        public int Percentage
        {
            get { return _percentage; }
        }
        /// <summary>
        /// 状況を表すメッセージを取得します。
        /// </summary>
        public string Message
        {
            get { return _message; }
        }

        public ProgressStatus(int percentage, string message)
        {
            _message = message;
            _percentage = percentage;
        }
    }
}