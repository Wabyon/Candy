using System.Runtime.Serialization;

namespace Candy.Client.Models
{
    /// <summary>
    /// アプリケーション情報サービスから返却されるオブジェクトの、各アプリケーションの情報をデシリアライズするためのコンテナとして使用します。
    /// </summary>
    [DataContract]
    public class ApplicationMetadata
    {
        [DataMember(Name = "fileName")]
        public string Id { get; set; }
        [DataMember(Name = "name")]
        public string DisplayName { get; set; }
        [DataMember(Name = "definition")]
        public string Definition { get; set; }
        [DataMember(Name = "updateUrl")]
        public string UpdateServiceUrl { get; set; }
        [DataMember(Name = "installUrl")]
        public string InstallUrl { get; set; }
        [DataMember(Name = "developername")]
        public string DeveloperName { get; set; }
    }
}