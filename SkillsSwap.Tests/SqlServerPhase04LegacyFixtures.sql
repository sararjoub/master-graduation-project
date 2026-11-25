SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @l uniqueidentifier='04000000-0000-0000-0000-000000000001', @t uniqueidentifier='04000000-0000-0000-0000-000000000002';
IF EXISTS (SELECT 1 FROM AspNetUsers WHERE Id IN (@l,@t)) THROW 51001,'Fixtures already exist',1;
INSERT AspNetUsers(Id,FullName,UserName,NormalizedUserName,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount,IsProfileComplete,Status,CreatedAtUtc)
VALUES (@l,N'04B Synthetic Learner',N'04b-learner',N'04B-LEARNER',1,0,0,0,0,1,0,SYSUTCDATETIME()),
(@t,N'04B Synthetic Teacher',N'04b-teacher',N'04B-TEACHER',1,0,0,0,0,1,0,SYSUTCDATETIME());
DECLARE @skill int=(SELECT MIN(Id) FROM Skills WHERE AccessTier=0 AND Status=1);
INSERT LearningRequests(LearnerId,TeacherId,SkillId,IsExpertBooking,Message,Status,CreatedAtUtc)
SELECT @l,@t,@skill,0,CONCAT('04B legacy ',v.Status),v.Status,SYSUTCDATETIME() FROM (VALUES(0),(2),(3)) v(Status);
INSERT LearningRequests(LearnerId,TeacherId,SkillId,IsExpertBooking,Message,Status,CreatedAtUtc)
VALUES (@t,@l,@skill,0,'04B legacy 1',1,SYSUTCDATETIME());
DECLARE @r bigint=SCOPE_IDENTITY();
INSERT LearningSessions(LearningRequestId,StartsAt,DurationMinutes,Status) VALUES (@r,'2025-01-01T09:00:00+00:00',45,1);
DECLARE @s bigint=SCOPE_IDENTITY();
INSERT Reviews(LearningSessionId,ReviewerId,ReviewedUserId,Rating,Comment,CreatedAtUtc)
VALUES (@s,@t,@l,4,'04B legacy review',SYSUTCDATETIME());
COMMIT;
