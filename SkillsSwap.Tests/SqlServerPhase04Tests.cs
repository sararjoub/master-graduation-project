using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using SkillsSwap.Controllers;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Security;
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

public sealed class SqlServerAutomaticCompletionFactAttribute : FactAttribute
{
    public SqlServerAutomaticCompletionFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SKILLSWAP_PHASE04_SQL") != "1" ||
            Environment.GetEnvironmentVariable("SKILLSWAP_PHASE05_AUTOMATIC_SQL") != "1")
            Skip = "Requires the automatic completion migration on the isolated SQL Server database.";
    }
}

public sealed partial class SqlServerPhase04Tests(ITestOutputHelper output)
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

    [SqlServerAutomaticCompletionFact]
    public async Task Direct_peer_lifecycle_persists_requests_completion_reviews_and_closure()
    {
        await using var connection = await OpenAsync();
        await using var db = Context(connection);
        var learner = Guid.NewGuid();
        var teacher = Guid.NewGuid();
        var role = await db.Roles.SingleOrDefaultAsync(r => r.NormalizedName == "MEMBER");
        var temporaryRole = role is null;
        role ??= new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Member", NormalizedName = "MEMBER" };
        if (temporaryRole) db.Roles.Add(role);
        var skill = await db.Skills.Where(s => s.AccessTier == AccessTier.Free && s.Status == PublicationStatus.Published &&
            s.SubCategory.IsActive && s.SubCategory.Category.IsActive).OrderBy(s => s.Id).FirstAsync();
        db.Users.AddRange(Member(learner), Member(teacher));
        db.UserRoles.AddRange(new() { UserId = learner, RoleId = role.Id }, new() { UserId = teacher, RoleId = role.Id });
        db.UserSkills.Add(new UserSkill { UserId = teacher, SkillId = skill.Id, Intent = SkillIntent.Teach });
        var relationship = new LearningRequest { LearnerId = learner, TeacherId = teacher, SkillId = skill.Id, Status = RequestStatus.Accepted };
        var reverseRelationship = new LearningRequest { LearnerId = teacher, TeacherId = learner, SkillId = skill.Id, Status = RequestStatus.Accepted };
        db.LearningRequests.AddRange(relationship, reverseRelationship);
        await db.SaveChangesAsync();
        var now = new DateTimeOffset(2090, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new SessionTestClock(now);
        var schedule = new SessionScheduleInput { Title = "Phase 04 test", Objectives = "Practise the selected skill.",
            TimeZoneId = "UTC", LocalStart = "2090-01-02T12:00", DurationMinutes = 30 };

        async Task<string?> Act(Guid actor, string action, long? session = null, long? item = null, SessionScheduleInput? input = null)
        {
            await using var workerConnection = await OpenAsync();
            await using var worker = Context(workerConnection);
            return await new PeerSessionService(worker, clock).ChangeAsync(relationship.Id, session, item, actor, action, input ?? new(), default);
        }

        async Task<LearningSession> ReadSession(long id) => await db.LearningSessions.AsNoTracking().SingleAsync(s => s.Id == id);
        async Task<string> Version(long id) => Convert.ToBase64String((await ReadSession(id)).RowVersion);
        IQueryable<LearningSessionRequest> Requests() => db.LearningSessionRequests.AsNoTracking().Where(r => r.LearningRequestId == relationship.Id);
        try
        {
            Assert.NotNull(await Act(Guid.NewGuid(), "request"));
            Assert.NotNull(await Act(teacher, "request"));
            Assert.NotNull(await Act(learner, "schedule", input: schedule));
            Assert.Null(await Act(learner, "request"));
            Assert.NotNull(await Act(learner, "request"));
            var original = await Requests().SingleAsync();
            Assert.Null(await Act(learner, "withdraw-request", item: original.Id));
            Assert.Null(await Act(learner, "request"));
            var pending = await Requests().SingleAsync(r => r.Status == LearningSessionRequestStatus.Pending);
            Assert.NotNull(await Act(teacher, "schedule", item: original.Id, input: schedule));

            var bothReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var ready = 0;
            async Task<string?> RaceSchedule()
            {
                if (Interlocked.Increment(ref ready) == 2) bothReady.TrySetResult();
                await bothReady.Task.WaitAsync(TimeSpan.FromSeconds(10));
                return await Act(teacher, "schedule", input: schedule);
            }
            var race = await Task.WhenAll(RaceSchedule(), RaceSchedule());
            Assert.Single(race, error => error is null);
            Assert.Contains(race, error => error?.Contains("overlaps", StringComparison.Ordinal) == true);
            var firstId = await db.LearningSessions.Where(s => s.LearningRequestId == relationship.Id).Select(s => s.Id).SingleAsync();
            Assert.Equal(SessionStatus.Scheduled, (await ReadSession(firstId)).Status);
            await using (var crossConnection = await OpenAsync())
            {
                await using var crossContext = Context(crossConnection);
                var conflict = await new PeerSessionService(crossContext, clock).ChangeAsync(
                    reverseRelationship.Id, null, null, learner, "schedule", schedule, default);
                Assert.Contains("overlaps", conflict);
            }
            var reverseSlot = new SessionScheduleInput { Title = "Reverse role fixture", Objectives = "Practice the skill",
                TimeZoneId = "UTC", LocalStart = "2090-01-02T15:00", DurationMinutes = 30 };
            await using (var reverseConnection = await OpenAsync())
            {
                await using var reverseContext = Context(reverseConnection);
                Assert.Null(await new PeerSessionService(reverseContext, clock).ChangeAsync(
                    reverseRelationship.Id, null, null, learner, "schedule", reverseSlot, default));
            }
            var reverseSessionId = await db.LearningSessions.Where(s => s.LearningRequestId == reverseRelationship.Id)
                .Select(s => s.Id).SingleAsync();
            Assert.Equal(LearningSessionRequestStatus.Pending, (await Requests().SingleAsync(r => r.Id == pending.Id)).Status);
            Assert.NotNull(await Act(teacher, "schedule", item: pending.Id, input: schedule));
            Assert.Equal(LearningSessionRequestStatus.Pending, (await Requests().SingleAsync(r => r.Id == pending.Id)).Status);

            schedule.LocalStart = "2090-01-02T12:30";
            Assert.Null(await Act(teacher, "schedule", item: pending.Id, input: schedule));
            var fulfilled = await Requests().SingleAsync(r => r.Id == pending.Id);
            var secondId = Assert.IsType<long>(fulfilled.FulfilledSessionId);
            Assert.Equal(LearningSessionRequestStatus.Fulfilled, fulfilled.Status);
            Assert.Empty(await db.SessionScheduleProposals.Where(p => p.LearningSession.LearningRequestId == relationship.Id).ToListAsync());

            Assert.NotNull(await Act(learner, "absence", secondId, input: new() { Reason = "\t\n\u00a0" }));
            Assert.Null(await Act(learner, "absence", secondId, input: new() { Reason = "Schedule conflict" }));
            var absence = await db.SessionAbsenceRequests.AsNoTracking().SingleAsync(a => a.LearningSessionId == secondId);
            Assert.NotNull(await Act(learner, "absence", secondId, input: new() { Reason = "Duplicate" }));
            schedule.DurationMinutes = 45;
            schedule.Version = await Version(secondId);
            Assert.Null(await Act(teacher, "reschedule", secondId, input: schedule));
            Assert.Equal(SessionAbsenceRequestStatus.Pending, (await db.SessionAbsenceRequests.AsNoTracking().SingleAsync(a => a.Id == absence.Id)).Status);
            schedule.LocalStart = "2090-01-02T13:00";
            Assert.NotNull(await Act(teacher, "reschedule", secondId, input: schedule));
            schedule.Version = await Version(secondId);
            Assert.Null(await Act(teacher, "reschedule", secondId, input: schedule));
            Assert.Equal(SessionAbsenceRequestStatus.ResolvedByReschedule, (await db.SessionAbsenceRequests.AsNoTracking().SingleAsync(a => a.Id == absence.Id)).Status);
            Assert.Equal(2, await db.SessionScheduleHistory.CountAsync(h => h.LearningSessionId == secondId));
            Assert.Null(await Act(learner, "absence", firstId, input: new() { Reason = "Cannot attend" }));
            Assert.Null(await Act(teacher, "cancel", firstId, input: new() { Version = await Version(firstId), Reason = "Schedule adjustment" }));
            Assert.Equal(SessionStatus.Cancelled, (await ReadSession(firstId)).Status);
            Assert.Equal(SessionAbsenceRequestStatus.ResolvedByCancellation,
                await db.SessionAbsenceRequests.Where(a => a.LearningSessionId == firstId).Select(a => a.Status).SingleAsync());
            Assert.NotNull(await Act(learner, "complete", secondId));
            async Task<bool> Complete(long requestId, long sessionId)
            {
                await using var sql = await OpenAsync();
                await using var worker = Context(sql);
                return await new PeerSessionService(worker, clock).CompleteDueSessionAsync(requestId, sessionId, default);
            }

            Assert.False(await Complete(relationship.Id, secondId));
            Assert.NotNull(await Act(teacher, "end"));
            Assert.Null(await Act(learner, "absence", secondId, input: new() { Reason = "Late change" }));
            var updated = await ReadSession(secondId);
            clock.Now = updated.StartsAt.AddMinutes(updated.DurationMinutes).AddTicks(-1);
            Assert.False(await Complete(relationship.Id, secondId));
            Assert.NotNull(await Act(learner, "review", secondId, input: new() { Rating = 5 }));

            clock.Now = updated.StartsAt.AddMinutes(updated.DurationMinutes);
            Assert.False(await Complete(relationship.Id, firstId));
            Assert.Single(await Task.WhenAll(Complete(relationship.Id, secondId), Complete(relationship.Id, secondId)), x => x);
            var reverse = await ReadSession(reverseSessionId);
            var legacyConfirmation = reverse.StartsAt.AddMinutes(reverse.DurationMinutes);
            await db.LearningSessions.Where(x => x.Id == reverseSessionId).ExecuteUpdateAsync(set => set
                .SetProperty(x => x.Status, SessionStatus.Completed)
                .SetProperty(x => x.CompletedAtUtc, legacyConfirmation)
                .SetProperty(x => x.LearnerCompletedAtUtc, legacyConfirmation)
                .SetProperty(x => x.TeacherCompletedAtUtc, legacyConfirmation));
            Assert.False(await Complete(reverseRelationship.Id, reverseSessionId));
            var legacy = await ReadSession(reverseSessionId);
            Assert.Equal(legacyConfirmation, legacy.LearnerCompletedAtUtc);
            Assert.Equal(legacyConfirmation, legacy.TeacherCompletedAtUtc);
            Assert.Equal(legacyConfirmation, legacy.CompletedAtUtc);
            var completed = await ReadSession(secondId);
            Assert.Equal(SessionStatus.Completed, completed.Status);
            Assert.Null(completed.LearnerCompletedAtUtc);
            Assert.Null(completed.TeacherCompletedAtUtc);
            Assert.Equal(clock.Now, completed.CompletedAtUtc);
            Assert.False(await Complete(relationship.Id, secondId));

            var expired = await db.SessionAbsenceRequests.AsNoTracking().SingleAsync(a => a.LearningSessionId == secondId &&
                a.Status == SessionAbsenceRequestStatus.ExpiredUnresolved);
            Assert.Null(expired.ResolvedByUserId);
            var chatServices = new ServiceCollection();
            chatServices.AddLogging();
            chatServices.AddAuthorizationCore();
            chatServices.AddSingleton<IAuthorizationHandler, RequestAuthorization>();
            await using var chatProvider = chatServices.BuildServiceProvider();
            var chat = new LearningChatController(db, new PeerSessionService(db, clock),
                chatProvider.GetRequiredService<IAuthorizationService>(), clock)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, learner.ToString()), new Claim(ClaimTypes.Role, "Member")], "test"))
                } }
            };
            Assert.IsType<JsonResult>(await chat.Send(relationship.Id, "After automatic completion", default));
            Assert.Null(await Act(learner, "request"));
            Assert.Null(await Act(teacher, "end"));
            Assert.IsType<ConflictObjectResult>(await chat.Send(relationship.Id, "After closure", default));
            Assert.Equal(clock.Now, await db.LearningRequests.Where(r => r.Id == relationship.Id).Select(r => r.EndedAtUtc).SingleAsync());
            Assert.Single(await Requests().Where(r => r.Status == LearningSessionRequestStatus.ClosedWithRelationship).ToListAsync());
            Assert.NotNull(await Act(learner, "request"));
            Assert.NotNull(await Act(teacher, "review", secondId, input: new() { Rating = 5 }));
            Assert.NotNull(await Act(learner, "review", secondId, input: new() { Rating = 6 }));
            var reviewGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var reviewReady = 0;
            async Task<string?> SubmitReview()
            {
                if (Interlocked.Increment(ref reviewReady) == 2) reviewGate.TrySetResult();
                await reviewGate.Task.WaitAsync(TimeSpan.FromSeconds(10));
                return await Act(learner, "review", secondId, input: new() { Rating = 5, Comment = "Good session" });
            }
            Assert.Single(await Task.WhenAll(SubmitReview(), SubmitReview()), error => error is null);
            Assert.NotNull(await Act(learner, "review", secondId, input: new() { Rating = 4 }));
            var review = await db.Reviews.AsNoTracking().SingleAsync(r => r.LearningSessionId == secondId);
            Assert.Equal(teacher, review.ReviewedUserId);
            Assert.Equal(learner, review.ReviewerId);
            var discovery = await new SkillDiscoveryService(db).DiscoverAsync(skill.Slug, learner, new());
            var rating = Assert.Single(discovery!.Partners, partner => partner.UserId == teacher);
            Assert.Equal(5, rating.Rating);
            Assert.Equal(1, rating.ReviewCount);
            Assert.Equal(1, rating.CompletedTeachingSessions);
            Assert.Null(rating.AcceptedRelationshipId);
            Assert.Null(await new PeerRequestService(db).FindActiveIdAsync(learner, teacher, skill.Id, default));

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorizationCore();
            services.AddSingleton<IAuthorizationHandler, RequestAuthorization>();
            await using var provider = services.BuildServiceProvider();
            var controller = new SessionsController(db, provider.GetRequiredService<IAuthorizationService>(),
                new PeerSessionService(db, clock), clock,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SessionsController>.Instance);
            void SignIn(Guid actor)
            {
                controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, actor.ToString()), new Claim(ClaimTypes.Role, "Member")], "test"))
                } };
            }
            SignIn(learner);
            var view = Assert.IsType<ViewResult>(await controller.SessionDetails(secondId, default));
            var history = Assert.IsType<PeerSessionPage>(view.Model);
            Assert.Equal((byte?)5, Assert.Single(history.Sessions).Review?.Rating);
            Assert.Equal(2, Assert.Single(history.Sessions).History.Count);
            SignIn(teacher);
            Assert.IsType<ViewResult>(await controller.SessionReview(secondId, default));
            SignIn(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(await controller.SessionDetails(secondId, default));
            Assert.IsType<NotFoundResult>(await controller.Change(relationship.Id, secondId, null, "review", new() { Rating = 1 }, default));
            Assert.IsType<BadRequestResult>(await controller.Change(relationship.Id, secondId, null, "confirm", new(), default));

            await using var reopenedConnection = await OpenAsync();
            await using var reopenedContext = Context(reopenedConnection);
            var reopened = await new PeerRequestService(reopenedContext).CreateAsync(learner,
                new PeerRequestInput { TeacherId = teacher, SkillId = skill.Id, Message = "After closure" }, default);
            Assert.NotNull(reopened.Id);

            var closureGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var closureReady = 0;
            reverseSlot.LocalStart = "2090-01-05T15:00";
            async Task<string?> RaceClosure(string action)
            {
                await using var sql = await OpenAsync();
                await using var context = Context(sql);
                if (Interlocked.Increment(ref closureReady) == 2) closureGate.TrySetResult();
                await closureGate.Task.WaitAsync(TimeSpan.FromSeconds(10));
                return await new PeerSessionService(context, clock).ChangeAsync(
                    reverseRelationship.Id, null, null, learner, action, reverseSlot, default);
            }
            var closure = await Task.WhenAll(RaceClosure("end"), RaceClosure("schedule"));
            Assert.Single(closure, error => error is null);
            var reverseClosed = await db.LearningRequests.Where(r => r.Id == reverseRelationship.Id)
                .Select(r => r.EndedAtUtc != null).SingleAsync();
            var newCommitment = await db.LearningSessions.AnyAsync(s => s.LearningRequestId == reverseRelationship.Id &&
                s.Status == SessionStatus.Scheduled);
            Assert.NotEqual(reverseClosed, newCommitment);

            // A newly started worker must catch an overdue session without a browser.
            // Use a timestamp well before the shared fixtures and assert the sweep cannot touch them.
            var restartClock = new SessionTestClock(new DateTimeOffset(1900, 1, 2, 0, 0, 0, TimeSpan.Zero));
            Assert.False(await db.LearningSessions.AnyAsync(x =>
                x.Status == SessionStatus.Scheduled && x.StartsAt <= restartClock.Now));
            var recoveryRequest = new LearningRequest
            {
                LearnerId = teacher, TeacherId = learner, SkillId = skill.Id, Status = RequestStatus.Accepted,
                EndedAtUtc = null
            };
            var recoverySession = new LearningSession
            {
                LearningRequest = recoveryRequest, StartsAt = restartClock.Now.AddHours(-1), DurationMinutes = 30
            };
            try
            {
                // Use a separate skill so the fixture cannot conflict with the accepted pair.
                recoveryRequest.SkillId = await db.Skills.Where(x => x.Id != skill.Id).Select(x => x.Id).FirstAsync();
                db.LearningSessions.Add(recoverySession);
                await db.SaveChangesAsync();
                var workerServices = new ServiceCollection();
                workerServices.AddLogging();
                workerServices.AddSingleton<TimeProvider>(restartClock);
                workerServices.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Connection));
                workerServices.AddScoped<PeerSessionService>();
                await using var workerProvider = workerServices.BuildServiceProvider();
                using var worker = new SessionCompletionWorker(workerProvider.GetRequiredService<IServiceScopeFactory>(),
                    restartClock, Microsoft.Extensions.Options.Options.Create(new SessionCompletionOptions()),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<SessionCompletionWorker>.Instance);
                await worker.StartAsync(default);
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    while ((await ReadSession(recoverySession.Id)).Status == SessionStatus.Scheduled)
                        await Task.Delay(25, timeout.Token);
                    var recovered = await ReadSession(recoverySession.Id);
                    Assert.Equal(SessionStatus.Completed, recovered.Status);
                    Assert.Equal(restartClock.Now, recovered.CompletedAtUtc);
                    Assert.Null(recovered.LearnerCompletedAtUtc);
                    Assert.Null(recovered.TeacherCompletedAtUtc);
                }
                finally
                {
                    await worker.StopAsync(default);
                }
                var raceSession = new LearningSession
                {
                    LearningRequestId = recoveryRequest.Id, StartsAt = restartClock.Now.AddHours(-1), DurationMinutes = 30
                };
                db.LearningSessions.Add(raceSession);
                await db.SaveChangesAsync();

                async Task<string?> RaceEnd()
                {
                    await using var sql = await OpenAsync();
                    await using var workerDb = Context(sql);
                    return await new PeerSessionService(workerDb, restartClock)
                        .ChangeAsync(recoveryRequest.Id, null, null, learner, "end", new(), default);
                }

                var automatic = Complete(recoveryRequest.Id, raceSession.Id);
                var close = RaceEnd();
                await Task.WhenAll(automatic, close);
                Assert.True(await automatic);
                Assert.Equal(SessionStatus.Completed, (await ReadSession(raceSession.Id)).Status);
                if (await close is not null)
                    Assert.Null(await RaceEnd());
            }
            finally
            {
                await db.LearningSessions.Where(x => x.LearningRequestId == recoveryRequest.Id).ExecuteDeleteAsync();
                await db.LearningRequests.Where(x => x.Id == recoveryRequest.Id).ExecuteDeleteAsync();
            }
        }
        finally
        {
            await db.Reviews.Where(r => r.LearningSession.LearningRequestId == relationship.Id).ExecuteDeleteAsync();
            var sessionIds = db.LearningSessions.Where(s => s.LearningRequestId == relationship.Id).Select(s => s.Id);
            await db.SessionAbsenceRequests.Where(a => sessionIds.Contains(a.LearningSessionId)).ExecuteDeleteAsync();
            await db.SessionScheduleHistory.Where(h => sessionIds.Contains(h.LearningSessionId)).ExecuteDeleteAsync();
            await db.LearningSessionRequests.Where(r => r.LearningRequestId == relationship.Id).ExecuteDeleteAsync();
            await db.LearningSessions.Where(s => s.LearningRequestId == relationship.Id || s.LearningRequestId == reverseRelationship.Id).ExecuteDeleteAsync();
            await db.LearningRequests.Where(r => (r.LearnerId == learner && r.TeacherId == teacher) ||
                (r.LearnerId == teacher && r.TeacherId == learner)).ExecuteDeleteAsync();
            await db.UserSkills.Where(s => s.UserId == teacher).ExecuteDeleteAsync();
            await db.UserRoles.Where(x => x.UserId == learner || x.UserId == teacher).ExecuteDeleteAsync();
            await db.Users.Where(x => x.Id == learner || x.Id == teacher).ExecuteDeleteAsync();
            if (temporaryRole) await db.Roles.Where(x => x.Id == role.Id).ExecuteDeleteAsync();
        }
    }

    private sealed class SessionTestClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
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
