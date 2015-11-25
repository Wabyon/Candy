using System.Configuration;
using System.Runtime.Serialization;
using AutoMapper;

namespace Candy.Client.Models
{
    [DataContract]
    public class CandySettings : NotificationObject
    {
        private string _applicationRootDirectoryPath;
        private string _applicationInformationServiceUrl;

        public static readonly string DefaultApplicationInformationServiceUrl = ConfigurationManager.AppSettings["candy:DefaultAppService"];

        [DataMember(Name = "appRoot")]
        public string ApplicationRootDirectoryPath
        {
            get { return _applicationRootDirectoryPath; }
            set { SetValue(ref _applicationRootDirectoryPath, value); }
        }
        [DataMember(Name = "appService")]
        public string ApplicationInformationServiceUrl
        {
            get { return _applicationInformationServiceUrl; }
            set { SetValue(ref _applicationInformationServiceUrl, value); }
        }

        static CandySettings()
        {
            Mapper.CreateMap<CandySettings, CandySettings>();
        }

        public void SetDefaultService()
        {
            ApplicationInformationServiceUrl = DefaultApplicationInformationServiceUrl;
        }
        public CandySettings Clone()
        {
            return Mapper.Map<CandySettings>(this);
        }
        public void ApplySettings(CandySettings newSettings)
        {
            Mapper.Map(newSettings, this);
        }
    }
}
