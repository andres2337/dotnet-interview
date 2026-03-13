using ExternalTodoApi.Mock.Models;

namespace ExternalTodoApi.Mock;

public class MockStore
{
    private readonly object _lock = new();
    private long _nextListId = 3;
    private long _nextItemId = 5;

    public List<MockTodoList> TodoLists { get; } =
    [
        new MockTodoList
        {
            Id = 1,
            Name = "External seeded list",
            TodoItems =
            [
                new MockTodoItem { Id = 1, Text = "Seeded external item", IsCompleted = false },
                new MockTodoItem { Id = 2, Text = "Already completed external item", IsCompleted = true }
            ]
        },
        new MockTodoList
        {
            Id = 2,
            Name = "External archived list",
            IsDeleted = true,
            TodoItems = [new MockTodoItem { Id = 3, Text = "Deleted list item", IsCompleted = false, IsDeleted = true }]
        }
    ];

    public IReadOnlyList<MockTodoList> Snapshot()
    {
        lock (_lock)
        {
            return TodoLists.Select(CloneList).ToList();
        }
    }

    public MockTodoList CreateList(CreateTodoListBody body)
    {
        lock (_lock)
        {
            var list = new MockTodoList
            {
                Id = _nextListId++,
                Name = body.Name,
                TodoItems = body.TodoItems.Select(item => new MockTodoItem
                {
                    Id = _nextItemId++,
                    Text = item.Text,
                    IsCompleted = item.IsCompleted,
                }).ToList()
            };

            TodoLists.Add(list);
            return CloneList(list);
        }
    }

    public MockTodoList? UpdateList(long id, UpdateTodoListBody body)
    {
        lock (_lock)
        {
            var list = TodoLists.FirstOrDefault(x => x.Id == id);
            if (list is null)
            {
                return null;
            }

            list.Name = body.Name;
            list.IsDeleted = body.IsDeleted;

            foreach (var newItem in body.TodoItems ?? [])
            {
                list.TodoItems.Add(new MockTodoItem
                {
                    Id = _nextItemId++,
                    Text = newItem.Text,
                    IsCompleted = newItem.IsCompleted,
                });
            }

            return CloneList(list);
        }
    }

    public bool DeleteList(long id)
    {
        lock (_lock)
        {
            var list = TodoLists.FirstOrDefault(x => x.Id == id);
            if (list is null)
            {
                return false;
            }

            list.IsDeleted = true;
            foreach (var item in list.TodoItems)
            {
                item.IsDeleted = true;
            }

            return true;
        }
    }

    public MockTodoItem? UpdateItem(long listId, long itemId, UpdateTodoItemBody body)
    {
        lock (_lock)
        {
            var item = TodoLists.FirstOrDefault(x => x.Id == listId)?.TodoItems.FirstOrDefault(x => x.Id == itemId);
            if (item is null)
            {
                return null;
            }

            item.Text = body.Text;
            item.IsCompleted = body.IsCompleted;
            item.IsDeleted = body.IsDeleted;
            return CloneItem(item);
        }
    }

    public bool DeleteItem(long listId, long itemId)
    {
        lock (_lock)
        {
            var item = TodoLists.FirstOrDefault(x => x.Id == listId)?.TodoItems.FirstOrDefault(x => x.Id == itemId);
            if (item is null)
            {
                return false;
            }

            item.IsDeleted = true;
            return true;
        }
    }

    private static MockTodoList CloneList(MockTodoList list)
    {
        return new MockTodoList
        {
            Id = list.Id,
            Name = list.Name,
            IsDeleted = list.IsDeleted,
            TodoItems = list.TodoItems.Select(CloneItem).ToList()
        };
    }

    private static MockTodoItem CloneItem(MockTodoItem item)
    {
        return new MockTodoItem
        {
            Id = item.Id,
            Text = item.Text,
            IsCompleted = item.IsCompleted,
            IsDeleted = item.IsDeleted,
        };
    }
}
