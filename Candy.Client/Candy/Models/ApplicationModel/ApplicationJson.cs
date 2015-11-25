using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Candy.Client.Models
{
    /// <summary>
    /// アプリケーション情報サービスから返却されるオブジェクトをデシリアライズするためのコンテナとして使用します。
    /// </summary>
    [DataContract]
    public class ApplicationJson
    {
        [DataMember(Name = "defaultService")]
        public string DefaultServiceUrl { get; set; }
        [DataMember(Name = "defaultInstall")]
        public string DefaultInstallUrl { get; set; }
        [DataMember(Name = "applications")]
        public IList<ApplicationMetadata> Applications { get; private set; }

        public ApplicationJson()
        {
            Applications = new List<ApplicationMetadata>();
        }
    }
}