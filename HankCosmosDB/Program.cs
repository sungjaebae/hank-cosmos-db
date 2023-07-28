using Azure.Core;
using Azure.Identity;
using HankCosmosDB.Data;
using HankCosmosDB.Todos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TodoDbContext>(options =>
{
    options.UseCosmos(@"AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", "ToDoList");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/CreateDatabaseAsync", async ([FromServices]TodoDbContext todoDbContext) =>
{
    await todoDbContext.Database.EnsureDeletedAsync();
    var databaseResponse = await todoDbContext.Database.GetCosmosClient().CreateDatabaseIfNotExistsAsync("ToDoList");
    return databaseResponse.Database.Id;
}).WithName("1. CreateDatabaseAsync")
.WithOpenApi();

app.MapGet("/CreateContainerAsync", async ([FromServices] TodoDbContext todoDbContext) =>
{
    var databaseResponse = await todoDbContext.Database.GetCosmosClient().CreateDatabaseIfNotExistsAsync("ToDoList");
    var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync("Todo", "/Id", 1000);
        
    return containerResponse.Container.Id;
}).WithName("2. CreateDatabaseAsync")
.WithOpenApi();

app.MapGet("/ScaleContainerAsync", async ([FromServices] TodoDbContext todoDbContext) =>
{
    var databaseResponse = await todoDbContext.Database.GetCosmosClient().CreateDatabaseIfNotExistsAsync("ToDoList");
    var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync("Todo", "/Id", 1000);

    // Read the current throughput
    int? throughput = await containerResponse.Container.ReadThroughputAsync();
    int newThroughput = 0;
    if (throughput.HasValue)
    {
        Console.WriteLine("Current provisioned throughput : {0}\n", throughput.Value);
        newThroughput = throughput.Value - 100;
        if(newThroughput == 300) newThroughput = 1000;
        // Update throughput
        await containerResponse.Container.ReplaceThroughputAsync(newThroughput);
        Console.WriteLine("New provisioned throughput : {0}\n", newThroughput);
    }
    return $"Current provisioned throughput : {throughput.Value}\nNew provisioned throughput : {newThroughput}\n";
}).WithName("3. ScaleContainerAsync")
.WithOpenApi();

app.MapGet("/AddItemsToContainerAsync", async ([FromServices] TodoDbContext todoDbContext) =>
{
    Todo todo = new Todo { Id="todo.1", Title="공부하자", Body="SQL Antipatterns 읽기", IsCompleted=false};
    var findTodo = await todoDbContext.Todos.WithPartitionKey("todo.1").SingleOrDefaultAsync(i => i.Id == todo.Id);
    if (findTodo is null)
    {
        await todoDbContext.AddAsync(todo);
        await todoDbContext.SaveChangesAsync();
        return $"todo is not exists, new Todo Id is {todo}";
    }
    return $"todo exists, Todo is {todo}";
}).WithName("4. AddItemsToContainerAsync")
.WithOpenApi();

app.MapGet("/QueryItemsAsync", async ([FromServices] TodoDbContext todoDbContext) =>
{
    var todos = await todoDbContext.Todos.AsNoTracking().ToListAsync();
    return $"todos are {string.Join("\n",todos.Select(x => x.ToString()))}";
}).WithName("5. QueryItemsAsync")
.WithOpenApi();

app.MapGet("/ReplaceTodoItemAsync", async ([FromServices] TodoDbContext todoDbContext) =>
{
    var todo = await todoDbContext.Todos.AsNoTracking().WithPartitionKey("todo.1").SingleOrDefaultAsync(i => i.Id == "todo.1");
    if (todo is null)
    {
        return $"todo is not exists";
    }
    var newTodo = todo with { IsCompleted = !todo.IsCompleted };
    todoDbContext.Update(newTodo);
    await todoDbContext.SaveChangesAsync();

    return $"Todo is {todo}";
}).WithName("6. ReplaceTodoItemAsync")
.WithOpenApi();

app.MapGet("/DeleteTodoItemAsync", async ([FromServices] TodoDbContext todoDbContext) =>
{
    var todo = await todoDbContext.Todos.AsNoTracking().WithPartitionKey("todo.1").SingleOrDefaultAsync(i => i.Id == "todo.1");
    if (todo is null)
    {
        return $"todo is not exists";
    }
    todoDbContext.Remove(todo);
    await todoDbContext.SaveChangesAsync();

    return $"Todo is deleted";
}).WithName("7. DeleteTodoItemAsync")
.WithOpenApi();

app.MapGet("/DeleteDatabaseAndCleanupAsync", async ([FromServices] TodoDbContext todoDbContext) =>
{
    await todoDbContext.Database.EnsureDeletedAsync();
    return $"Deleted Database";
}).WithName("8. DeleteDatabaseAndCleanupAsync")
.WithOpenApi();


app.Run();
