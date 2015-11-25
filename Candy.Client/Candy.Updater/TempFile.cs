using System;
using System.IO;

namespace Candy.Updater
{
    /// <summary>
    /// 一時ファイルを表します。このクラスはスレッドセーフではありません。
    /// </summary>
    public class TempFile : IDisposable
    {
        private FileInfo _file;

        /// <summary>
        /// 作成された一時ファイルの <see cref="FileInfo"/> オブジェクトを取得します。
        /// </summary>
        public FileInfo File
        {
            get
            {
                if (_file == null) throw new ObjectDisposedException(typeof(TempFile).Name);
                return _file;
            }
        }

        /// <summary>
        /// 一意な名前を持つ 0 バイトの一時ファイルをディスク上に作成し、<see cref="TempFile"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public TempFile()
        {
            _file = new FileInfo(Path.GetTempFileName());
        }

        void IDisposable.Dispose()
        {
            if (_file == null) return;

            _file.Attributes = _file.Attributes & (~FileAttributes.ReadOnly);
            _file.Delete();

            _file = null;
        }
    }
}