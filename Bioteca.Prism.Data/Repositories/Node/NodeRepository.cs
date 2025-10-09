using Bioteca.Prism.Data.Interfaces.Node;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bioteca.Prism.Data.Repositories.Node;

/// <summary>
/// PostgreSQL implementation of node repository using Entity Framework Core
/// </summary>
public class NodeRepository : INodeRepository
{
    private readonly PrismDbContext _context;
    private readonly ILogger<NodeRepository> _logger;

    public NodeRepository(PrismDbContext context, ILogger<NodeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResearchNode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ResearchNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving node {Id}", id);
            throw;
        }
    }

    public async Task<ResearchNode?> GetByCertificateFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ResearchNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.CertificateFingerprint == fingerprint, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving node by certificate fingerprint {Fingerprint}", fingerprint);
            throw;
        }
    }

    public async Task<List<ResearchNode>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ResearchNodes
                .AsNoTracking()
                .OrderByDescending(n => n.RegisteredAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all nodes");
            throw;
        }
    }

    public async Task<List<ResearchNode>> GetByStatusAsync(AuthorizationStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ResearchNodes
                .AsNoTracking()
                .Where(n => n.Status == status)
                .OrderByDescending(n => n.RegisteredAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving nodes with status {Status}", status);
            throw;
        }
    }

    public async Task<ResearchNode> AddAsync(ResearchNode node, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.ResearchNodes.Add(node);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Node {Id} added to database", node.Id);
            return node;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding node {Id}", node.Id);
            throw;
        }
    }

    public async Task<ResearchNode> UpdateAsync(ResearchNode node, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.ResearchNodes.Update(node);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Node {Id} updated in database", node.Id);
            return node;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating node {Id}", node.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var node = await _context.ResearchNodes
                .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

            if (node == null)
            {
                _logger.LogWarning("Node {Id} not found for deletion", id);
                return false;
            }

            _context.ResearchNodes.Remove(node);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Node {Id} deleted from database", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting node {Id}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ResearchNodes
                .AnyAsync(n => n.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of node {Id}", id);
            throw;
        }
    }

    public async Task<bool> CertificateExistsAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ResearchNodes
                .AnyAsync(n => n.CertificateFingerprint == fingerprint, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of certificate {Fingerprint}", fingerprint);
            throw;
        }
    }
}
