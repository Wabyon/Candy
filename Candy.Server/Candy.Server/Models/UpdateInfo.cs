using System.Runtime.Serialization;

namespace Candy.Server.Models
{
    /// <summary>
    /// アプリケーション更新に関する情報を表します。
    /// </summary>
    [DataContract]
    public class UpdateInfo
    {
        private readonly UpdateSummaryCollection _updateSummaries = new UpdateSummaryCollection();

        /// <summary>
        /// 更新の一覧を取得します。
        /// </summary>
        [DataMember(Name = "update")]
        public UpdateSummaryCollection UpdateSummaries
        {
            get { return _updateSummaries; }
        }
    }
}