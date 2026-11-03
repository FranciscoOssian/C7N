using C7N.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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
    var akreadyExists = workflows.Any(w => w.Id == workflow.Id);
    if (akreadyExists)
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

app.Run();