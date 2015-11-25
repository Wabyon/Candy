using System;

namespace Candy.Client.Models
{
    public class OssLibraryInfo : NotificationObject
    {
        private string _name;
        private Uri _projectUri;

        public string Name
        {
            get { return _name; }
            set { SetValue(ref _name, value); }
        }

        public Uri ProjectUri
        {
            get { return _projectUri; }
            set { SetValue(ref _projectUri, value); }
        }
    }
}
