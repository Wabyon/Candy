using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Candy.Client.Models;
using Candy.Client.Utilities;
using Candy.Client.ViewModels;
using Candy.Client.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;

namespace Candy.Client
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            // System.Version をシリアライズ-デシリアライズ可能にする
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters =
                {
                    new VersionConverter()
                }
            };
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Error(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // C:\Users\<User>\AppData\Local\Planet\Candy に保存する。
            // 将来的に、この設定ファイルに Candy.Updater.exe のパスを持たせることで、
            // 各アプリケーションからも更新処理が呼べるようにするため
            var appSettings = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDirectory = Path.Combine(appSettings, "Planet", "Candy");
            var repository = new JsonFileStateRepository(Path.Combine(settingsDirectory, "settings.json"));

            var model = new ApplicationManager(repository);
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(model)
            };

            window.ShowDialog();
        }
    }
}