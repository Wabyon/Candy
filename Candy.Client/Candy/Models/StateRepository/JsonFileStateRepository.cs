using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Candy.Client.Utilities;
using Newtonsoft.Json;

namespace Candy.Client.Models
{
    /// <summary>
    /// JSON 形式でファイルに状態を記録する <see cref="IStateRepository"/> 実装を提供します。
    /// </summary>
    public class JsonFileStateRepository : IStateRepository
    {
        private static readonly AsyncLock _fileLock = new AsyncLock();

        private readonly string _settingFilePath;

        static JsonFileStateRepository()
        {
            Mapper.CreateMap<ApplicationManager, ApplicationManager>()
            // TODO: これ書いておかないと Applications の詰め替えがされない…
                  .ForMember(x => x.Applications, c => c.MapFrom(x => x.Applications));
        }

        /// <summary>
        /// <see cref="JsonFileStateRepository"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="filePath"></param>
        public JsonFileStateRepository(string filePath)
        {
            _settingFilePath = filePath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task SaveAsync(ApplicationManager obj)
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);

            using (await _fileLock.LockAsync())
            {
                var dir = new DirectoryInfo(Path.GetDirectoryName(_settingFilePath));
                if (!dir.Exists)
                {
                    dir.Create();
                }

                using (var stream = File.OpenWrite(_settingFilePath))
                using (var writer = new StreamWriter(stream))
                {
                    stream.SetLength(0);
                    await writer.WriteAsync(json).ConfigureAwait(false);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task LoadAsync(ApplicationManager obj)
        {
            var jsonText = await ReadJsonAsync().ConfigureAwait(false);
            if (jsonText == null) return;

            var json = JsonConvert.DeserializeObject<ApplicationManager>(jsonText, new InstalledApplicationConverter());

            Mapper.Map(json, obj);
        }

        private class InstalledApplicationConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var obj = (dynamic)serializer.Deserialize(reader);

                if (obj.path == null) throw new NotSupportedException();

                var installedPath = (string)obj.path;

                return IsCandy(installedPath)
                    ? new CandyApplication()
                    : new InstalledApplication(installedPath);
            }
            public override bool CanConvert(Type objectType)
            {
                return typeof(InstalledApplication).IsAssignableFrom(objectType);
            }
            private static bool IsCandy(string installedPath)
            {
                var fileName = Path.GetFileNameWithoutExtension(installedPath);
                return String.Equals(fileName, "Candy", StringComparison.OrdinalIgnoreCase);
            }
        }

        private async Task<string> ReadJsonAsync()
        {
            using (await _fileLock.LockAsync())
            {
                if (!File.Exists(_settingFilePath)) return null;

                using (var stream = File.OpenRead(_settingFilePath))
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }
    }
}