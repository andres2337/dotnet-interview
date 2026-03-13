# Sync Design Notes

## High-Level Overview

The synchronization logic lives in the ASP.NET Core API because the challenge explicitly emphasizes resilience, retries, partial failures, and background execution. Those concerns fit naturally in the backend, while the React app acts as an observability and demo surface.

The solution stores synchronization state in the local SQL Server database instead of relying on Hangfire state. Hangfire schedules and runs synchronization jobs, but business-level sync state belongs to the domain model and audit tables.

## Structure

The implementation is split into four areas:

1. Domain persistence
- `TodoList` and `TodoItem` now persist sync metadata such as `ExternalId`, `SyncStatus`, `LastModifiedAtUtc`, `LastSyncedAtUtc`, and `LastSyncError`.
- `SyncRun` stores an audit trail for each synchronization execution.

2. Sync orchestration
- `ITodoSyncService` owns the sync workflow.
- `ISyncPlanner` builds a simple plan by comparing local entities with the external snapshot.
- `ISyncExecutor` applies the plan and updates entity state plus `SyncRun` counters.

3. External integration
- `IExternalTodoApiClient` isolates the external API contract.
- The HTTP client applies lightweight retry logic for transient failures.
- A local mock external API project is included for demo and end-to-end testing.

4. Demo UI
- The React app polls the local API, displays sync status, shows the last run summary, and allows manual sync execution.

## Structure of the Mock External API

A second ASP.NET Core project, `ExternalTodoApi.Mock`, simulates the external system described by the exercise.
It exposes:
- `GET /todolists`
- `POST /todolists`
- `PATCH /todolists/{todolistId}`
- `DELETE /todolists/{todolistId}`
- `PATCH /todolists/{todolistId}/todoitems/{todoitemId}`
- `DELETE /todolists/{todolistId}/todoitems/{todoitemId}`

The mock stores state in memory and starts with seeded todo lists/items. This allows the sync workflow to be demonstrated without depending on a third-party environment.

## Key Design Decisions

### Persist sync state in SQL Server
This makes entity-level sync status queryable by the UI and testable independently of Hangfire. It also keeps the state durable across restarts.

### Use Hangfire for scheduling
Hangfire simplifies recurring execution, concurrency control, and operational visibility. It is a better fit here than building a custom `BackgroundService` loop from scratch.

### Keep sync status on entities and keep run history separate
`TodoList` and `TodoItem` answer "what is the state of this record right now?".
`SyncRun` answers "what happened during the last execution?".
Those are different concerns and both matter in the interview demo.

### Prefer deterministic behavior over over-engineering conflict resolution
The challenge documentation does not guarantee robust remote versioning or timestamps. The implementation therefore uses a simple, deterministic strategy and documents the limitation instead of pretending full conflict-free synchronization exists.

## Resilience and Error Handling

- Transient HTTP failures retry with exponential backoff.
- Non-transient failures are surfaced immediately.
- Partial failures do not abort the entire run.
- Failing entities are marked with `SyncStatus = Failed` and keep `LastSyncError`.
- Each synchronization run writes aggregated counters and error summary to `SyncRun`.

## Contract Assumptions

The provided external API documentation is not fully symmetrical. In particular, it documents updates and deletes for todo items, but does not clearly expose a dedicated create-item endpoint.

Because of that, the implementation makes this explicit assumption:
- creating a list with items is supported through the list create endpoint
- updates and deletes for existing items are supported through nested item endpoints
- creating a brand-new item inside an already existing external list may require list-level update semantics beyond what is fully specified

This ambiguity is captured in the code and should be discussed during the interview.

## Edge Cases Considered

- Local entities pending sync should not be overwritten blindly by remote state during the same run.
- Soft-deleted local entities remain visible when `includeDeleted=true`, which allows the UI to demonstrate delete propagation.
- Sync runs should not overlap; concurrent runs are rejected.
- A failed run should still leave enough data for the UI to explain what happened.

## Areas for Improvement

- Add per-entity sync audit rows (`SyncRunEntries`) if deeper debugging becomes necessary.
- Improve conflict resolution if the external API later exposes timestamps, row versions, or etags.
- Protect the Hangfire dashboard and manual sync endpoint if this moves beyond interview scope.
- Add frontend integration tests once a test runner is introduced in the React repo.
- Introduce background notifications instead of polling if real-time UX becomes important.

## How to Run

### External mock API
1. Run `dotnet run --project C:\Users\Andres\GitHub\dotnet-interview\ExternalTodoApi.Mock\ExternalTodoApi.Mock.csproj`
2. It will listen on `http://localhost:8080`

### Backend
1. Ensure SQL Server is reachable using the `TodoContext` connection string from `appsettings.Development.json`.
2. Run EF Core migrations.
3. Run `dotnet run --project C:\Users\Andres\GitHub\dotnet-interview\TodoApi\TodoApi.csproj`
4. Visit `/hangfire` in development if you want job visibility.

### Frontend
1. Start the React dev server in `react-interview`.
2. The Vite proxy forwards `/api` requests to the local API.
3. Use the UI to create local changes, trigger sync manually, and observe polling-based updates.
