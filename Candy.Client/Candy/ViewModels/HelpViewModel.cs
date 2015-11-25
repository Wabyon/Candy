using System;
using System.Linq;
using System.Threading;
using Candy.Client.Models;
using Livet;
using Reactive.Bindings;
using StatefulModel;

namespace Candy.Client.ViewModels
{
    public class HelpViewModel : ViewModel
    {
        private readonly SynchronizationContextCollection<WebsiteViewModel> _usingLibraries;
        
        public SynchronizationContextCollection<WebsiteViewModel> UsingLibraries
        {
            get { return _usingLibraries; }
        }

        public WebsiteViewModel IconProvider { get; private set; }

        public HelpViewModel()
        {
            var sites = new[]
            {
                new OssLibraryInfo
                {
                    Name = "Auto Mapper",
                    ProjectUri = new Uri("http://automapper.org/")
                },
                new OssLibraryInfo
                {
                    Name = "Rx(Reactive Extensions) / Ix(Interactive Extensions)",
                    ProjectUri = new Uri("https://rx.codeplex.com/")
                },
                new OssLibraryInfo
                {
                    Name = "Json.NET",
                    ProjectUri = new Uri("http://www.newtonsoft.com/json")
                },
                new OssLibraryInfo
                {
                    Name = "Livet",
                    ProjectUri = new Uri("http://ugaya40.hateblo.jp/entry/Livet")
                },
                new OssLibraryInfo
                {
                    Name = "StatefulModel",
                    ProjectUri = new Uri("https://github.com/ugaya40/StatefulModel")
                },
                new OssLibraryInfo
                {
                    Name = "MahApps.Metro",
                    ProjectUri = new Uri("http://mahapps.com/")
                },
                new OssLibraryInfo
                {
                    Name = "NLog",
                    ProjectUri = new Uri("http://nlog-project.org/")
                },
                new OssLibraryInfo
                {
                    Name = "ReactiveProperty",
                    ProjectUri = new Uri("https://github.com/runceel/ReactiveProperty")
                },
            };

            _usingLibraries = new SynchronizationContextCollection<WebsiteViewModel>(
                sites.Select(x => new WebsiteViewModel(x)),
                SynchronizationContext.Current);

            IconProvider = new WebsiteViewModel("IconDrawer", new Uri("http://www.icondrawer.com/"));
        }
    }
}
