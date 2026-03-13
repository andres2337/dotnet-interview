using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos.TodoList;
using TodoApi.Enums;
using TodoApi.Models;

namespace TodoApi.Controllers;

[Route("api/todolists")]
[ApiController]
public class TodoListsController : ControllerBase
{
    private readonly TodoContext _context;

    public TodoListsController(TodoContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IList<TodoListResponse>>> GetTodoLists([FromQuery] bool includeDeleted = false)
    {
        var query = _context.TodoList
            .AsNoTracking()
            .Include(x => x.Items)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(x => !x.IsDeleted);
        }

        var todoLists = await query
            .OrderBy(x => x.Id)
            .ToListAsync();

        return Ok(todoLists.Select(x => x.ToResponse()).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoListResponse>> GetTodoList(long id)
    {
        var todoList = await _context.TodoList
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (todoList == null)
        {
            return NotFound();
        }

        return Ok(todoList.ToResponse());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> PutTodoList(long id, UpdateTodoList payload)
    {
        var todoList = await _context.TodoList.Include(x => x.Items).FirstOrDefaultAsync(t => t.Id == id);

        if (todoList == null)
        {
            return NotFound();
        }

        todoList.Name = payload.Name;
        todoList.IsDeleted = payload.IsDeleted;
        todoList.LastModifiedAtUtc = DateTime.UtcNow;
        todoList.SyncStatus = todoList.ExternalId.HasValue ? (payload.IsDeleted ? SyncStatus.PendingDelete : SyncStatus.PendingUpdate) : SyncStatus.PendingCreate;

        await _context.SaveChangesAsync();

        return Ok(todoList.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<TodoListResponse>> PostTodoList(CreateTodoList payload)
    {
        var todoList = new TodoList
        {
            Name = payload.Name,
            SyncStatus = SyncStatus.PendingCreate,
            LastModifiedAtUtc = DateTime.UtcNow,
        };

        _context.TodoList.Add(todoList);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTodoList), new { id = todoList.Id }, todoList.ToResponse());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTodoList(long id)
    {
        var todoList = await _context.TodoList.FirstOrDefaultAsync(t => t.Id == id);
        if (todoList == null)
        {
            return NotFound();
        }

        todoList.IsDeleted = true;
        todoList.LastModifiedAtUtc = DateTime.UtcNow;
        todoList.SyncStatus = todoList.ExternalId.HasValue ? SyncStatus.PendingDelete : SyncStatus.PendingCreate;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/complete-all")]
    public async Task<ActionResult> CompleteAllItems(long id)
    {
        var todoList = await _context.TodoList.Include(x => x.Items).FirstOrDefaultAsync(t => t.Id == id);
        if (todoList == null)
        {
            return NotFound();
        }

        var activeItems = todoList.Items.Where(i => !i.IsDeleted && !i.IsCompleted).ToList();
        foreach (var item in activeItems)
        {
            item.IsCompleted = true;
            item.LastModifiedAtUtc = DateTime.UtcNow;
            item.SyncStatus = item.ExternalId.HasValue ? SyncStatus.PendingUpdate : SyncStatus.PendingCreate;
        }

        await _context.SaveChangesAsync();

        return Ok(new { UpdatedCount = activeItems.Count });
    }
}
