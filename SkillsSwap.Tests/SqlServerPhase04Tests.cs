using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Services;
using Xunit.Abstractions;

namespace SkillsSwap.Tests;

public sealed class SqlServerPhase04FactAttribute : FactAttribute
{
    public SqlServerPhase04FactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SKILLSWAP_PHASE04_SQL") != "1")
            Skip = "Set SKILLSWAP_PHASE04_SQL=1 to opt in to the isolated local SQL Server database.";
    }
}

public sealed class SqlServerPhase04Tests(ITestOutputHelper output)
{
    private const string Connection = "Server=localhost;Database=SkillsSwap_Phase04_Integration;Integrated Security=True;TrustServerCertificate=True";

    private static async Task<SqlConnection> OpenAsync()
    {
        var connection = new SqlConnection(Connection);
        try
        {
            await connection.OpenAsync();
            await using var guard = connection.CreateCommand();
            guard.CommandText = """
                IF DB_NAME() <> N'SkillsSwap_Phase04_Integration'
                   OR CAST(SERVERPROPERTY('ServerName') AS nvarchar(128)) <> N'DESKTOP-70O9F5H'
                    THROW 51000, 'Integration database allowlist mismatch', 1;
                """;
            await guard.ExecuteNonQueryAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    [SqlServerPhase04Fact]
    public async Task Transaction_lock_times_out_then_releases_after_commit()
    {
        await using var first = await OpenAsync();
        await using var second = await OpenAsync();
        await using var tx1 = (SqlTransaction)await first.BeginTransactionAsync();
        await using var tx2 = (SqlTransaction)await second.BeginTransactionAsync();
        var resource = "SkillSwap:Phase04Test:" + Guid.NewGuid().ToString("N");
        Assert.Equal(0, await LockAsync(first, tx1, resource));
        Assert.Equal(-1, await LockAsync(second, tx2, resource));
        await tx1.CommitAsync();
        Assert.Equal(0, await LockAsync(second, tx2, resource));
        await tx2.RollbackAsync();
    }

    private static async Task<int> LockAsync(SqlConnection connection, SqlTransaction transaction, string resource)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DECLARE @rc int;
            EXEC @rc=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=250;
            SELECT @rc;
            """;
        command.Parameters.AddWithValue("@resource", resource);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    [SqlServerPhase04Fact]
    public async Task Concurrent_review_pending_proposal_and_revision_inserts_each_save_only_one_row()
    {
        await using var connection = await OpenAsync();
        await using var db = Context(connection);
        var request = await db.LearningRequests.AsNoTracking().SingleAsync(r => r.Message == "04B legacy 1");
        var session = new LearningSession { LearningRequestId = request.Id, StartsAt = DateTimeOffset.UtcNow.AddDays(1), DurationMinutes = 30 };
        db.LearningSessions.Add(session);
        await db.SaveChangesAsync();
        try
        {
            await using var staleConnection = await OpenAsync();
            await using var staleContext = Context(staleConnection);
            var stale = await staleContext.LearningSessions.SingleAsync(x => x.Id == session.Id);
            session.DurationMinutes = 45;
            await db.SaveChangesAsync();
            stale.DurationMinutes = 60;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());
            Assert.Equal(45, await staleContext.LearningSessions.AsNoTracking()
                .Where(x => x.Id == session.Id).Select(x => x.DurationMinutes).SingleAsync());

            async Task RaceAsync(Func<ApplicationDbContext, int, Task> insert)
            {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var ready = 0;
                async Task<bool> RunAsync(int side)
                {
                    await using var sql = await OpenAsync();
                    await using var worker = Context(sql);
                    await using var tx = await worker.Database.BeginTransactionAsync();
                    if (Interlocked.Increment(ref ready) == 2) gate.TrySetResult();
                    await gate.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    try
                    {
                        await insert(worker, side);
                        await worker.SaveChangesAsync();
                        await tx.CommitAsync();
                        return true;
                    }
                    catch (DbUpdateException ex) when (DatabaseError(ex) is 2601 or 2627)
                    {
                        return false;
                    }
                }
                Assert.Single(await Task.WhenAll(RunAsync(0), RunAsync(1)), saved => saved);
            }

            await RaceAsync((worker, _) =>
            {
                worker.Reviews.Add(new Review { LearningSessionId = session.Id, ReviewerId = request.LearnerId,
                    ReviewedUserId = request.TeacherId, Rating = 5 });
                return Task.CompletedTask;
            });
            Assert.Equal(1, await db.Reviews.CountAsync(r => r.LearningSessionId == session.Id));

            Task AddProposal(ApplicationDbContext worker, int revision, ScheduleProposalStatus status)
            {
                var now = DateTimeOffset.UtcNow;
                worker.SessionScheduleProposals.Add(new SessionScheduleProposal
                {
                    LearningSessionId = session.Id, Revision = revision, Status = status,
                    ProposedByUserId = request.LearnerId, StartsAtUtc = now.AddDays(2),
                    CreatedAtUtc = now, DurationMinutes = 45,
                    ResolvedByUserId = status == ScheduleProposalStatus.Withdrawn ? request.LearnerId : null,
                    ResolvedAtUtc = status == ScheduleProposalStatus.Withdrawn ? now : null
                });
                return Task.CompletedTask;
            }
            await RaceAsync((worker, side) => AddProposal(worker, side + 1, ScheduleProposalStatus.Pending));
            Assert.Equal(1, await db.SessionScheduleProposals.CountAsync(p => p.LearningSessionId == session.Id && p.Status == ScheduleProposalStatus.Pending));
            await RaceAsync((worker, _) => AddProposal(worker, 3, ScheduleProposalStatus.Withdrawn));
            Assert.Equal(1, await db.SessionScheduleProposals.CountAsync(p => p.LearningSessionId == session.Id && p.Revision == 3));
        }
        finally
        {
            await db.SessionScheduleProposals.Where(p => p.LearningSessionId == session.Id).ExecuteDeleteAsync();
            await db.Reviews.Where(r => r.LearningSessionId == session.Id).ExecuteDeleteAsync();
            await db.LearningSessions.Where(s => s.Id == session.Id).ExecuteDeleteAsync();
        }
    }

    private static ApplicationDbContext Context(SqlConnection connection) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);

    [SqlServerPhase04Fact]
    public async Task Peer_request_persists_and_rejects_sequential_duplicates_and_repeated_transitions()
    {
        await using var connection = await OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        var learner = Guid.NewGuid();
        var teacher = Guid.NewGuid();
        var role = await db.Roles.SingleOrDefaultAsync(r => r.NormalizedName == "MEMBER");
        var temporaryRole = role is null;
        role ??= new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Member", NormalizedName = "MEMBER" };
        var skill = await db.Skills.Where(s => s.AccessTier == AccessTier.Free && s.Status == PublicationStatus.Published
            && s.SubCategory.IsActive && s.SubCategory.Category.IsActive).OrderBy(s => s.Id).FirstAsync();
        if (temporaryRole) db.Roles.Add(role);
        db.Users.AddRange(Member(learner), Member(teacher));
        db.UserRoles.AddRange(new() { UserId = learner, RoleId = role.Id }, new() { UserId = teacher, RoleId = role.Id });
        db.UserSkills.Add(new() { UserId = teacher, SkillId = skill.Id, Intent = SkillIntent.Teach });
        await db.SaveChangesAsync();
        try
        {
            var service = new PeerRequestService(db);
            var input = new PeerRequestInput { TeacherId = teacher, SkillId = skill.Id, Message = "04B isolated service fixture" };
            var created = await service.CreateAsync(learner, input, default);
            var id = Assert.IsType<long>(created.Id);
            await using var second = await OpenAsync();
            await using var verifier = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(second).Options);
            var persisted = await verifier.LearningRequests.AsNoTracking().SingleAsync(r => r.Id == id);
            Assert.Equal(RequestStatus.Pending, persisted.Status);
            Assert.Null((await service.CreateAsync(learner, input, default)).Id);
            Assert.Null(await service.TransitionAsync(persisted, teacher, RequestStatus.Accepted, default));
            Assert.NotNull(await service.TransitionAsync(persisted, teacher, RequestStatus.Accepted, default));
            Assert.Null((await service.CreateAsync(learner, input, default)).Id);
            var history = await verifier.LearningRequests.AsNoTracking().Where(r => r.LearnerId == learner).ToListAsync();
            Assert.Equal(RequestStatus.Accepted, Assert.Single(history).Status);
            Assert.NotNull(await service.TransitionAsync(persisted, teacher, RequestStatus.Declined, default));

            await db.LearningRequests.Where(r => r.Id == id).ExecuteUpdateAsync(set =>
                set.SetProperty(r => r.Status, RequestStatus.Declined));
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var initialized = 0;
            async Task<(long? Id, string? Error)> Submit()
            {
                await using var sql = await OpenAsync();
                await using var worker = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(sql).Options);
                if (Interlocked.Increment(ref initialized) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
                var result = await new PeerRequestService(worker).CreateAsync(learner, input, default);
                Assert.DoesNotContain(worker.ChangeTracker.Entries<LearningRequest>(), e => e.State == EntityState.Added);
                return result;
            }
            var outcomes = await Task.WhenAll(Submit(), Submit());
            foreach (var outcome in outcomes)
                output.WriteLine($"Concurrent submit: saved={outcome.Id.HasValue}, feedback={outcome.Error}");
            Assert.Single(outcomes, x => x.Id.HasValue);
            var losing = Assert.Single(outcomes, x => !x.Id.HasValue);
            Assert.Contains(losing.Error, new[]
            {
                "You already have an active learning request for this teacher and skill.",
                "Another request operation was in progress. Please try again."
            });
            var pending = Assert.Single(await verifier.LearningRequests.AsNoTracking()
                .Where(r => r.LearnerId == learner && r.Status == RequestStatus.Pending).ToListAsync());
            foreach (var rival in new[] { RequestStatus.Accepted, RequestStatus.Declined, RequestStatus.Cancelled })
            {
                await db.LearningRequests.Where(r => r.Id == pending.Id).ExecuteUpdateAsync(set =>
                    set.SetProperty(r => r.Status, RequestStatus.Pending));
                ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
                initialized = 0;

                async Task<bool> Transition(RequestStatus status)
                {
                    await using var sql = await OpenAsync();
                    await using var worker = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(sql).Options);
                    if (Interlocked.Increment(ref initialized) == 2) ready.TrySetResult();
                    await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    var actor = status == RequestStatus.Cancelled ? learner : teacher;
                    try
                    {
                        return await new PeerRequestService(worker).TransitionAsync(pending, actor, status, default) is null;
                    }
                    catch (Exception ex) when (DatabaseError(ex) == 1205)
                    {
                        output.WriteLine($"Concurrent transition {status}: SQL error 1205");
                        return false;
                    }
                }

                var results = await Task.WhenAll(Transition(RequestStatus.Accepted), Transition(rival));
                Assert.Single(results, success => success);
                var final = await verifier.LearningRequests.AsNoTracking().SingleAsync(r => r.Id == pending.Id);
                Assert.Equal(results[0] ? RequestStatus.Accepted : rival, final.Status);
                output.WriteLine($"Concurrent accept versus {rival}: persisted {final.Status}");
            }
        }
        finally
        {
            await db.LearningRequests.Where(r => r.LearnerId == learner && r.TeacherId == teacher).ExecuteDeleteAsync();
            await db.UserSkills.Where(x => x.UserId == teacher).ExecuteDeleteAsync();
            await db.UserRoles.Where(x => x.UserId == learner || x.UserId == teacher).ExecuteDeleteAsync();
            await db.Users.Where(x => x.Id == learner || x.Id == teacher).ExecuteDeleteAsync();
            if (temporaryRole)
                await db.Roles.Where(x => x.Id == role.Id).ExecuteDeleteAsync();
        }
    }

    private static int? DatabaseError(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is SqlException sql) return sql.Number;
        return null;
    }

    private static ApplicationUser Member(Guid id) => new()
    {
        Id = id, FullName = "04B Synthetic Member", UserName = id.ToString(), NormalizedUserName = id.ToString(),
        EmailConfirmed = true, IsProfileComplete = true, Status = AccountStatus.Active
    };
}
