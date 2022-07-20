using Candy.Client.Models;
using Livet;
using System.Collections.Generic;

namespace Candy.Client.ViewModels
{
    public class InstallableApplicationsViewModel : ViewModel
    {
        private readonly IReadOnlyList<ApplicationMetadata> _installableApplications;

        public IReadOnlyList<ApplicationMetadata> InstallableApplications
        {
            get { return _installableApplications; }
        }

        public InstallableApplicationsViewModel(IReadOnlyList<ApplicationMetadata> installableApplications)
        {
            _installableApplications = installableApplications;
        }
    }
}
