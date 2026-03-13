using ExternalTodoApi.Mock;
using ExternalTodoApi.Mock.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MockStore>();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/todolists", (MockStore store) => Results.Ok(store.Snapshot()));

app.MapPost("/todolists", (CreateTodoListBody body, MockStore store) =>
{
    var created = store.CreateList(body);
    return Results.Created($"/todolists/{created.Id}", created);
});

app.MapPatch("/todolists/{todolistId:long}", (long todolistId, UpdateTodoListBody body, MockStore store) =>
{
    var updated = store.UpdateList(todolistId, body);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapDelete("/todolists/{todolistId:long}", (long todolistId, MockStore store) =>
    store.DeleteList(todolistId) ? Results.NoContent() : Results.NotFound());

app.MapPatch("/todolists/{todolistId:long}/todoitems/{todoitemId:long}", (long todolistId, long todoitemId, UpdateTodoItemBody body, MockStore store) =>
{
    var updated = store.UpdateItem(todolistId, todoitemId, body);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapDelete("/todolists/{todolistId:long}/todoitems/{todoitemId:long}", (long todolistId, long todoitemId, MockStore store) =>
    store.DeleteItem(todolistId, todoitemId) ? Results.NoContent() : Results.NotFound());

app.Run();
