namespace TodoApi.Services.ExternalApi;

public class ExternalTodoListDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public bool IsDeleted { get; set; }
    public List<ExternalTodoItemDto> TodoItems { get; set; } = [];
}
