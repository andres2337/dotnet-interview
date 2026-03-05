using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Controllers;
using TodoApi.Dtos.TodoList;
using TodoApi.Models;

namespace TodoApi.Tests;

#nullable disable
public class TodoListsControllerTests
{
    private DbContextOptions<TodoContext> DatabaseContextOptions()
    {
        return new DbContextOptionsBuilder<TodoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private void PopulateDatabaseContext(TodoContext context)
    {
        context.TodoList.Add(new TodoList { Id = 1, Name = "Task 1" });
        context.TodoList.Add(new TodoList { Id = 2, Name = "Task 2" });
        context.SaveChanges();
    }

    [Fact]
    public async Task GetTodoList_WhenCalled_ReturnsTodoListList()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var result = await controller.GetTodoLists();

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(2, ((result.Result as OkObjectResult).Value as IList<TodoListResponse>).Count);
        }
    }

    [Fact]
    public async Task GetTodoList_WhenCalled_ReturnsTodoListById()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var result = await controller.GetTodoList(1);

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(1, ((result.Result as OkObjectResult).Value as TodoListResponse).Id);
        }
    }

    [Fact]
    public async Task PutTodoList_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var result = await controller.PutTodoList(
                3,
                new Dtos.TodoList.UpdateTodoList { Name = "Task 3" }
            );

            Assert.IsType<NotFoundResult>(result);
        }
    }

    [Fact]
    public async Task PutTodoList_WhenCalled_UpdatesTheTodoList()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var todoList = await context.TodoList.Where(x => x.Id == 2).FirstAsync();
            var result = await controller.PutTodoList(
                todoList.Id,
                new UpdateTodoList { Name = "Changed Task 2", IsDeleted = true }
            );

            Assert.IsType<OkObjectResult>(result);
            var response = (result as OkObjectResult).Value as TodoListResponse;
            Assert.Equal("Changed Task 2", response.Name);
            Assert.True(response.IsDeleted);
        }
    }

    [Fact]
    public async Task PostTodoList_WhenCalled_CreatesTodoList()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var result = await controller.PostTodoList(new Dtos.TodoList.CreateTodoList { Name = "Task 3" });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(3, context.TodoList.Count());
        }
    }

    [Fact]
    public async Task GetTodoList_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var result = await controller.GetTodoList(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }

    [Fact]
    public async Task PutTodoList_WhenCalled_UpdatesIsDeleted()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var result = await controller.PutTodoList(
                1,
                new UpdateTodoList { Name = "Task 1", IsDeleted = true }
            );

            Assert.IsType<OkObjectResult>(result);
            var response = (result as OkObjectResult).Value as TodoListResponse;
            Assert.True(response.IsDeleted);

            var updated = await context.TodoList.FirstAsync(t => t.Id == 1);
            Assert.True(updated.IsDeleted);
        }
    }

    [Fact]
    public async Task GetTodoLists_WhenIncludeDeletedTrue_ReturnsDeletedItems()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);
            var itemToDelete = await context.TodoList.FirstAsync(t => t.Id == 1);
            itemToDelete.IsDeleted = true;
            await context.SaveChangesAsync();

            var controller = new TodoListsController(context);

            var resultWithoutDeleted = await controller.GetTodoLists();
            var withoutDeleted = (resultWithoutDeleted.Result as OkObjectResult).Value as IList<TodoListResponse>;
            Assert.Equal(1, withoutDeleted.Count);

            var resultWithDeleted = await controller.GetTodoLists(includeDeleted: true);
            var withDeleted = (resultWithDeleted.Result as OkObjectResult).Value as IList<TodoListResponse>;
            Assert.Equal(2, withDeleted.Count);
        }
    }

    [Fact]
    public async Task DeleteTodoList_WhenCalled_RemovesTodoList()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var result = await controller.DeleteTodoList(2);

            Assert.IsType<NoContentResult>(result);
            Assert.True(context.TodoList.First(t => t.Id == 2).IsDeleted);
        }
    }

    [Fact]
    public async Task CompleteAllItems_WhenCalled_MarksAllItemsAsCompleted()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);
            context.TodoItem.Add(new TodoItem { Id = 1, Text = "Item 1", IsCompleted = false, TodoListId = 1 });
            context.TodoItem.Add(new TodoItem { Id = 2, Text = "Item 2", IsCompleted = false, TodoListId = 1 });
            context.TodoItem.Add(new TodoItem { Id = 3, Text = "Item 3", IsCompleted = true, TodoListId = 1 });
            context.SaveChanges();

            var controller = new TodoListsController(context);

            var result = await controller.CompleteAllItems(1);

            Assert.IsType<OkObjectResult>(result);

            var dbItems = await context.TodoItem.Where(i => i.TodoListId == 1).ToListAsync();
            Assert.Equal(3, dbItems.Count);
            Assert.All(dbItems, i => Assert.True(i.IsCompleted));
        }
    }

    [Fact]
    public async Task CompleteAllItems_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var result = await controller.CompleteAllItems(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }

    [Fact]
    public async Task CompleteAllItems_WhenNoItems_ReturnsOkWithZeroUpdated()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoListsController(context);

            var result = await controller.CompleteAllItems(1);

            Assert.IsType<OkObjectResult>(result);
        }
    }

    [Fact]
    public async Task CompleteAllItems_SkipsDeletedItems()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);
            context.TodoItem.Add(new TodoItem { Id = 1, Text = "Active", IsCompleted = false, IsDeleted = false, TodoListId = 1 });
            context.TodoItem.Add(new TodoItem { Id = 2, Text = "Deleted", IsCompleted = false, IsDeleted = true, TodoListId = 1 });
            context.SaveChanges();

            var controller = new TodoListsController(context);

            var result = await controller.CompleteAllItems(1);

            Assert.IsType<OkObjectResult>(result);

            var activeItem = await context.TodoItem.FirstAsync(i => i.Id == 1);
            Assert.True(activeItem.IsCompleted);

            var deletedItem = await context.TodoItem.FirstAsync(i => i.Id == 2);
            Assert.False(deletedItem.IsCompleted);
        }
    }
}
