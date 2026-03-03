using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos;
using TodoApi.Dtos.TodoItem;
using TodoApi.Models;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TodoItemController : ControllerBase
{
    private readonly TodoContext _context;

    public TodoItemController(TodoContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IList<TodoItemResponse>>> GetTodoItems()
    {
        var response = await _context.TodoItem.AsNoTracking().Select(x => new TodoItemResponse
        {
            Id = x.Id,
            Text = x.Text,
            IsCompleted = x.IsCompleted
        }).ToListAsync();

        return Ok(response);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItemResponse>> GetTodoItem(long id)
    {
        var todoItem = await _context.TodoItem.FindAsync(id);

        if (todoItem == null)
        {
            return NotFound();
        }

        return Ok(new TodoItemResponse { Text = todoItem.Text, IsCompleted = todoItem.IsCompleted, Id = todoItem.Id});
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> PutTodoItem(long id, UpdateTodoItem payload)
    {
        var todoItem = await _context.TodoItem.FindAsync(id);

        if (todoItem == null)
        {
            return NotFound();
        }

        todoItem.Text = payload.Text;
        todoItem.IsCompleted = payload.IsCompleted;
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult<TodoItemResponse>> PostTodoItem(CreateTodoItem payload)
    {
        var newItem = new TodoItem { Text = payload.Text, TodoListId = payload.TodoListId, IsCompleted = false };

        _context.TodoItem.Add(newItem);
        await _context.SaveChangesAsync();

        var response = new TodoItemResponse { Text = newItem.Text, Id = newItem.Id, IsCompleted = newItem.IsCompleted };

        return CreatedAtAction(nameof(GetTodoItem), new { id = newItem.Id }, response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTodoItem(long id)
    {
        var todoItem = await _context.TodoItem.FindAsync(id);
        if (todoItem == null)
        {
            return NotFound();
        }

        _context.TodoItem.Remove(todoItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
