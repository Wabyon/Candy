using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Candy.Updater
{
    /// <summary>
    /// アプリケーションを更新する処理を提供します。
    /// </summary>
    public class CandyUpdater
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly UpdateArgs _args;

        /// <summary>
        /// <see cref="CandyUpdater"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="args"></param>
        public CandyUpdater(UpdateArgs args)
        {
            _args = args;
        }
        /// <summary>
        /// 各ステップの間隔を取得または設定します。この値を指定することで、意図的に各ステップの間に遅延時間を作ることができます。
        /// </summary>
        public int StepDelay { get; set; }

        /// <summary>
        /// 進捗を報告しながら、アプリケーションを更新します。
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task UpdateApplicationAsync(IProgress<ProgressStatus> progress = null)
        {
            progress = progress ?? new Progress<ProgressStatus>();

            progress.Report(20, "更新を確認しています。");
            await WaitAsync().ConfigureAwait(false);

            var latest = await GetLatestSummaryAsync().ConfigureAwait(false);

            using (var temp = new TempFile())
            {
                progress.Report(40, "パッケージをダウンロードしています。");
                await WaitAsync().ConfigureAwait(false);

                await DownloadPackageAsync(latest.PackagePath, temp.File.FullName).ConfigureAwait(false);

                var processName = Path.GetFileNameWithoutExtension(_args.ApplicationExecutionPath);

                progress.Report(60, String.Format("プロセス {0} の終了を待機しています。", processName));
                await WaitAsync().ConfigureAwait(false);

                await WaitProcessExitAsync(processName).ConfigureAwait(false);

                progress.Report(80, "ファイルを最新に更新しています。");
                await WaitAsync().ConfigureAwait(false);

                await UpdateFilesAsync(temp.File, _args.ApplicationDirectory).ConfigureAwait(false);
                await RemoveFilesAsync(latest.RemoveFiles, _args.ApplicationDirectory).ConfigureAwait(false);
            }

            if (_args.StartProcess)
            {
                progress.Report(100, "アプリケーションを起動します。");
                await WaitAsync().ConfigureAwait(false);

                Process.Start(_args.ApplicationExecutionPath);
            }
        }
        private Task WaitAsync()
        {
            return Task.Delay(StepDelay);
        }
        protected virtual async Task<UpdateSummary> GetLatestSummaryAsync()
        {
            var json = await _httpClient.GetStringAsync(_args.ServiceUrl).ConfigureAwait(false);
            var summary = JsonConvert.DeserializeObject<UpdateSummary>(json);
            return summary;
        }
        protected virtual async Task DownloadPackageAsync(string zipUrl, string downloadPath)
        {
            using (var sourceStream = await _httpClient.GetStreamAsync(zipUrl).ConfigureAwait(false))
            using (var destStream = File.OpenWrite(downloadPath))
            {
                await sourceStream.CopyToAsync(destStream).ConfigureAwait(false);
            }
        }
        private static async Task WaitProcessExitAsync(string processName)
        {
            while (true)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length <= 0)
                {
                    return;
                }
                await Task.Delay(500).ConfigureAwait(false);
            }
        }
        protected virtual async Task UpdateFilesAsync(FileInfo packageFile, string targetDirectoryPath)
        {
            using (var archive = ZipFile.OpenRead(packageFile.FullName))
            {
                await UpdateFilesAsync(archive, targetDirectoryPath).ConfigureAwait(false);
            }
        }
        private static async Task UpdateFilesAsync(ZipArchive archive, string targetDirectoryPath)
        {
            // Length > 0 でファイル（非フォルダ）にしぼる（で正しいのか？）
            foreach (var entry in archive.Entries.Where(x => x.Length > 0))
            {
                var destPath = Path.Combine(targetDirectoryPath, entry.FullName);

                // 自分自身の場合は確実に書き込み失敗するため無視する（現状は Candy.exe 側で書き換えてもらう想定。将来的には自身で自身をアップデートできるようにしたい＝シャルロッテ方式）
                if (String.Equals(Assembly.GetEntryAssembly().Location, destPath)) continue;

                var destFile = new FileInfo(destPath);
                var destDir = new DirectoryInfo(Path.GetDirectoryName(destPath));

                // 上書き先のディレクトリが存在しない場合は作成しておかないと File.OpenWrite で例外になる
                if (!destDir.Exists)
                {
                    destDir.Create();
                }

                if (destFile.Exists)
                {
                    if (destFile.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        destFile.Attributes = destFile.Attributes & ~FileAttributes.ReadOnly;
                    }
                }

                using (var source = entry.Open())
                using (var dest = File.OpenWrite(destPath))
                {
                    dest.SetLength(0);
                    await source.CopyToAsync(dest).ConfigureAwait(false);
                }
            }
        }
        private static Task RemoveFilesAsync(IEnumerable<string> removeFiles, string applicationDirectory)
        {
            foreach (var file in removeFiles)
            {
                // サブディレクトリ内のワイルドカード指定はとりあえず非対応で…
                if (file.StartsWith("*", StringComparison.Ordinal))
                {
                    foreach (var f in Directory.EnumerateFiles(applicationDirectory, file))
                    {
                        DeleteFile(f);
                    }
                }
                else
                {
                    DeleteFile(Path.Combine(applicationDirectory, file));
                }
            }
            return Task.FromResult((object)null);
        }
        private static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
                File.Delete(path);
            }
        }

        /// <summary>
        /// 更新の情報を表します。
        /// </summary>
        [DataContract]
        protected class UpdateSummary
        {
            private readonly HashSet<string> _allowedUserIds = new HashSet<string>();
            private readonly ICollection<string> _removeFiles = new List<string>();

            /// <summary>
            /// この更新のバージョンを取得または設定します。
            /// </summary>
            [DataMember(Name = "version")]
            public Version Version { get; set; }
            /// <summary>
            /// この更新がリリースされた日付を取得または設定します。
            /// </summary>
            [DataMember(Name = "date")]
            public DateTime PublishDate { get; set; }
            /// <summary>
            /// この更新がサポートするバージョン（この更新を適用可能な最低バージョン）を取得または設定します。
            /// </summary>
            [DataMember(Name = "for")]
            public Version SupportedVersion { get; set; }
            /// <summary>
            /// 削除するファイルの一覧を取得します。
            /// </summary>
            [DataMember(Name = "remove")]
            public ICollection<string> RemoveFiles
            {
                get { return _removeFiles; }
            }
            /// <summary>
            /// この更新の適用を許可する対象のユーザー一覧を取得します。
            /// </summary>
            [DataMember(Name = "allow")]
            public ICollection<string> AllowedUserIds
            {
                get { return _allowedUserIds; }
            }
            /// <summary>
            /// この更新のパッケージ ファイルのパスを取得または設定します。
            /// </summary>
            [DataMember(Name = "package")]
            public string PackagePath { get; set; }
            /// <summary>
            /// この更新の更新内容を取得または設定します。
            /// </summary>
            [DataMember(Name = "releaseNote")]
            public string ReleaseNote { get; set; }
        }

    }
}