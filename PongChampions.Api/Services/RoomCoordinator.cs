using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PongChampions.Data;

namespace PongChampions.Api.Services;

// The server deliberately runs one instance. The database lock also serializes
// independent contexts and migration/maintenance clients that use this protocol.
public sealed class RoomCoordinator
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<IAsyncDisposable> BeginAsync(AppDbContext context, CancellationToken token = default)
    {
        await gate.WaitAsync(token);

        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await context.Database.BeginTransactionAsync(token);
            await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(70630101)", token);

            return new Lease(gate, transaction);
        }
        catch
        {
            if (transaction is not null)
                await transaction.DisposeAsync();

            gate.Release();

            throw;
        }
    }

    private sealed class Lease(
        SemaphoreSlim gate,
        IDbContextTransaction transaction) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await transaction.DisposeAsync();
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
