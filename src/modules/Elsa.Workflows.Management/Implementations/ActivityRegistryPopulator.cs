using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

/// <summary>
/// Populates the <see cref="IActivityRegistry"/> with descriptors provided by <see cref="IActivityProvider"/>s. 
/// </summary>
public class ActivityRegistryPopulator : IActivityRegistryPopulator
{
    private readonly IEnumerable<IActivityProvider> _providers;
    private readonly IActivityRegistry _registry;

    public ActivityRegistryPopulator(IEnumerable<IActivityProvider> providers, IActivityRegistry registry)
    {
        _providers = providers;
        _registry = registry;
    }

    public async Task PopulateRegistryAsync(CancellationToken cancellationToken)
    {
        _registry.Clear();

        foreach (var provider in _providers)
            await PopulateRegistryAsync(provider, cancellationToken);
    }

    public async Task PopulateRegistryAsync(Type providerType, CancellationToken cancellationToken = default)
    {
        _registry.ClearProvider(providerType);
        var provider = _providers.First(x => x.GetType() == providerType);
        await PopulateRegistryAsync(provider, cancellationToken);
    }

    private async Task PopulateRegistryAsync(IActivityProvider provider, CancellationToken cancellationToken = default)
    {
        var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
        _registry.AddMany(provider.GetType(), descriptors);
    }
}