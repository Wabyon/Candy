using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using Candy.Client.Messaging;
using Candy.Client.Models;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using StatefulModel;
using ErrorEventArgs = Candy.Client.Models.ErrorEventArgs;

namespace Candy.Client.ViewModels
{
    /// <summary>
    /// メイン画面のビュー ロジックを提供します。
    /// </summary>
    public class MainWindowViewModel : ViewModel
    {
        private readonly ApplicationManager _manager;
        private readonly SynchronizationContextCollection<ApplicationViewModel> _applications;

        /// <summary>
        /// 設定画面が開いているかどうかを示す値を取得します。
        /// </summary>
        public ReactiveProperty<bool> IsSettingsOpen { get; private set; }
        /// <summary>
        /// 詳細画面が開いているかどうかを示す値を取得します。
        /// </summary>
        public ReactiveProperty<bool> IsDetailsOpen { get; private set; }
        /// <summary>
        /// タイトルバーの右上に表示されるコマンドを表示するかどうかを示す値を取得します。
        /// </summary>
        public ReactiveProperty<bool> ShowWindowRightCommands { get; private set; }
        /// <summary>
        /// 待機処理が進行中かどうかを示す値を取得します。
        /// </summary>
        public ReactiveProperty<bool> IsProgressActive { get; private set; }
        /// <summary>
        /// インストール済みアプリケーションの追加時に初期表示するディレクトリのパスを取得します。
        /// </summary>
        public ReactiveProperty<string> InitialDirectory { get; private set; }
        /// <summary>
        /// 登録済みのアプリケーション一覧を取得します。
        /// </summary>
        public IReadOnlyCollection<ApplicationViewModel> Applications
        {
            get { return _applications; }
        }

