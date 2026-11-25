using System.Collections.Concurrent;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using C7N.Engine.Registry;
using C7N.Engine.Workflows;
using C7N.Engine.Execution;
using C7N.Engine.Registry.Definitions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.UseCors();

var workflows = new ConcurrentDictionary<string, Workflow>();
var executions = new ConcurrentDictionary<string, WorkflowExecution>();

app.MapPost("/workflows", (Workflow workflow) =>
{
    if(!workflows.TryAdd(workflow.Id, workflow))
        return Results.Conflict($"Workflow {workflow.Id} already exists");
    return Results.Created($"/workflows/{workflow.Id}", workflow);
})
.Produces<Workflow>(201).Produces(409)
.WithSummary("Create a new workflow")
.WithDescription("""
                 Workflow engine rules:
                 - Every workflow must include a `ManualTriggerNode` as its entry point.
                 - `Connection.sourceOutputId` must match one of the source node's `outputs` (`"main"`, `"true"`, `"false"`). `targetInputId` is `"main"`.
                 - Node parameters (URLs, headers, queryParameters, body, SetNode values, IfNode leftValue/rightValue) support template placeholders using `<< path.to.field >>` (e.g. `<< body.userId >>`) resolved against merged incoming node outputs.
                 """);

app.MapGet("/workflows", () =>
        Results.Ok(workflows.Values.Select(w => new WorkflowSummary(w.Id, w.Name)))
        )
        .Produces<IEnumerable<WorkflowSummary>>(200);

app.MapDelete("/workflows/{id}", (string id) =>
{
    if(workflows.TryRemove(id, out _)) return Results.NoContent();
    return Results.NotFound();
})
.Produces(204).Produces(404);

app.MapGet("/workflows/{id}", (string id) =>
{
    if(!workflows.TryGetValue(id, out var workflow)) return  Results.NotFound();
    Console.WriteLine(workflow);
    return Results.Ok(workflow);
})
.Produces<Workflow>(200).Produces(404);

app.MapGet("/executions/{id}", (string id) =>
{
    if(!executions.TryGetValue(id, out var execution)) return Results.NotFound();
    return Results.Ok(execution.GetWorkflowSnapshot());
})
.Produces<WorkflowExecutionSnapshot>(200).Produces(404);

app.MapGet("/executions", () => Results.Ok(executions.Keys))
    .Produces<IEnumerable<string>>(200);

app.MapDelete("/executions/{id}", (string id) =>
{
    if(executions.TryRemove(id, out _)) return Results.NoContent();
    return Results.NotFound();
})
.Produces(204).Produces(404);

app.MapPost("/workflows/{id}/execute", (string id) =>
{
    if(!workflows.TryGetValue(id, out var workflow)) return Results.NotFound();
    WorkflowExecution executor = new(workflow);
    executions[executor.Id] = executor;
    _ = Task.Run(() => executor.Execute());
    return Results.Accepted($"/executions/{executor.Id}", new ExecuteWorkflowResponse(executor.Id));
})
    .Produces<ExecuteWorkflowResponse>(202).Produces(404)
    .WithSummary("Start an asynchronous workflow execution")
    .WithDescription("""
                     Starts execution in the background and immediately returns `202 Accepted` with `{ "executionId": "string" }`.
                     Use the returned `executionId` to subscribe to `GET /executions/{id}/events` (SSE) and/or fetch `GET /executions/{id}` for the full snapshot.
                     """);

app.MapPut("/workflows/{id}", (string id, Workflow workflow) =>
{
    if(workflow.Id != id) return Results.BadRequest();
    if(!workflows.ContainsKey(id)) return   Results.NotFound();
    workflows[id] = workflow;
    return Results.Ok(workflow);
})
.Produces<Workflow>(200).Produces(400).Produces(404)
.WithSummary("Update an existing workflow")
.WithDescription("Body workflow must match the {id} path parameter (returns 400 otherwise).");

app.MapGet("/nodes", () =>
    {
        NodeRegistry registry = new();
        return Results.Ok(registry.GetAvailableNodes());
    })
    .Produces<List<NodeDefinition>>(200)
    .WithDescription("""
                     Returns metadata for building the visual node palette, inspector forms, and output handles:
                     - `outputs`: output handle IDs to render on the node (e.g., `["main"]` or `["true", "false"]` for `IfNode`), used as `Connection.sourceOutputId`.
                     - When `parameters[].type` is `"Options"`, populate the select dropdown from `parameters[].options`.
                     """);

app.MapGet("/executions/{id}/events", (string id, CancellationToken ct) =>
{
    if (!executions.TryGetValue(id, out var execution)) return Results.NotFound();

    async IAsyncEnumerable<SseItem<object?>> StreamEvents(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (subscriptionId, reader) = execution.Subscribe();
        try
        {
            await foreach (var evt in reader.ReadAllAsync(cancellationToken))
            {
                yield return new SseItem<object?>(evt.Payload, eventType: evt.EventType.ToString());
            }
        }
        finally
        {
            execution.Unsubscribe(subscriptionId);
        }
    }

    return Results.ServerSentEvents(StreamEvents(ct));
})
.Produces<ExecutionEvent>(200, contentType: "text/event-stream").Produces(404)
.WithDescription("""
                 Server-Sent Events (EventSource) stream for live execution updates.
                 Each SSE message uses `ExecutionEventType` as the `event` name (listen with `addEventListener`) and sends the unwrapped payload directly in `data`:
                 - `NodeRunning`, `NodeCompleted`, `NodeFailed` -> `NodeEventPayload`
                 - `WorkflowRunning`, `WorkflowCompleted`, `WorkflowFailed` -> `WorkflowEventPayload`
                 Important for clients:
                 - Fast nodes may complete before SSE connects; open the EventSource and also fetch `GET /executions/{id}` to sync the full snapshot.
                 - Call `eventSource.close()` when receiving `WorkflowCompleted` or `WorkflowFailed` so the browser does not auto-reconnect.
                 """);

app.Run();

record ExecuteWorkflowResponse(string ExecutionId);
record WorkflowSummary(string Id, string Name);