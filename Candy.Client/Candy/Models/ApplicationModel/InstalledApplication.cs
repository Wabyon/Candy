using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Candy.Client.Utilities;
using Newtonsoft.Json;

namespace Candy.Client.Models
{
    /// <summary>
    /// インストール済みのアプリケーションを表します。
    /// </summary>
    [DataContract]
    public class InstalledApplication : NotificationObject
    {
        /// <summary>
        /// インストール先パスはイミュータブルに設計されています。この値を変更する必要がある場合、新しいインスタンスを作成することを検討してください。
        /// </summary>
        private readonly string _installedPath;
        /// <summary>
        /// 初期化の処理を直列化するためのロックを提供します。
        /// </summary>
        private readonly AsyncLock _initializeLock = new AsyncLock();

        private bool _isInitalized;
        private string _updateServiceUrlTemplate;

        #region backing fields
        private string _id;
        private string _displayName;
        private string _definition;
        private string _developerName;
        private Uri _installUrl;
        private Version _applicationVersion;
        private UpdateSummary _latest;
        private bool _isSupported;
        private bool _canExecute;
        #endregion

        /// <summary>
        /// このアプリケーションの識別子を取得します。
        /// </summary>
        public string Id
        {
            // 現状はアプリケーションの識別子＝実行ファイル名という前提のもとに作られていますが、
            // 日本語ファイル名のツールの存在を考えるとインストール時に使用する識別子はエイリアスを設けられるようにした方がいいかもしれません。
            get { return _id; }
            private set { SetValue(ref _id, value); }
        }
        /// <summary>
        /// このアプリケーションの表示名を取得します。
        /// </summary>
        public string DisplayName
        {
            get { return _displayName; }
            private set { SetValue(ref _displayName, value); }
        }
        /// <summary>
        /// このアプリケーションの説明を取得します。
        /// </summary>
        public string Definition
        {
            get { return _definition; }
            private set { SetValue(ref _definition, value); }
        }
        /// <summary>
        /// このアプリケーションの開発者を取得します。
        /// </summary>
        public string DeveloperName
        {
            get { return _developerName; }
            private set { SetValue(ref _developerName, value); }
        }
        /// <summary>
        /// このアプリケーションの実行ファイル (*.exe) のパスを取得します。
        /// </summary>
        [JsonProperty(PropertyName="path")]
        public string InstalledPath
        {
            get { return _installedPath; }
        }
        /// <summary>
        /// このアプリケーションをインストールするために必要なパッケージの <see cref="Uri"/> を取得します。
        /// </summary>
        public Uri InstallUrl
        {
            get { return _installUrl; }
            private set { SetValue(ref _installUrl, value); }
        }
        /// <summary>
        /// このアプリケーションの現在のバージョンを取得します。
        /// </summary>
        public Version ApplicationVersion
        {
            get { return _applicationVersion; }
            private set { SetValue(ref _applicationVersion, value); }
        }
        /// <summary>
        /// このアプリケーションに対して適用可能な最新のリリースの情報を取得します。
        /// </summary>
        public UpdateSummary Latest
        {
            get { return _latest; }
            private set { SetValue(ref _latest, value); }
        }
        /// <summary>
        /// このアプリケーションが Candy によってサポートされているかどうかを示す値を取得します。
        /// </summary>
        public bool IsSupported
        {
            get { return _isSupported; }
            private set { SetValue(ref _isSupported, value); }
        }
        /// <summary>
        /// このアプリケーションを一覧から削除可能かどうかを示す値を取得します。
        /// </summary>
        public virtual bool CanRemove
        {
            get { return true; }
        }
        /// <summary>
        /// このアプリケーションを最新版に更新する際にユーザーに提供するためのメッセージを取得します。
        /// </summary>
        public virtual string UpdateConfirmationMessage
        {
            get { return "アプリケーションを最新版に更新します。よろしいですか？"; }
        }
        /// <summary>
        /// このインストールを実行可能かどうかを示す値を取得または設定します。
        /// </summary>
        public virtual bool CanExecute
        {
            get { return _canExecute; }
            protected set { SetValue(ref _canExecute, value); }
        }
        /// <summary>
        /// このアプリケーションが含まれているディレクトリを監視する <see cref="FileSystemWatcher"/> を取得します。
        /// </summary>
        protected FileSystemWatcher FileSystemWatcher { get; private set; }

