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
    /// �A�v���P�[�V�������X�V���鏈����񋟂��܂��B
    /// </summary>
    public class CandyUpdater
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly UpdateArgs _args;

        /// <summary>
        /// <see cref="CandyUpdater"/> �N���X�̐V�����C���X�^���X�����������܂��B
        /// </summary>
        /// <param name="args"></param>
        public CandyUpdater(UpdateArgs args)
        {
            _args = args;
        }
        /// <summary>
        /// �e�X�e�b�v�̊Ԋu���擾�܂��͐ݒ肵�܂��B���̒l���w�肷�邱�ƂŁA�Ӑ}�I�Ɋe�X�e�b�v�̊Ԃɒx�����Ԃ���邱�Ƃ��ł��܂��B
        /// </summary>
        public int StepDelay { get; set; }

        /// <summary>
        /// �i����񍐂��Ȃ���A�A�v���P�[�V�������X�V���܂��B
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task UpdateApplicationAsync(IProgress<ProgressStatus> progress = null)
        {
            progress = progress ?? new Progress<ProgressStatus>();

            progress.Report(20, "�X�V���m�F���Ă��܂��B");
            await WaitAsync().ConfigureAwait(false);

            var latest = await GetLatestSummaryAsync().ConfigureAwait(false);

            using (var temp = new TempFile())
            {
                progress.Report(40, "�p�b�P�[�W���_�E�����[�h���Ă��܂��B");
                await WaitAsync().ConfigureAwait(false);

                await DownloadPackageAsync(latest.PackagePath, temp.File.FullName).ConfigureAwait(false);

                var processName = Path.GetFileNameWithoutExtension(_args.ApplicationExecutionPath);

                progress.Report(60, String.Format("�v���Z�X {0} �̏I����ҋ@���Ă��܂��B", processName));
                await WaitAsync().ConfigureAwait(false);

                await WaitProcessExitAsync(processName).ConfigureAwait(false);

                progress.Report(80, "�t�@�C�����ŐV�ɍX�V���Ă��܂��B");
                await WaitAsync().ConfigureAwait(false);

                await UpdateFilesAsync(temp.File, _args.ApplicationDirectory).ConfigureAwait(false);
                await RemoveFilesAsync(latest.RemoveFiles, _args.ApplicationDirectory).ConfigureAwait(false);
            }

            if (_args.StartProcess)
            {
                progress.Report(100, "�A�v���P�[�V�������N�����܂��B");
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
            // Length > 0 �Ńt�@�C���i��t�H���_�j�ɂ��ڂ�i�Ő������̂��H�j
            foreach (var entry in archive.Entries.Where(x => x.Length > 0))
            {
                var destPath = Path.Combine(targetDirectoryPath, entry.FullName);

                // �������g�̏ꍇ�͊m���ɏ������ݎ��s���邽�ߖ�������i����� Candy.exe ���ŏ��������Ă��炤�z��B�����I�ɂ͎��g�Ŏ��g���A�b�v�f�[�g�ł���悤�ɂ��������V�������b�e�����j
                if (String.Equals(Assembly.GetEntryAssembly().Location, destPath)) continue;

                var destFile = new FileInfo(destPath);
                var destDir = new DirectoryInfo(Path.GetDirectoryName(destPath));

                // �㏑����̃f�B���N�g�������݂��Ȃ��ꍇ�͍쐬���Ă����Ȃ��� File.OpenWrite �ŗ�O�ɂȂ�
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
                // �T�u�f�B���N�g�����̃��C���h�J�[�h�w��͂Ƃ肠������Ή��Łc
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
        /// �X�V�̏���\���܂��B
        /// </summary>
        [DataContract]
        protected class UpdateSummary
        {
            private readonly HashSet<string> _allowedUserIds = new HashSet<string>();
            private readonly ICollection<string> _removeFiles = new List<string>();

            /// <summary>
            /// ���̍X�V�̃o�[�W�������擾�܂��͐ݒ肵�܂��B
            /// </summary>
            [DataMember(Name = "version")]
            public Version Version { get; set; }
            /// <summary>
            /// ���̍X�V�������[�X���ꂽ���t���擾�܂��͐ݒ肵�܂��B
            /// </summary>
            [DataMember(Name = "date")]
            public DateTime PublishDate { get; set; }
            /// <summary>
            /// ���̍X�V���T�|�[�g����o�[�W�����i���̍X�V��K�p�\�ȍŒ�o�[�W�����j���擾�܂��͐ݒ肵�܂��B
            /// </summary>
            [DataMember(Name = "for")]
            public Version SupportedVersion { get; set; }
            /// <summary>
            /// �폜����t�@�C���̈ꗗ���擾���܂��B
            /// </summary>
            [DataMember(Name = "remove")]
            public ICollection<string> RemoveFiles
            {
                get { return _removeFiles; }
            }
            /// <summary>
            /// ���̍X�V�̓K�p��������Ώۂ̃��[�U�[�ꗗ���擾���܂��B
            /// </summary>
            [DataMember(Name = "allow")]
            public ICollection<string> AllowedUserIds
            {
                get { return _allowedUserIds; }
            }
            /// <summary>
            /// ���̍X�V�̃p�b�P�[�W �t�@�C���̃p�X���擾�܂��͐ݒ肵�܂��B
            /// </summary>
            [DataMember(Name = "package")]
            public string PackagePath { get; set; }
            /// <summary>
            /// ���̍X�V�̍X�V���e���擾�܂��͐ݒ肵�܂��B
            /// </summary>
            [DataMember(Name = "releaseNote")]
            public string ReleaseNote { get; set; }
        }

    }
}