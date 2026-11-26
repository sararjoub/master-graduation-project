using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkillsSwap.Controllers;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services;

namespace SkillsSwap.Tests;

public sealed partial class SqlServerPhase04Tests
{
    [SqlServerPhase04Fact]
    public async Task Phase05_meeting_and_chat_enforce_relationship_and_serialize_writes_in_SQL_Server()
    {
        await using var connection = await OpenAsync();
        await using var db = Context(connection);
        var learner = Guid.NewGuid();
        var teacher = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var role = await db.Roles.SingleOrDefaultAsync(r => r.NormalizedName == "MEMBER");
        var temporaryRole = role is null;
        role ??= new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Member", NormalizedName = "MEMBER" };
        if (temporaryRole) db.Roles.Add(role);
        db.Users.AddRange(Member(learner), Member(teacher), Member(outsider));
        db.UserRoles.AddRange(new() { UserId = learner, RoleId = role.Id }, new() { UserId = teacher, RoleId = role.Id },
            new() { UserId = outsider, RoleId = role.Id });
        var skill = await db.Skills.FirstAsync();
        var request = new LearningRequest { LearnerId = learner, TeacherId = teacher, SkillId = skill.Id, Status = RequestStatus.Accepted };
        var pending = new LearningRequest { LearnerId = teacher, TeacherId = learner, SkillId = skill.Id, Status = RequestStatus.Pending };
        var clock = new SessionTestClock(new DateTimeOffset(2090, 2, 1, 10, 0, 0, TimeSpan.Zero));
        var meeting = new LearningSession { LearningRequest = request, StartsAt = clock.Now.AddMinutes(30), DurationMinutes = 30 };
        db.LearningRequests.AddRange(request, pending);
        db.LearningSessions.Add(meeting);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore();
        services.AddSingleton<IAuthorizationHandler, RequestAuthorization>();
        await using var provider = services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthorizationService>();

        T WithActor<T>(T controller, Guid actor) where T : Controller
        {
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, actor.ToString()), new Claim(ClaimTypes.Role, "Member")], "test"))
            } };
            return controller;
        }

        async Task<IActionResult> Meet(Guid actor, bool post, long? id = null)
        {
            await using var sql = await OpenAsync();
            await using var worker = Context(sql);
            var controller = WithActor(new VideoMeetingController(worker, new PeerSessionService(worker, clock),
                auth, Options.Create(new VideoMeetingOptions()), clock), actor);
            return post ? await controller.Open(id ?? meeting.Id, default) : await controller.Index(id ?? meeting.Id, default);
        }

        async Task<IActionResult> Chat(Guid actor, Func<LearningChatController, Task<IActionResult>> action)
        {
            await using var sql = await OpenAsync();
            await using var worker = Context(sql);
            return await action(WithActor(new LearningChatController(worker, new PeerSessionService(worker, clock), auth, clock), actor));
        }

        async Task<string?> Room() => await db.LearningSessions.Where(s => s.Id == meeting.Id)
            .Select(s => s.MeetingProviderId).SingleAsync();

        try
        {
            Assert.IsType<NotFoundResult>(await Meet(outsider, true));
            Assert.IsType<NotFoundResult>(await Meet(teacher, true, -1));
            Assert.IsType<RedirectToActionResult>(await Meet(teacher, true));
            Assert.Null(Assert.IsType<VideoMeetingPage>(Assert.IsType<ViewResult>(await Meet(learner, false)).Model).Room);
            Assert.Null(await Room());

            clock.Now = clock.Now.AddMinutes(20);
            await Meet(teacher, false);
            Assert.Null(await Room());
            Assert.All(await Task.WhenAll(Meet(teacher, true), Meet(learner, true)), response =>
                Assert.Equal(nameof(VideoMeetingController.Index), Assert.IsType<RedirectToActionResult>(response).ActionName));
            var room = Assert.IsType<string>(await Room());
            Assert.Matches("^skillswap-jitsi-[0-9a-f]{32}$", room);
            Assert.Equal(room, Assert.IsType<VideoMeetingPage>(Assert.IsType<ViewResult>(await Meet(learner, false)).Model).Room);

            await db.LearningSessions.Where(s => s.Id == meeting.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(s => s.StartsAt, clock.Now.AddDays(1)));
            Assert.Null(Assert.IsType<VideoMeetingPage>(Assert.IsType<ViewResult>(await Meet(learner, false)).Model).Room);
            clock.Now = clock.Now.AddDays(1);
            Assert.Equal(room, Assert.IsType<VideoMeetingPage>(Assert.IsType<ViewResult>(await Meet(teacher, false)).Model).Room);
            clock.Now = clock.Now.AddMinutes(46);
            Assert.Null(Assert.IsType<VideoMeetingPage>(Assert.IsType<ViewResult>(await Meet(teacher, false)).Model).Room);
            Assert.Equal(SessionStatus.Scheduled, await db.LearningSessions.Where(s => s.Id == meeting.Id).Select(s => s.Status).SingleAsync());
            await db.LearningSessions.Where(s => s.Id == meeting.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(s => s.Status, SessionStatus.Cancelled));

            Assert.IsType<NotFoundResult>(await Chat(outsider, c => c.Messages(request.Id, null, null, default)));
            Assert.IsType<NotFoundResult>(await Chat(teacher, c => c.Messages(pending.Id, null, null, default)));
            await db.LearningRequests.Where(r => r.Id == pending.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(r => r.Status, RequestStatus.Declined));
            Assert.IsType<NotFoundResult>(await Chat(teacher, c => c.Messages(pending.Id, null, null, default)));
            Assert.IsType<ViewResult>(await Chat(learner, c => c.Index(request.Id, default)));
            Assert.IsType<BadRequestObjectResult>(await Chat(learner, c => c.Send(request.Id, " ", default)));
            Assert.IsType<BadRequestObjectResult>(await Chat(learner, c => c.Send(request.Id, new string('a', 4001), default)));

            const string body = "<b>plain text</b>";
            var first = Assert.IsType<ChatMessage>(Assert.IsType<JsonResult>(
                await Chat(learner, c => c.Send(request.Id, body, default))).Value);
            var second = Assert.IsType<ChatMessage>(Assert.IsType<JsonResult>(
                await Chat(learner, c => c.Send(request.Id, "second", default))).Value);
            var stored = await db.Messages.AsNoTracking().SingleAsync(m => m.Id == first.Id);
            Assert.Equal(learner, stored.SenderId);
            Assert.Equal(body, stored.Body);
            var page = Assert.IsType<JsonResult>(await Chat(teacher, c => c.Messages(request.Id, first.Id, null, default)));
            var rows = Assert.IsAssignableFrom<IEnumerable<ChatMessage>>(page.Value!.GetType().GetProperty("messages")!.GetValue(page.Value));
            Assert.Equal(second.Id, Assert.Single(rows).Id);
            Assert.Equal(2, Assert.IsType<JsonResult>(await Chat(teacher, c => c.Unread(default))).Value);
            Assert.IsType<NoContentResult>(await Chat(learner, c => c.Read(request.Id, [first.Id], default)));
            Assert.Null(await db.Messages.Where(m => m.Id == first.Id).Select(m => m.ReadAtUtc).SingleAsync());
            Assert.IsType<NoContentResult>(await Chat(teacher, c => c.Read(request.Id, [first.Id], default)));
            var readAt = await db.Messages.Where(m => m.Id == first.Id).Select(m => m.ReadAtUtc).SingleAsync();
            Assert.NotNull(readAt);
            clock.Now = clock.Now.AddMinutes(1);
            await Chat(teacher, c => c.Read(request.Id, [first.Id], default));
            Assert.Equal(readAt, await db.Messages.Where(m => m.Id == first.Id).Select(m => m.ReadAtUtc).SingleAsync());

            await db.Users.Where(u => u.Id == learner).ExecuteUpdateAsync(set => set.SetProperty(u => u.Status, AccountStatus.Suspended));
            Assert.IsType<NotFoundResult>(await Meet(learner, false));
            Assert.IsType<NotFoundResult>(await Chat(learner, c => c.Messages(request.Id, null, null, default)));
            Assert.IsType<JsonResult>(await Chat(teacher, c => c.Messages(request.Id, null, null, default)));
            Assert.IsType<ConflictObjectResult>(await Chat(teacher, c => c.Send(request.Id, "suspended partner", default)));
            await db.Users.Where(u => u.Id == learner).ExecuteUpdateAsync(set => set.SetProperty(u => u.Status, AccountStatus.Active));

            async Task<string?> Close()
            {
                await using var sql = await OpenAsync();
                await using var worker = Context(sql);
                return await new PeerSessionService(worker, clock).ChangeAsync(request.Id, null, null, teacher, "end", new(), default);
            }

            var racingSend = Chat(learner, c => c.Send(request.Id, "race", default));
            var closing = Close();
            await Task.WhenAll(racingSend, closing);
            Assert.Null(closing.Result);
            var raceCount = await db.Messages.CountAsync(m => m.LearningRequestId == request.Id && m.Body == "race");
            if (racingSend.Result is JsonResult) Assert.Equal(1, raceCount);
            else
            {
                Assert.IsType<ConflictObjectResult>(racingSend.Result);
                Assert.Equal(0, raceCount);
            }
            Assert.IsType<ConflictObjectResult>(await Chat(learner, c => c.Send(request.Id, "after closure", default)));
            Assert.IsType<JsonResult>(await Chat(learner, c => c.Messages(request.Id, null, null, default)));
            Assert.Null(Assert.IsType<VideoMeetingPage>(Assert.IsType<ViewResult>(await Meet(teacher, false)).Model).Room);
        }
        finally
        {
            await db.Messages.Where(m => m.LearningRequestId == request.Id).ExecuteDeleteAsync();
            await db.LearningSessions.Where(s => s.Id == meeting.Id).ExecuteDeleteAsync();
            await db.LearningRequests.Where(r => r.Id == request.Id || r.Id == pending.Id).ExecuteDeleteAsync();
            await db.UserRoles.Where(r => r.UserId == learner || r.UserId == teacher || r.UserId == outsider).ExecuteDeleteAsync();
            await db.Users.Where(u => u.Id == learner || u.Id == teacher || u.Id == outsider).ExecuteDeleteAsync();
            if (temporaryRole) await db.Roles.Where(r => r.Id == role.Id).ExecuteDeleteAsync();
        }
    }
}
