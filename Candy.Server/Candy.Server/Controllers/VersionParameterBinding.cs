using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;
using System.Web.Http.ValueProviders.Providers;
using Candy.Server.Utilities;

namespace Candy.Server.Controllers
{
    /// <summary>
    /// <see cref="Version"/> クラスにパラメーターをバインドします。
    /// </summary>
    public class VersionParameterBinding : HttpParameterBinding
    {
        private static readonly Task _completed = Task.FromResult(default(object));

        /// <summary>
        /// <see cref="VersionParameterBinding"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="p">An <see cref="HttpParameterDescriptor"/> that describes the parameters.</param>
        public VersionParameterBinding(HttpParameterDescriptor p)
            : base(p)
        {
        }

        /// <summary>
        /// Asynchronously executes the binding for the given request.
        /// </summary>
        /// <returns>
        /// A task object representing the asynchronous operation.
        /// </returns>
        /// <param name="metadataProvider">Metadata provider to use for validation.</param>
        /// <param name="actionContext">The action context for the binding. The action context contains the parameter dictionary that will get populated with the parameter.</param>
        /// <param name="cancellationToken">Cancellation token for cancelling the binding operation.</param>
        public override Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            // TODO: 本当はどうやってやるべきなのかがわからない…
            var queryString = new QueryStringValueProvider(actionContext, CultureInfo.CurrentCulture);
            var parameterName = Descriptor.ParameterName;
            var value = queryString.GetValue(parameterName).Maybe(x => x.AttemptedValue) ??
                        actionContext.ControllerContext.RouteData.Values[parameterName] as string;

            Version version;
            if (Version.TryParse(value, out version))
            {
                SetValue(actionContext, version);
            }

            return _completed;
        }
    }
}