using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Candy.Updater
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters =
                {
                    new VersionConverter()
                }
            };
            var commandLineArgs = UpdateArgs.Parse(args);
            Application.Run(new MainForm(commandLineArgs));
        }
    }
}
