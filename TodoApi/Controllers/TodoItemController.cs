using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos.TodoItem;
using TodoApi.Enums;
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
        var query = _context.TodoItem.AsNoTracking().AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(x => !x.IsDeleted);
        }

        var items = await query.OrderBy(x => x.Id).ToListAsync();
        return Ok(items.Select(x => x.ToResponse()).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItemResponse>> GetTodoItem(long id)
    {
        var todoItem = await _context.TodoItem.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        if (todoItem == null)
        {
            return NotFound();
        }

        return Ok(todoItem.ToResponse());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> PutTodoItem(long id, UpdateTodoItem payload)
    {
        var todoItem = await _context.TodoItem.Include(x => x.TodoList).FirstOrDefaultAsync(t => t.Id == id);

        if (todoItem == null)
        {
            return NotFound();
        }

        todoItem.Text = payload.Text;
        todoItem.IsCompleted = payload.IsCompleted;
        todoItem.IsDeleted = payload.IsDeleted;
        todoItem.LastModifiedAtUtc = DateTime.UtcNow;
        todoItem.SyncStatus = todoItem.ExternalId.HasValue
            ? (payload.IsDeleted ? SyncStatus.PendingDelete : SyncStatus.PendingUpdate)
            : SyncStatus.PendingCreate;

        MarkParentListForSync(todoItem.TodoList, todoItem.ExternalId.HasValue);
        await _context.SaveChangesAsync();

        return Ok(todoItem.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<TodoItemResponse>> PostTodoItem(CreateTodoItem payload)
    {
        var todoList = await _context.TodoList.FirstOrDefaultAsync(l => l.Id == payload.TodoListId);

        if (todoList == null)
        {
            return BadRequest(new { error = "The specified list does not exist." });
        }

        var newItem = new TodoItem
        {
            Text = payload.Text,
            TodoListId = payload.TodoListId,
            IsCompleted = false,
            SyncStatus = SyncStatus.PendingCreate,
            LastModifiedAtUtc = DateTime.UtcNow,
        };

        MarkParentListForSync(todoList, hasExternalItemId: false);

        _context.TodoItem.Add(newItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTodoItem), new { id = newItem.Id }, newItem.ToResponse());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTodoItem(long id)
    {
        var todoItem = await _context.TodoItem.Include(x => x.TodoList).FirstOrDefaultAsync(t => t.Id == id);
        if (todoItem == null)
        {
            return NotFound();
        }

        todoItem.IsDeleted = true;
        todoItem.LastModifiedAtUtc = DateTime.UtcNow;

        if (todoItem.ExternalId.HasValue)
        {
            todoItem.SyncStatus = SyncStatus.PendingDelete;
            MarkParentListForSync(todoItem.TodoList, hasExternalItemId: true);
        }
        else
        {
            todoItem.SyncStatus = SyncStatus.Synced;
            todoItem.LastSyncedAtUtc = DateTime.UtcNow;
            todoItem.LastSyncError = null;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static void MarkParentListForSync(TodoList todoList, bool hasExternalItemId)
    {
        todoList.LastModifiedAtUtc = DateTime.UtcNow;

        if (!todoList.ExternalId.HasValue)
        {
            todoList.SyncStatus = SyncStatus.PendingCreate;
            return;
        }

        if (!hasExternalItemId && todoList.SyncStatus == SyncStatus.Synced)
        {
            todoList.SyncStatus = SyncStatus.PendingUpdate;
            return;
        }

        if (todoList.SyncStatus == SyncStatus.Failed)
        {
            todoList.SyncStatus = SyncStatus.PendingUpdate;
        }
    }
}

