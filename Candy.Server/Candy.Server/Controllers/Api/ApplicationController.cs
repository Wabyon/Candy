using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Http;
using Candy.Server.Models;
using Newtonsoft.Json;

namespace Candy.Server.Controllers.Api
{
    [RoutePrefix("api/Apps")]
    public class ApplicationController : ApiController
    {
        private static readonly string ApplicationMetadataFile = ConfigurationManager.AppSettings["Candy:ApplicationMetadataFile"];
        private static readonly string PhysicalPackageRoot = ConfigurationManager.AppSettings["Candy:PhysicalPackageRoot"];
        private static readonly string PackageRoot = ConfigurationManager.AppSettings["Candy:PackageRoot"];
        private static readonly string UpdateDefinitionFileName = ConfigurationManager.AppSettings["Candy:UpdateDefinitionFileName"];

        [Route(@"")]
        [HttpGet]
        public IHttpActionResult Index()
        {
            var jsonText = File.ReadAllText(ApplicationMetadataFile);
            var json = JsonConvert.DeserializeObject(jsonText, JsonConvert.DefaultSettings());
            
            return Json(json, JsonConvert.DefaultSettings());
        }

        /// <summary>
        /// 指定されたツールの指定されたバージョンに対する最新のアップデート パッケージの情報を返却します。
        /// </summary>
        /// <param name="app"></param>
        /// <param name="for"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        [Route(@"Update/Latest")]
        [HttpGet]
        public IHttpActionResult Latest(string app, Version @for, [FromUri(Name = "user")] string user)
        {
            var appName = app;
            var currentVersion = @for;

            // user は適用可能なバージョンを絞るために使用するが、ベータ版を一般公開しないなどが目的のため認証まではしない（詐称を許可）

            // トラバーサル防止
            if (appName.Contains(Path.DirectorySeparatorChar) ||
                appName.Contains(Path.AltDirectorySeparatorChar))
            {
                return NotFound();
            }

            var appDir = Path.Combine(PhysicalPackageRoot, appName);
            var jsonPath = Path.Combine(appDir, UpdateDefinitionFileName);

            if (!File.Exists(jsonPath))
            {
                // 更新が存在しない間は null を返す（application.json に掲載したアプリでも、更新が用意されるまでは update.json を用意しなくていいようにするため）
                return Json((object)null);
            }

            var json = File.ReadAllText(jsonPath);
            var updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(json);

            var summaries = updateInfo.UpdateSummaries;

            // 適用可能な最新バージョンを取得
            var latest = summaries.Where(x => x.AllowedUserIds.Contains("Any") || x.AllowedUserIds.Contains(user))
                                  .Where(x => x.SupportedVersion <= currentVersion && currentVersion < x.Version)
                                  .OrderByDescending(x => x.Version)
                                  .FirstOrDefault();

            if (latest != null)
            {
                latest.PackagePath = PackageRoot + appName + "/update/" + latest.PackagePath;
            }

            return Json(latest, JsonConvert.DefaultSettings());
        }

    }
}
