using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Extensions;

namespace Server.Services;

public sealed class ProductEntryService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ILogger<ProductEntryService> _logger;

    public ProductEntryService(AppDbContext db, IHttpContextAccessor contextAccessor, ILogger<ProductEntryService> logger)
    {
        _db = db;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public async Task<ProductEntry> SaveAsync(ProductEntry entry, CancellationToken cancellationToken = default)
    {
        var userId = _contextAccessor.HttpContext?.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = string.IsNullOrWhiteSpace(entry.UserId)
                ? throw new InvalidOperationException("Cannot save entry without an authenticated user or provided user id.")
                : entry.UserId;
        }

        entry.UserId = userId;
        entry.CapturedAt = entry.CapturedAt == default ? DateTimeOffset.UtcNow : entry.CapturedAt;

        var exists = await _db.ProductEntries.AnyAsync(e => e.Id == entry.Id, cancellationToken);
        if (exists)
        {
            _logger.LogInformation("Entry {EntryId} already exists; skipping insert", entry.Id);
            return entry;
        }

        await _db.ProductEntries.AddAsync(entry, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public Task<List<ProductEntry>> GetLatestAsync(int take = 50, CancellationToken cancellationToken = default) =>
        _db.ProductEntries
            .AsNoTracking()
            .OrderByDescending(x => x.CapturedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<ProductEntry?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ProductEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
