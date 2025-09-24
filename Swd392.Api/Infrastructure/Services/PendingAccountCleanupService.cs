using Microsoft.EntityFrameworkCore;
using Swd392.Api.Infrastructure.Database;

namespace Swd392.Api.Infrastructure.Services;

public class PendingAccountCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingAccountCleanupService> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _ttl;

    public class Options
    {
        public int IntervalSeconds { get; set; } = 60;
        public int TtlSeconds { get; set; } = 120;
    }

    public PendingAccountCleanupService(IServiceScopeFactory scopeFactory, ILogger<PendingAccountCleanupService> logger, Microsoft.Extensions.Options.IOptions<Options> opt)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var o = opt.Value;
        _interval = TimeSpan.FromSeconds(Math.Max(10, o.IntervalSeconds));
        _ttl = TimeSpan.FromSeconds(Math.Max(30, o.TtlSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PendingAccountCleanupService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await CleanupDanglingAccountsAsync(stoppingToken);
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cleanup loop");
            }
        }
        _logger.LogInformation("PendingAccountCleanupService stopping");
    }

    private async Task CleanupDanglingAccountsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KidsMathDbContext>();

        var cutoff = DateTime.UtcNow - _ttl;

        // Parents: Accounts with role parent, older than cutoff, and no parent row
        var danglingParents = await db.Accounts
            .Where(a => a.role == "parent" && a.created_at < cutoff && !db.Parents.Any(p => p.parent_id == a.account_id))
            .ToListAsync(ct);

        // Students: Accounts with role student, older than cutoff, and no student row
        var danglingStudents = await db.Accounts
            .Where(a => a.role == "student" && a.created_at < cutoff && !db.Students.Any(s => s.student_id == a.account_id))
            .ToListAsync(ct);

        if (danglingParents.Count == 0 && danglingStudents.Count == 0)
            return;

        db.Accounts.RemoveRange(danglingParents);
        db.Accounts.RemoveRange(danglingStudents);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Cleaned {ParentCount} unconfirmed parent accounts and {StudentCount} unconfirmed student accounts", danglingParents.Count, danglingStudents.Count);
    }
}