using System.Collections.Concurrent;
using C7N.Engine.Registry;
using C7N.Engine.Workflows;
using C7N.Engine.Execution;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

var workflows = new ConcurrentDictionary<string, Workflow>();
var executions = new ConcurrentDictionary<string, WorkflowExecution>();

app.MapPost("/workflows", (Workflow workflow) =>
{
    if(!workflows.TryAdd(workflow.Id, workflow))
        return Results.Conflict($"Workflow {workflow.Id} already exists");
    return Results.Created($"/workflows/{workflow.Id}", workflow);
});

app.MapGet("/workflows/{id}", (string id) =>
{
    if(!workflows.TryGetValue(id, out var workflow)) return  Results.NotFound();
    Console.WriteLine(workflow);
    return Results.Ok(workflow);
});

app.MapGet("/executions/{id}", (string id) =>
{
    if(!executions.TryGetValue(id, out var execution)) return Results.NotFound();
    return Results.Ok(execution);
});

app.MapPost("/workflows/{id}/execute", (string id) =>
{
    if(!workflows.TryGetValue(id, out var workflow)) return Results.NotFound();
    WorkflowExecution executor = new(workflow);
    executions[executor.Id] = executor;
    _ = Task.Run(() => executor.Execute());
    return Results.Accepted($"/executions/{executor.Id}", new { executionId = executor.Id });
});

app.MapGet("/workflows/nodes", () =>
{
    NodeRegistry registry = new();
    return  Results.Ok(registry.GetAvailableNodes());
});

app.Run();