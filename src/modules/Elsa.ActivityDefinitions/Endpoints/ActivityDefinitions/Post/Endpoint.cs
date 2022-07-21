using System.Text.Json;
using Elsa.ActivityDefinitions.Services;
using Elsa.Api.Common;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Post;

public class Post : ProtectedEndpoint<Request, Response>
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly IActivityDefinitionPublisher _activityDefinitionPublisher;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    public Post(
        SerializerOptionsProvider serializerOptionsProvider,
        IActivityDefinitionPublisher activityDefinitionPublisher,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _activityDefinitionPublisher = activityDefinitionPublisher;
        _variableDefinitionMapper = variableDefinitionMapper;
    }

    public override void Configure()
    {
        Post("/activity-definitions");
        ConfigureSecurity(List.SecurityConstants.Permissions, List.SecurityConstants.Policies, List.SecurityConstants.Roles);
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var definitionId = request.DefinitionId;

        // Get a workflow draft version.
        var draft = !string.IsNullOrWhiteSpace(definitionId)
            ? await _activityDefinitionPublisher.GetDraftAsync(definitionId, cancellationToken)
            : default;

        var isNew = draft == null;

        // Create a new workflow in case no existing definition was found.
        if (isNew)
        {
            draft = _activityDefinitionPublisher.New();

            if (!string.IsNullOrWhiteSpace(definitionId))
                draft.DefinitionId = definitionId;
        }

        // Update the draft with the received model.
        var root = request.Root ?? new Flowchart();
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var data = JsonSerializer.Serialize(root, serializerOptions);
        var variables = _variableDefinitionMapper.Map(request.Variables).ToList();

        draft!.Data = data;
        draft.TypeName = request.TypeName;
        draft.DisplayName = request.DisplayName?.Trim();
        draft.Category = request.Category?.Trim();
        draft.Description = request.Description?.Trim();
        draft.Metadata = request.Metadata ?? new Dictionary<string, object>();
        draft.Variables = variables;
        draft.ApplicationProperties = request.ApplicationProperties ?? new Dictionary<string, object>();
        draft = request.Publish ? await _activityDefinitionPublisher.PublishAsync(draft, cancellationToken) : await _activityDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);

        // Create a response DTO.
        var response = new Response(
            draft.Id,
            draft.DefinitionId,
            draft.TypeName,
            draft.DisplayName,
            draft.Category,
            draft.Description,
            draft.CreatedAt,
            draft.Version,
            request.Variables ?? new List<VariableDefinition>(),
            draft.Metadata,
            draft.ApplicationProperties,
            draft.IsLatest,
            draft.IsPublished,
            root);

        if (isNew)
            await SendCreatedAtAsync<Get.Get>(new { Id = draft.Id }, response, default, default, false, cancellationToken);
        else
            await SendOkAsync(response, cancellationToken);

        return response;
    }
}