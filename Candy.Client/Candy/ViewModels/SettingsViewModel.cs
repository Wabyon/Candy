using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using Candy.Client.Models;
using Livet;
using Livet.Messaging;
using Livet.Messaging.IO;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Candy.Client.ViewModels
{
    public class SettingsViewModel : ViewModel
    {
        private readonly CandySettings _settings;
        private readonly CandySettings _phantom;

        [Required(ErrorMessage="この項目は必須です")]
        public ReactiveProperty<string> ApplicationInformationServiceUrl { get; private set; }
        public ReactiveProperty<string> ApplicationRootDirectoryPath { get; private set; }

        public ReactiveCommand SetDefaultServiceCommand { get; private set; }
        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        public SettingsViewModel(CandySettings settings)
        {
            _settings = settings;
            _phantom = _settings.Clone();

            ApplicationInformationServiceUrl = _phantom.ToReactivePropertyAsSynchronized(x => x.ApplicationInformationServiceUrl)
                                                       .SetValidateAttribute(() => ApplicationInformationServiceUrl);
            ApplicationRootDirectoryPath = _phantom.ToReactivePropertyAsSynchronized(x => x.ApplicationRootDirectoryPath);

            SetDefaultServiceCommand = ApplicationInformationServiceUrl.DistinctUntilChanged()
                                                                       .Select(x => x != CandySettings.DefaultApplicationInformationServiceUrl)
                                                                       .ToReactiveCommand();
            SetDefaultServiceCommand.Subscribe(_ => SetDefaultService());

            OkCommand = new ReactiveCommand();
            OkCommand.Subscribe(_ => ApplySettings());

            CancelCommand = new ReactiveCommand();
            CancelCommand.Subscribe(_ => Cancel());
        }

        public void OnApplicationDirectorySelected(FolderSelectionMessage m)
        {
            if (m.Response != null)
            {
                ApplicationRootDirectoryPath.Value = m.Response;
            }
        }
        public void Initialize()
        {
        }
        public void SetDefaultService()
        {
            _phantom.SetDefaultService();
        }
        private void ApplySettings()
        {
            if (ApplicationInformationServiceUrl.HasErrors)
            {
                return;
            }

            _settings.ApplySettings(_phantom);

            Messenger.Raise(new InteractionMessage
            {
                MessageKey = "Close"
            });
        }
        private void Cancel()
        {
            Messenger.Raise(new InteractionMessage
            {
                MessageKey = "Close"
            });
        }
    }
}
