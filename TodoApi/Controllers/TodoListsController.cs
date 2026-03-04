using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos;
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

    // GET: api/todolists
    [HttpGet]
    public async Task<ActionResult<IList<TodoListResponse>>> GetTodoLists([FromQuery] bool includeDeleted = false)
    {
        var query = _context.TodoList.AsNoTracking();

        if (!includeDeleted)
        {
            query = query.Where(x => !x.IsDeleted);
        }

        var response = await query.Select(x => new TodoListResponse
        {
            Id = x.Id,
            Name = x.Name,
            IsDeleted = x.IsDeleted
        }).ToListAsync();

        return Ok(response);
    }

    // GET: api/todolists/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoListResponse>> GetTodoList(long id)
    {
        var todoList = await _context.TodoList.FirstOrDefaultAsync(t => t.Id == id);

        if (todoList == null)
        {
            return NotFound();
        }

        return Ok(new TodoListResponse { Id = todoList.Id, Name = todoList.Name, IsDeleted = todoList.IsDeleted });
    }

    // PUT: api/todolists/5
    [HttpPut("{id}")]
    public async Task<ActionResult> PutTodoList(long id, UpdateTodoList payload)
    {
        var todoList = await _context.TodoList.FirstOrDefaultAsync(t => t.Id == id);

        if (todoList == null)
        {
            return NotFound();
        }

        todoList.Name = payload.Name;
        await _context.SaveChangesAsync();

        return Ok(new TodoListResponse { Id = todoList.Id, Name = todoList.Name, IsDeleted = todoList.IsDeleted });
    }

    // POST: api/todolists
    [HttpPost]
    public async Task<ActionResult<TodoListResponse>> PostTodoList(CreateTodoList payload)
    {
        var todoList = new TodoList { Name = payload.Name };

        _context.TodoList.Add(todoList);
        await _context.SaveChangesAsync();

        var response = new TodoListResponse { Id = todoList.Id, Name = todoList.Name, IsDeleted = todoList.IsDeleted };

        return CreatedAtAction(nameof(GetTodoList), new { id = todoList.Id }, response);
    }

    // DELETE: api/todolists/5
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTodoList(long id)
    {
        var todoList = await _context.TodoList.FirstOrDefaultAsync(t => t.Id == id);
        if (todoList == null)
        {
            return NotFound();
        }

        todoList.IsDeleted = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
