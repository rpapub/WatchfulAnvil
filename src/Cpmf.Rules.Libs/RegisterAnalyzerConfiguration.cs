using Cpmf.Rules.Project;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;

namespace Cpmf.Rules.Libs
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService api)
        {
            new ProjectOutputTypeRule().Initialize(api);
            new LibraryNameLengthRule().Initialize(api);
            new LibraryReservedNameRule().Initialize(api);
            new LibraryDescriptionLengthRule().Initialize(api);
        }
    }
}
