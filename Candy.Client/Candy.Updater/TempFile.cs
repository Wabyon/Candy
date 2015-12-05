using System;
using System.IO;

namespace Candy.Updater
{
    /// <summary>
    /// �ꎞ�t�@�C����\���܂��B���̃N���X�̓X���b�h�Z�[�t�ł͂���܂���B
    /// </summary>
    public class TempFile : IDisposable
    {
        private FileInfo _file;

        /// <summary>
        /// �쐬���ꂽ�ꎞ�t�@�C���� <see cref="FileInfo"/> �I�u�W�F�N�g���擾���܂��B
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
        /// ��ӂȖ��O������ 0 �o�C�g�̈ꎞ�t�@�C�����f�B�X�N��ɍ쐬���A<see cref="TempFile"/> �N���X�̐V�����C���X�^���X�����������܂��B
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