        /// <summary>
        /// <see cref="InstalledApplication"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="installedPath"></param>
        public InstalledApplication(string installedPath)
        {
            _installedPath = installedPath;

            Id = Path.GetFileNameWithoutExtension(InstalledPath);
            CanExecute = File.Exists(InstalledPath);

            if (CanExecute)
            {
                FileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(InstalledPath), "*.exe")
                {
                    EnableRaisingEvents = true

                }.AddTo(CompositeDisposable);

                FileSystemWatcher.Changed += (_, e) =>
                {
                    if (String.Equals(InstalledPath, e.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        CanExecute = File.Exists(InstalledPath);
                    }
                };
            }
            else
            {
                FileSystemWatcher = new FileSystemWatcher();
            }
        }

        /// <summary>
        /// このアプリケーションを適用可能な最新版に更新します。
        /// </summary>
        /// <returns>更新の完了を通知するタスク。</returns>
        public virtual Task UpdateAsync()
        {
            var updateServiceUrl = GetUpdateServiceUrl();

            var arguments = String.Format(@"-n ""{0}"" -p ""{1}"" -u ""{2}""",
                                          DisplayName.Replace("\"", "\\\""),
                                          InstalledPath,
                                          updateServiceUrl);

            var source = new TaskCompletionSource<object>();
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "Candy.Updater.exe",
                    Arguments = arguments,
                },
            };
            process.Exited += (_, __) =>
            {
                process.Dispose();
                source.SetResult(null);
            };
            process.Start();

            return source.Task;
        }
        /// <summary>
        /// このアプリケーションを起動します。
        /// </summary>
        /// <returns>派生クラスでオーバーライドされた場合、起動の指示を実行するまでを待機するタスク。このタスクは起動したアプリケーションの終了を待機しません。</returns>
        public virtual Task ExecuteAsync()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = InstalledPath,
                WorkingDirectory = Path.GetDirectoryName(InstalledPath)
            });

            return TaskEx.Completed;
        }
        /// <summary>
        /// 更新のためのパッケージの <see cref="Uri"/> を取得します。
        /// </summary>
        /// <returns></returns>
        public Uri GetUpdateServiceUrl()
        {
            return new Uri(MakeUrl(_updateServiceUrlTemplate, Id, ApplicationVersion));
        }
        /// <summary>
        /// 指定されたアプリケーション情報によってこのインスタンスを初期化します。
        /// </summary>
        /// <param name="appInfo"></param>
        /// <returns></returns>
        public async Task InitializeAsync(ApplicationJson appInfo)
        {
            if (_isInitalized) return;

            using (await _initializeLock.LockAsync().ConfigureAwait(false))
            {
                if (_isInitalized) return;

                try
                {
                    FileSystemWatcher.Changed += async (_, e) =>
                    {
                        if (String.Equals(InstalledPath, e.FullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            await RefreshFileInfoAsync().ConfigureAwait(false);
                        }
                    };

                    var app = appInfo.Applications.FirstOrDefault(x => x.Id == Id);

                    if (app != null)
                    {
                        IsSupported = true;
                        _updateServiceUrlTemplate = app.UpdateServiceUrl ?? appInfo.DefaultServiceUrl;
                        InstallUrl = new Uri(app.InstallUrl ?? appInfo.DefaultInstallUrl);
                        DisplayName = app.DisplayName;
                        Definition = app.Definition;
                        DeveloperName = app.DeveloperName;

                        await RefreshFileInfoAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        IsSupported = false;
                    }
                }
                finally
                {
                    _isInitalized = true;
                }
            }
        }
        /// <summary>
        /// 実行ファイル (*.exe) から取得される情報を最新に更新します。
        /// </summary>
        /// <returns></returns>
        private async Task RefreshFileInfoAsync()
        {
            if (!File.Exists(InstalledPath))
            {
                ApplicationVersion = null;
                Latest = null;
                return;
            }

            // exe ファイルのファイルバージョンを使用
            var fileVersion = FileVersionInfo.GetVersionInfo(InstalledPath).FileVersion;
            if (fileVersion != null)
            {
                ApplicationVersion = Version.Parse(fileVersion);

                // 最新版の情報を取得
                var httpClient = new HttpClient();
                var latest = await httpClient.GetStringAsync(GetUpdateServiceUrl()).ConfigureAwait(false);

                Latest = JsonConvert.DeserializeObject<UpdateSummary>(latest);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="urlTemplate"></param>
        /// <param name="applicationName"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private static string MakeUrl(string urlTemplate, string applicationName, Version version)
        {
            return urlTemplate.Replace("{appName}", applicationName)
                              .Replace("{version}", version.ToString())
                              .Replace("{user}", Environment.UserName);
        }
    }
}