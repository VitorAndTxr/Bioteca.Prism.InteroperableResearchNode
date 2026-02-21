using Bioteca.Prism.Data.Interfaces.Sync;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bioteca.Prism.Data.Repositories.Sync;

/// <summary>
/// PostgreSQL implementation of sync log repository using Entity Framework Core
/// </summary>
public class SyncLogRepository : ISyncLogRepository
{
    private readonly PrismDbContext _context;
    private readonly ILogger<SyncLogRepository> _logger;

    public SyncLogRepository(PrismDbContext context, ILogger<SyncLogRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SyncLog> AddAsync(SyncLog syncLog, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.SyncLogs.Add(syncLog);
            await _context.SaveChangesAsync(cancellationToken);
            return syncLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding sync log for remote node {RemoteNodeId}", syncLog.RemoteNodeId);
            throw;
        }
    }

    public async Task<SyncLog> UpdateAsync(SyncLog syncLog, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.SyncLogs.Update(syncLog);
            await _context.SaveChangesAsync(cancellationToken);
            return syncLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sync log {Id}", syncLog.Id);
            throw;
        }
    }

    public async Task<(List<SyncLog> items, int totalCount)> GetByRemoteNodeIdAsync(
        Guid remoteNodeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.SyncLogs
                .AsNoTracking()
                .Where(s => s.RemoteNodeId == remoteNodeId)
                .OrderByDescending(s => s.StartedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sync logs for remote node {RemoteNodeId}", remoteNodeId);
            throw;
        }
    }

    public async Task<SyncLog?> GetLatestCompletedAsync(Guid remoteNodeId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SyncLogs
                .AsNoTracking()
                .Where(s => s.RemoteNodeId == remoteNodeId && s.Status == "completed")
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest sync log for remote node {RemoteNodeId}", remoteNodeId);
            throw;
        }
    }
}
