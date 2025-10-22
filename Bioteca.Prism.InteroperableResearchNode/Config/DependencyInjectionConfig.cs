using Bioteca.Prism.CrossCutting;

namespace Bioteca.Prism.InteroperableResearchNode.Config
{
    public static class DependencyInjectionConfig
    {
        public static void AddDependencyInjectionConfiguration(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            NativeInjectorBootStrapper.RegisterAllDependencies(services);

        }

    }
}
