using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Controllers;
using TodoApi.Dtos.TodoItem;
using TodoApi.Models;

namespace TodoApi.Tests;

#nullable disable
public class TodoItemControllerTests
{
    private DbContextOptions<TodoContext> DatabaseContextOptions()
    {
        return new DbContextOptionsBuilder<TodoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private void PopulateDatabaseContext(TodoContext context)
    {
        context.TodoList.Add(new TodoList { Id = 1, Name = "List 1" });

        context.TodoItem.Add(new TodoItem { Id = 1, Text = "Item 1", IsCompleted = false, IsDeleted = false, TodoListId = 1 });
        context.TodoItem.Add(new TodoItem { Id = 2, Text = "Item 2", IsCompleted = true, IsDeleted = false, TodoListId = 1 });
        context.TodoItem.Add(new TodoItem { Id = 3, Text = "Item 3", IsCompleted = false, IsDeleted = true, TodoListId = 1 });

        context.SaveChanges();
    }

    [Fact]
    public async Task GetTodoItems_WhenCalled_ReturnsNonDeletedItems()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.GetTodoItems();

            Assert.IsType<OkObjectResult>(result.Result);
            var items = (result.Result as OkObjectResult).Value as IList<TodoItemResponse>;
            Assert.Equal(2, items.Count);
            Assert.DoesNotContain(items, i => i.IsDeleted);
        }
    }

    [Fact]
    public async Task GetTodoItems_WhenIncludeDeleted_ReturnsAllItems()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.GetTodoItems(includeDeleted: true);

            Assert.IsType<OkObjectResult>(result.Result);
            var items = (result.Result as OkObjectResult).Value as IList<TodoItemResponse>;
            Assert.Equal(3, items.Count);
        }
    }

    [Fact]
    public async Task GetTodoItem_WhenCalled_ReturnsTodoItemById()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.GetTodoItem(1);

            Assert.IsType<OkObjectResult>(result.Result);
            var item = (result.Result as OkObjectResult).Value as TodoItemResponse;
            Assert.Equal(1, item.Id);
            Assert.Equal("Item 1", item.Text);
        }
    }

    [Fact]
    public async Task GetTodoItem_WhenItemDoesntExist_ReturnsNotFound()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.GetTodoItem(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }

    [Fact]
    public async Task PutTodoItem_WhenCalled_UpdatesTheTodoItem()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.PutTodoItem(1, new UpdateTodoItem
            {
                Text = "Updated Item 1",
                IsCompleted = true,
                IsDeleted = false
            });

            Assert.IsType<OkResult>(result);

            var updated = await context.TodoItem.FirstAsync(t => t.Id == 1);
            Assert.Equal("Updated Item 1", updated.Text);
            Assert.True(updated.IsCompleted);
        }
    }

    [Fact]
    public async Task PutTodoItem_WhenItemDoesntExist_ReturnsNotFound()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.PutTodoItem(999, new UpdateTodoItem
            {
                Text = "Does not matter",
                IsCompleted = false,
                IsDeleted = false
            });

            Assert.IsType<NotFoundResult>(result);
        }
    }

    [Fact]
    public async Task PostTodoItem_WhenCalled_CreatesTodoItem()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.PostTodoItem(new CreateTodoItem
            {
                Text = "New Item",
                TodoListId = 1
            });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(4, context.TodoItem.Count());

            var response = (result.Result as CreatedAtActionResult).Value as TodoItemResponse;
            Assert.Equal("New Item", response.Text);
            Assert.False(response.IsCompleted);
        }
    }

    [Fact]
    public async Task PostTodoItem_WhenTodoListDoesntExist_ReturnsBadRequest()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.PostTodoItem(new CreateTodoItem
            {
                Text = "New Item",
                TodoListId = 999
            });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }

    [Fact]
    public async Task DeleteTodoItem_WhenCalled_SoftDeletesItem()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.DeleteTodoItem(1);

            Assert.IsType<NoContentResult>(result);

            var deleted = await context.TodoItem.FirstAsync(t => t.Id == 1);
            Assert.True(deleted.IsDeleted);
        }
    }

    [Fact]
    public async Task DeleteTodoItem_WhenItemDoesntExist_ReturnsNotFound()
    {
        using (var context = new TodoContext(DatabaseContextOptions()))
        {
            PopulateDatabaseContext(context);

            var controller = new TodoItemController(context);

            var result = await controller.DeleteTodoItem(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
