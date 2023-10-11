using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Candy.Client.Utilities;
using Newtonsoft.Json;
using StatefulModel;

namespace Candy.Client.Models
{
    /// <summary>
    /// ローカル アプリケーションの構成を管理します。
    /// </summary>
    [DataContract]
    public class ApplicationManager : NotificationObject
    {
        private readonly ObservableSynchronizedCollection<InstalledApplication> _applications = new ObservableSynchronizedCollection<InstalledApplication>();
        private readonly IStateRepository _repository;

        private ApplicationJson _appInfo;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName= "settings")]
        public CandySettings Settings { get; private set; }
        /// <summary>
        /// インストールされているアプリケーションの一覧を取得します。
        /// </summary>
        [JsonProperty(PropertyName="apps")]
        public ObservableSynchronizedCollection<InstalledApplication> Applications
        {
            get { return _applications; }
        }
        /// <summary>
        /// インストール可能なアプリケーションの一覧を取得します。
        /// </summary>
        internal IReadOnlyList<ApplicationMetadata> InstallableApplications
        {
            get { return new ReadOnlyCollection<ApplicationMetadata>(_appInfo.Applications); }
        }
        /// <summary>
        /// 構成情報を永続化するための <see cref="IStateRepository"/> を取得します。
        /// </summary>
        [JsonIgnore]
        public IStateRepository Repository
        {
            get { return _repository; }
        }

        /// <summary>
        /// 使用する <see cref="IStateRepository"/> を指定して、<see cref="ApplicationManager"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="repository">使用する <see cref="IStateRepository"/> 。</param>
        public ApplicationManager(IStateRepository repository)
        {
            _repository = repository;
            Settings = new CandySettings();
        }

        /// <summary>
        /// このアプリケーション構成情報を非同期で永続化します。
        /// </summary>
        /// <returns></returns>
        public Task SaveAsync()
        {
            return _repository.SaveAsync(this);
        }
        /// <summary>
        /// 永続化されているアプリケーション構成情報を非同期で読み込みます。
        /// </summary>
        /// <returns>この操作を待機するための <see cref="Task"/> 。</returns>
        public async Task LoadAsync()
        {
            Applications.Clear();

            await _repository.LoadAsync(this).ConfigureAwait(false);

            if (String.IsNullOrEmpty(Settings.ApplicationInformationServiceUrl))
            {
                Settings.SetDefaultService();
            }

            RegisterMeIfNotRegistered();

            var httpClient = new HttpClient();

            // アプリケーションの日本語名、説明と更新情報の URL はサービスから取得
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync(Settings.ApplicationInformationServiceUrl).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RaiseError(ex.Message + Environment.NewLine + Settings.ApplicationInformationServiceUrl + "へ接続中にエラーが起きました。");
                return;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return;
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            _appInfo = JsonConvert.DeserializeObject<ApplicationJson>(json);

            foreach (var application in Applications)
            {
                await application.InitializeAsync(_appInfo).ConfigureAwait(false);
            }
        }
        private void RegisterMeIfNotRegistered()
        {
            if (Applications.OfType<CandyApplication>().Any()) return;

            var installedPath = Assembly.GetExecutingAssembly().Location;
            var candy = new CandyApplication();
            Applications.Insert(0, candy);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationPath"></param>
        /// <returns></returns>
        public async Task RegisterInstalledApplication(string applicationPath)
        {
            if (Applications.Any(x => String.Equals(x.InstalledPath, applicationPath)))
            {
                RaiseError("既に登録済みのアプリケーションです。");
                return;
            }

            var application = new InstalledApplication(applicationPath);

            await application.InitializeAsync(_appInfo).ConfigureAwait(false);

            if (!application.IsSupported)
            {
                RaiseError("サポートされていないアプリケーションです。");
                return;
            }

            Applications.Add(application);

            await SaveAsync().ConfigureAwait(false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public async Task RemoveApplicationAsync(InstalledApplication app)
        {
            if (!app.CanRemove)
            {
                RaiseError("このアプリケーションを登録解除することはできません。");
            }

            Applications.Remove(app);

            await SaveAsync().ConfigureAwait(false);
        }

        private void RaiseError(string message)
        {
            OnError(new ErrorEventArgs(message));
        }

        protected virtual void OnError(ErrorEventArgs e)
        {
            var handler = Error;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public async Task AutoSuggestApplicationDirectory()
        {
            RegisterMeIfNotRegistered();

            Settings.ApplicationRootDirectoryPath = SuggestApplicationDirectory(Applications);

            await SaveAsync();
        }

        public static string SuggestApplicationDirectory(IReadOnlyList<InstalledApplication> apps)
        {
            if (apps.Count == 1)
            {
                var candy = apps[0];
                var dir = new DirectoryInfo(Path.GetDirectoryName(candy.InstalledPath));
                var candyDirPattern = new Regex("(Candy|キャンディ|きゃんでぃ)");
                var suggested = EnumerateParents(dir).FirstOrDefault(x => !candyDirPattern.IsMatch(x.FullName));

                return suggested != null ? suggested.FullName : null;
            }
            else
            {
                var path = ScanCommonPrefix(apps.Select(x => x.InstalledPath));
                // フォルダ名の先頭の一部がたまたま同じだった場合を考慮して、最後の \ までで切ってしまう
                var suggested = Path.GetDirectoryName(path);

                return suggested;
            }
        }

        private static IEnumerable<DirectoryInfo> EnumerateParents(DirectoryInfo dir)
        {
            while (true)
            {
                yield return dir;
                if (dir.Parent == null) yield break;
                dir = dir.Parent;
            }
        }
        private static string ScanCommonPrefix(IEnumerable<string> args)
        {
            var source = args as string[] ?? args.ToArray();

            return source.First()
                         .Scan("", (x, y) => x + y)
                         .TakeWhile(x => source.All(y => y.StartsWith(x)))
                         .LastOrDefault() ?? "";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                foreach (var app in Applications)
                {
                    app.Dispose();
                }
                Settings.Dispose();
            }
        }

        public async Task<bool> InstallApplicationAsync(string id)
        {
            if (Applications.Any(x => x.Id == id))
            {
                RaiseError("指定されたアプリケーションは既にインストールされています。");
                return false;
            }

            var target = _appInfo.Applications.FirstOrDefault(x => x.Id == id);

            if (target == null)
            {
                RaiseError("指定された識別子は登録されていません。");
                return false;
            }

            var appDir = new DirectoryInfo(Path.Combine(Settings.ApplicationRootDirectoryPath, target.Id));
            var installedPath = Path.Combine(appDir.FullName, target.Id + ".exe");

            if (appDir.Exists)
            {
                // アプリケーションのフォルダが存在していてもいいが、exe があったらアウト
                // （Candy がディレクトリだけ作成して何らかの理由でインストールできずに落ちた場合などを考慮）
                if (File.Exists(installedPath))
                {
                    // TODO: モデルがボタンのテキスト知ってちゃダメでしょ（エラーを識別するコード的なものを渡してビュー側でメッセージを決定した方が良さそう？）
                    RaiseError("指定されたアプリケーションは既にインストールされています。このアプリケーションを Candy で管理するには「インストール済みアプリケーションの追加」を選択してください。");
                    return false;
                }
            }
            else
            {
                // フォルダがなければ作る
                appDir.Create();
            }

            var client = new HttpClient();

            var urlTemplate = target.InstallUrl ?? _appInfo.DefaultInstallUrl;
            var url = urlTemplate.Replace("{appName}", target.Id);

            var response = await client.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    RaiseError("このアプリケーションは Candy でインストールすることができません。");
                    return false;
                }

                RaiseError(response.StatusCode.ToString());
                return false;
            }

            var tempFileName = Path.GetTempFileName();
            try
            {
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var zip = File.OpenWrite(tempFileName))
                {
                    await stream.CopyToAsync(zip).ConfigureAwait(false);
                }

                ZipFile.ExtractToDirectory(tempFileName, appDir.FullName);

                await RegisterInstalledApplication(installedPath).ConfigureAwait(false);

                return true;
            }
            finally
            {
                File.Delete(tempFileName);
            }
        }
    }
}
