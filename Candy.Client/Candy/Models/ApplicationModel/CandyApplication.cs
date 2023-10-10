using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace Candy.Client.Models
{
    /// <summary>
    /// アプリケーション構成管理ツールである「Candy」自身を表します。
    /// </summary>
    public sealed class CandyApplication : InstalledApplication
    {
        /// <summary>
        /// 永久に完了しない <see cref="Task"/> です。
        /// </summary>
        private static readonly Task<object> _never = new TaskCompletionSource<object>().Task;

        /// <summary>
        /// このアプリケーションを一覧から削除可能かどうかを示す値を取得します。
        /// </summary>
        public override bool CanRemove
        {
            get { return false; }
        }
        /// <summary>
        /// このインストールを実行可能かどうかを示す値を取得または設定します。
        /// </summary>
        public override bool CanExecute
        {
            get { return false; }
            protected set { /* noop */ }
        }
        /// <summary>
        /// このアプリケーションを最新版に更新する際にユーザーに提供するためのメッセージを取得します。
        /// </summary>
        public override string UpdateConfirmationMessage
        {
            get { return "Candy を最新版に更新します。アプリケーションは再起動されます。よろしいですか？"; }
        }

        /// <summary>
        /// <see cref="CandyApplication"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public CandyApplication()
            : base(Assembly.GetExecutingAssembly().Location)
        {
        }

        /// <summary>
        /// このアプリケーションを適用可能な最新版に更新します。
        /// </summary>
        /// <returns>更新の完了を通知するタスク。</returns>
        public override async Task UpdateAsync()
        {
            // Candy.Updater.exe だけは Candy.exe 側で書き換える
            var client = new HttpClient();

            var updateServiceUrl = GetUpdateServiceUrl();
            var json = await client.GetStringAsync(updateServiceUrl).ConfigureAwait(false);
            var latest = JsonConvert.DeserializeObject<UpdateSummary>(json);

            using (var stream = await client.GetStreamAsync(latest.PackagePath).ConfigureAwait(false))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                // Updater 関連は本体で書き換える(dllの依存が共通している場合、上書きできないため)
                foreach (var entry in archive.Entries
                    .Where(r => r.FullName.StartsWith(@"Updater/", StringComparison.OrdinalIgnoreCase)))
                {
                    // ディレクトリが存在しない場合はファイル作成時に例外となる
                    Directory.CreateDirectory(Path.GetDirectoryName(entry.FullName));

                    using (var updater = entry.Open())
                    using (var dest = new FileStream(entry.FullName, FileMode.OpenOrCreate))
                    {
                        dest.SetLength(0);
                        await updater.CopyToAsync(dest).ConfigureAwait(false);
                    }
                }
            }

            var arguments = String.Format(@"-n ""{0}"" -p ""{1}"" -u ""{2}"" -s",
                                          DisplayName.Replace("\"", "\\\""),
                                          InstalledPath,
                                          updateServiceUrl);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "Updater/Candy.Updater.exe",
                    Arguments = arguments,
                },
            };
            process.Start();

            // 自身を終了させる（Candy.Updater.exe は Candy.exe が終了するまで待機してくれる）
            Application.Current.Shutdown();

            // 完了しないタスクで呼出元をアプリケーション終了まで待機させ続ける
            await _never.ConfigureAwait(false);
        }
    }
}
