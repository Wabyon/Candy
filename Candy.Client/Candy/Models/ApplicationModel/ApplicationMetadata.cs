using System.Runtime.Serialization;

namespace Candy.Client.Models
{
    /// <summary>
    /// �A�v���P�[�V�������T�[�r�X����ԋp�����I�u�W�F�N�g�́A�e�A�v���P�[�V�����̏����f�V���A���C�Y���邽�߂̃R���e�i�Ƃ��Ďg�p���܂��B
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