        /// <summary>
        /// 
        /// </summary>
        public ReactiveCommand ShowHelpCommand { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public ReactiveCommand RegisterApplicationCommand { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public ReactiveCommand InstallApplicationCommand { get; private set; }
        /// <summary>
        /// 設定画面を表示するコマンドを取得します。
        /// </summary>
        public ReactiveCommand OpenSettingCommand { get; private set; }
        /// <summary>
        /// 選択されたアプリケーションのメニューを表示するコマンドを取得します。
        /// </summary>
        public ReactiveCommand<ApplicationViewModel> ShowDetailsCommand { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ReactiveProperty<ApplicationViewModel> CurrentItem { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public ReactiveProperty<SettingsViewModel> SettingsViewModel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        public MainWindowViewModel(ApplicationManager manager)
        {
            _manager = manager;
            _manager.Error += manager_Error;
            _applications = _manager.Applications.ToSyncedSynchronizationContextCollection(x => new ApplicationViewModel(_manager, x), SynchronizationContext.Current);

            IsSettingsOpen = new ReactiveProperty<bool>(false);
            IsDetailsOpen = new ReactiveProperty<bool>(false);
            IsProgressActive = new ReactiveProperty<bool>(false);

            ShowWindowRightCommands = IsSettingsOpen.CombineLatest(IsDetailsOpen, (a, b) => !a && !b)
                                                    .ToReactiveProperty();

            InitialDirectory = _manager.Settings.ToReactivePropertyAsSynchronized(x => x.ApplicationRootDirectoryPath);

            ShowHelpCommand = new ReactiveCommand();
            ShowHelpCommand.Subscribe(_ => ShowHelp());

            RegisterApplicationCommand = IsProgressActive.Select(x => !x).ToReactiveCommand();
            RegisterApplicationCommand.Subscribe(_ => RegisterApplication());

            InstallApplicationCommand = IsProgressActive.Select(x => !x).ToReactiveCommand();
            InstallApplicationCommand.Subscribe(_ => InstallApplication());

            OpenSettingCommand = new ReactiveCommand();
            OpenSettingCommand.Subscribe(_ => ShowSettings());

            ShowDetailsCommand = new ReactiveCommand<ApplicationViewModel>();
            ShowDetailsCommand.Subscribe(ShowDetails);

            CurrentItem = new ReactiveProperty<ApplicationViewModel>();

            SettingsViewModel = new ReactiveProperty<SettingsViewModel>();
        }

        /// <summary>
        /// 
        /// </summary>
        public async void Initialize()
        {
            IsProgressActive.Value = true;
            await _manager.LoadAsync();
            IsProgressActive.Value = false;

            if (String.IsNullOrEmpty(_manager.Settings.ApplicationRootDirectoryPath))
            {
                await _manager.AutoSuggestApplicationDirectory();

                if (!String.IsNullOrEmpty(_manager.Settings.ApplicationRootDirectoryPath))
                {
                    Messenger.Raise(new AsyncInformationMessage
                    {
                        MessageKey = "Information",
                        Text = "アプリケーション ディレクトリを " + _manager.Settings.ApplicationRootDirectoryPath + " に設定しました。" +
                               "このディレクトリがアプリケーション ディレクトリではない場合は設定画面から変更してください。"
                    });
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public async void Save()
        {
            await _manager.SaveAsync();
        }
        /// <summary>
        /// 
        /// </summary>
        public void ShowHelp()
        {
            Messenger.Raise(new TransitionMessage
            {
                MessageKey = "ShowHelp",
                TransitionViewModel = new HelpViewModel()
            });
        }
        /// <summary>
        /// アプリケーションを登録します。
        /// </summary>
        public async void RegisterApplication()
        {
            var selectedPath = Messenger.GetResponse(new OpeningFileSelectionMessage
            {
                MessageKey = "OpenFile",
                Title = "インストール済みアプリケーションの追加",
                Filter = "アプリケーション ファイル(*.exe)|*.exe",
                InitialDirectory = InitialDirectory.Value,
            
            }).Response;

            if (selectedPath == null || !selectedPath.Any()) return;

            await _manager.RegisterInstalledApplication(selectedPath[0]);
        }
        /// <summary>
        /// 
        /// </summary>
        public async void InstallApplication()
        {
            if (String.IsNullOrEmpty(_manager.Settings.ApplicationRootDirectoryPath))
            {
                Messenger.Raise(new AsyncInformationMessage
                {
                    MessageKey = "Information",
                    Caption = "エラー",
                    Text = "アプリケーション ディレクトリが設定されていません。設定画面でディレクトリを指定してください。"
                });

                ShowSettings();
                return;
            }

            if (!Directory.Exists(_manager.Settings.ApplicationRootDirectoryPath))
            {
                Messenger.Raise(new AsyncInformationMessage
                {
                    MessageKey = "Information",
                    Caption = "エラー",
                    Text = "指定されているアプリケーション ディレクトリが存在しません。設定画面でディレクトリを確認してください。"
                });

                IsSettingsOpen.Value = true;
                return;
            }

            var id = await Messenger.GetResponse(new AsyncInputMessage
            {
                MessageKey = "Input",
                Caption = "アプリケーションのインストール",
                Text = "アプリケーションの識別子を入れてください"

            }).Response;

            if (String.IsNullOrEmpty(id)) return;

            var success = await _manager.InstallApplicationAsync(id);

            if (success)
            {
                Messenger.Raise(new AsyncInformationMessage
                {
                    MessageKey = "Information",
                    Caption = "インストール完了",
                    Text = "アプリケーションのインストールが完了しました。"
                });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void ShowSettings()
        {
            SettingsViewModel.Value = new SettingsViewModel(_manager.Settings);
            IsSettingsOpen.Value = true;
        }
        /// <summary>
        /// 指定されたアプリケーションのメニューを表示します。
        /// </summary>
        /// <param name="app"></param>
        public void ShowDetails(ApplicationViewModel app)
        {
            IsDetailsOpen.Value = true;
            CurrentItem.Value = app;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void manager_Error(object sender, ErrorEventArgs e)
        {
            Messenger.Raise(new AsyncInformationMessage
            {
                MessageKey = "Information",
                Caption = "エラー",
                Text = e.Message
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _manager.Dispose();

                foreach (var appViewModel in Applications)
                {
                    appViewModel.Dispose();
                }
            }
        }
    }
}
