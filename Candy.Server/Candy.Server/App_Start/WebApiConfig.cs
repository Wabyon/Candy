using System;
using System.Web.Http;
using Candy.Server.Controllers;

namespace Candy.Server
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API の設定およびサービス

            config.ParameterBindingRules.Add(typeof(Version), d => new VersionParameterBinding(d));

            // Web API ルート
            config.MapHttpAttributeRoutes();
        }
    }
}