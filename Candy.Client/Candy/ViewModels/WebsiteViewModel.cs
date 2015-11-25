using System;
using System.Diagnostics;
using Candy.Client.Models;
using Livet;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Candy.Client.ViewModels
{
    public class WebsiteViewModel : ViewModel
    {
        public ReactiveProperty<string> Name { get; private set; }
        public ReactiveProperty<Uri> Uri { get; private set; }

        public ReactiveCommand NavigateUriCommand { get; private set; }

        private WebsiteViewModel()
        {
            NavigateUriCommand = new ReactiveCommand();
            NavigateUriCommand.Subscribe(_ => NavigateUri());
        }
        public WebsiteViewModel(string name, Uri projectUri)
            : this()
        {
            Name = new ReactiveProperty<string> { Value = name };
            Uri = new ReactiveProperty<Uri> { Value = projectUri };
        }
        public WebsiteViewModel(OssLibraryInfo info)
            : this()
        {
            Name = info.ToReactivePropertyAsSynchronized(x => x.Name);
            Uri = info.ToReactivePropertyAsSynchronized(x => x.ProjectUri);
        }

        private void NavigateUri()
        {
            Process.Start(Uri.Value.ToString());
        }
    }
}