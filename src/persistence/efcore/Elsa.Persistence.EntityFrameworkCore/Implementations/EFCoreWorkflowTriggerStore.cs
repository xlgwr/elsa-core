using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Implementations;

public class EFCoreWorkflowTriggerStore : IWorkflowTriggerStore
{
    private readonly IStore<WorkflowTrigger> _store;

    public EFCoreWorkflowTriggerStore(IStore<WorkflowTrigger> store)
    {
        _store = store;
    }

    public async Task SaveAsync(WorkflowTrigger record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<WorkflowTrigger> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    public async Task<IEnumerable<WorkflowTrigger>> FindManyByNameAsync(string name, string? hash, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query =>
        {
            query = query.Where(x => x.Name == name);

            if (hash != null)
                query = query.Where(x => x.Hash == hash);

            return query;
        }, cancellationToken);

    public async Task<IEnumerable<WorkflowTrigger>> FindManyByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => query.Where(x => x.WorkflowDefinitionId == workflowDefinitionId), cancellationToken);

    public async Task ReplaceAsync(IEnumerable<WorkflowTrigger> removed, IEnumerable<WorkflowTrigger> added, CancellationToken cancellationToken = default)
    {
        await _store.DeleteManyAsync(removed, cancellationToken);
        await _store.SaveManyAsync(added, cancellationToken);
    }

    public async Task DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        await _store.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }
}