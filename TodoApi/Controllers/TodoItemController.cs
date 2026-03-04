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
    public async Task<ActionResult<IList<TodoItemResponse>>> GetTodoItems([FromQuery] bool includeDeleted = false)
    {
        var query = _context.TodoItem.AsNoTracking();

        if (!includeDeleted)
        {
            query = query.Where(x => !x.IsDeleted);
        }

        var response = await query.Select(x => new TodoItemResponse
        {
            Id = x.Id,
            Text = x.Text,
            IsCompleted = x.IsCompleted,
            IsDeleted = x.IsDeleted
        }).ToListAsync();

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItemResponse>> GetTodoItem(long id)
    {
        var todoItem = await _context.TodoItem.FirstOrDefaultAsync(t => t.Id == id);

        if (todoItem == null)
        {
            return NotFound();
        }

        return Ok(new TodoItemResponse { Text = todoItem.Text, IsCompleted = todoItem.IsCompleted, Id = todoItem.Id, IsDeleted = todoItem.IsDeleted });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> PutTodoItem(long id, UpdateTodoItem payload)
    {
        var todoItem = await _context.TodoItem.FirstOrDefaultAsync(t => t.Id == id);

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
        var todoListExists = await _context.TodoList
            .AnyAsync(l => l.Id == payload.TodoListId);

        if (!todoListExists)
        {
            return BadRequest(new { error = "The specified list does not exist." });
        }

        var newItem = new TodoItem { Text = payload.Text, TodoListId = payload.TodoListId, IsCompleted = false };

        _context.TodoItem.Add(newItem);
        await _context.SaveChangesAsync();

        var response = new TodoItemResponse { Text = newItem.Text, Id = newItem.Id, IsCompleted = newItem.IsCompleted, IsDeleted = newItem.IsDeleted };

        return CreatedAtAction(nameof(GetTodoItem), new { id = newItem.Id }, response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTodoItem(long id)
    {
        var todoItem = await _context.TodoItem.FirstOrDefaultAsync(t => t.Id == id);
        if (todoItem == null)
        {
            return NotFound();
        }

        todoItem.IsDeleted = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
