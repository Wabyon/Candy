using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Candy.Server.Models
{
    /// <summary>
    /// 更新の情報を表します。
    /// </summary>
    [DataContract]
    public class UpdateSummary
    {
        private readonly HashSet<string> _allowedUserIds = new HashSet<string>();
        private readonly List<string> _removeFiles = new List<string>();

        /// <summary>
        /// この更新のバージョンを取得または設定します。
        /// </summary>
        [DataMember(Name = "version")]
        public Version Version { get; set; }

        /// <summary>
        /// この更新がリリースされた日付を取得または設定します。
        /// </summary>
        [DataMember(Name = "date")]
        public DateTime PublishDate { get; set; }

        /// <summary>
        /// この更新がサポートするバージョン（この更新を適用可能な最低バージョン）を取得または設定します。
        /// </summary>
        [DataMember(Name = "for")]
        public Version SupportedVersion { get; set; }

        /// <summary>
        /// この更新の適用を許可する対象のユーザー一覧を取得します。
        /// </summary>
        [DataMember(Name = "allow")]
        public ICollection<string> AllowedUserIds
        {
            get { return _allowedUserIds; }
        }

        /// <summary>
        /// 削除するファイルの一覧を取得します。
        /// </summary>
        [DataMember(Name = "remove")]
        public ICollection<string> RemoveFiles
        {
            get { return _removeFiles; }
        }

        /// <summary>
        /// この更新のパッケージ ファイルのパスを取得または設定します。
        /// </summary>
        [DataMember(Name = "package")]
        public string PackagePath { get; set; }

        /// <summary>
        /// この更新の更新内容を取得または設定します。
        /// </summary>
        [DataMember(Name = "releaseNote")]
        public string ReleaseNote { get; set; }
    }
}