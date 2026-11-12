using C7N.Engine;
using C7N.Engine.Registry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var workflows = new List<Workflow>();

app.MapPost("/workflows", (Workflow workflow) =>
{
    var alreadyExists = workflows.Any(w => w.Id == workflow.Id);
    if (alreadyExists)
    {
        return Results.Conflict($"Workflow {workflow.Id} already exists");
    }
    workflows.Add(workflow);
    return Results.Created($"/workflows/{workflow.Id}", workflow);
});

app.MapGet("/workflows/{id}", (string id) =>
{
    var workflow = workflows.SingleOrDefault(w => w.Id == id);
    Console.WriteLine(workflows);

    return workflow is null
        ? Results.NotFound()
        : Results.Ok(workflow);
});

app.MapPost("/workflows/{id}/execute", async (string id) =>
{
    var workflow = workflows.SingleOrDefault(w => w.Id == id);

    if (workflow is null)
    {
        return Results.NotFound();
    }

    WorkflowExecution executor = new(workflow);

    var results = await executor.Execute();

    return Results.Ok(results);
});

app.MapGet("/workflows/nodes", () =>
{
    NodeRegistry nodes = new();
    return  Results.Ok(nodes.GetAvailableNodes());
});

app.Run();