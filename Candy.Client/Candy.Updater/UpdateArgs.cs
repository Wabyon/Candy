using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Candy.Updater
{
    /// <summary>
    /// プログラムの更新に必要な情報を表します。
    /// </summary>
    public class UpdateArgs
    {
        private static readonly IReadOnlyDictionary<string, PropertyInfo> _propertyDic;
        private string _applicationDirectory;

        /// <summary>
        /// アプリケーションの表示名を取得します。
        /// </summary>
        [CommandLineArgs("n", "name")]
        public string ApplicationName { get; private set; }
        /// <summary>
        /// アプリケーションを実行するアセンブリのパスを取得します。
        /// </summary>
        [CommandLineArgs("p", "path")]
        public string ApplicationExecutionPath { get; private set; }
        /// <summary>
        /// アプリケーションの配置されているディレクトリのローカル パスを取得します。
        /// </summary>
        [CommandLineArgs("d", "dir")]
        public string ApplicationDirectory
        {
            get
            {
                return _applicationDirectory ??
                       (ApplicationExecutionPath != null ? Path.GetDirectoryName(ApplicationExecutionPath) : null);
            }
            private set
            {
                _applicationDirectory = value;
            }
        }
        /// <summary>
        /// アプリケーションの更新情報を取得するための HTTP サービスの URL 文字列を取得します。
        /// </summary>
        [CommandLineArgs("u", "url")]
        public string ServiceUrl { get; private set; }

        public int StepDelay { get; private set; }
        [CommandLineArgs("s", "start")]
        public bool StartProcess { get; private set; }

        static UpdateArgs()
        {
            var properties = typeof(UpdateArgs).GetProperties();
            // n -> ApplicationName, name -> ApplicationName, p -> ApplicationPath ... のような辞書に変換
            _propertyDic = properties.SelectMany(x => x.GetCustomAttribute<CommandLineArgsAttribute>().Maybe(_ => _.Keys) ?? Enumerable.Empty<string>(),
                                                 (property, key) => new { key, property, })
                                     .ToDictionary(x => x.key, x => x.property);
        }

        /// <summary>
        /// コマンドライン引数から <see cref="UpdateArgs"/> を生成します。
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static UpdateArgs Parse(string[] args)
        {
            /*
             * ToolUpdater.exe /n "シャルロッテ" /p "D:\Tools\Charlotte" /u "http://192.168.47.74:8020/Charlotte/Update?v=1.12"
             * のようなコマンドライン引数をパース
             */

            var obj = new UpdateArgs
            {
                StepDelay = 400,
            };

            PropertyInfo property = null;
            foreach (var s in args.Where(x => x.Length > 0))
            {
                // スラッシュまたはハイフンで始まる場合をキーとみなす
                var c = s[0];
                if (c == '/' || c == '-')
                {
                    // キーに一致するプロパティを取得
                    _propertyDic.TryGetValue(s.Substring(1), out property);
                    // プロパティが Boolean の場合は指定された時点で true をセット
                    if (property.PropertyType == typeof(bool))
                    {
                        property.SetValue(obj, true);
                        property = null;
                    }
                    continue;
                }

                // 直前のコマンドライン引数がプロパティのキーだった場合は、今回の文字列を値とみなす
                if (property != null)
                {
                    property.SetValue(obj, s);
                }

                property = null;
            }

            return obj;
        }
    }
}