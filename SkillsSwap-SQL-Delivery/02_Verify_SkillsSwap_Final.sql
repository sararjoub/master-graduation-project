/* Read-only checks for the delivery database. Does not change any rows. */
USE [SkillsSwap_Final];
GO
SET NOCOUNT ON;
IF DB_NAME()<>N'SkillsSwap_Final' THROW 51100,'Wrong database.',1;
SELECT DB_NAME() AS DatabaseName,
 (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) AS TablesCount,
 (SELECT COUNT(*) FROM sys.foreign_keys) AS ForeignKeysCount,
 (SELECT COUNT(*) FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) AS DisabledOrUntrustedFKs,
 (SELECT COUNT(*) FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) AS DisabledOrUntrustedChecks;

DECLARE @Expected TABLE(TableName sysname PRIMARY KEY, ExpectedRecords bigint);
INSERT @Expected VALUES
(N'AspNetUsers',5),
(N'AspNetRoles',2),
(N'AspNetUserRoles',5),
(N'AspNetUserClaims',0),
(N'AspNetRoleClaims',0),
(N'AspNetUserLogins',0),
(N'AspNetUserTokens',0),
(N'UserProfiles',5),
(N'Categories',6),
(N'SubCategories',7),
(N'Skills',19),
(N'UserSkills',15),
(N'UserAvailability',28),
(N'ExpertApplications',4),
(N'ExpertSkills',2),
(N'SubscriptionPlans',2),
(N'PlanPrices',2),
(N'UserSubscriptions',2),
(N'Payments',3),
(N'ExpertCreditGrants',3),
(N'LearningRequests',5),
(N'ExpertCreditReservations',3),
(N'LearningSessions',5),
(N'SessionConfirmations',6),
(N'LearningSessionChanges',3),
(N'Conversations',4),
(N'ConversationParticipants',8),
(N'Messages',8),
(N'SessionReviews',6),
(N'CareerGoals',6),
(N'UserCareerPaths',3),
(N'CareerPathKnownSkills',2),
(N'CareerPathSteps',14),
(N'CareerStepSessions',1),
(N'PaymentProviderEvents',3),
(N'PaymentRefunds',1);
DECLARE @Actual TABLE(TableName sysname PRIMARY KEY, ActualRecords bigint);
INSERT @Actual
SELECT N'AspNetUsers', COUNT_BIG(*) FROM dbo.[AspNetUsers]
UNION ALL
SELECT N'AspNetRoles', COUNT_BIG(*) FROM dbo.[AspNetRoles]
UNION ALL
SELECT N'AspNetUserRoles', COUNT_BIG(*) FROM dbo.[AspNetUserRoles]
UNION ALL
SELECT N'AspNetUserClaims', COUNT_BIG(*) FROM dbo.[AspNetUserClaims]
UNION ALL
SELECT N'AspNetRoleClaims', COUNT_BIG(*) FROM dbo.[AspNetRoleClaims]
UNION ALL
SELECT N'AspNetUserLogins', COUNT_BIG(*) FROM dbo.[AspNetUserLogins]
UNION ALL
SELECT N'AspNetUserTokens', COUNT_BIG(*) FROM dbo.[AspNetUserTokens]
UNION ALL
SELECT N'UserProfiles', COUNT_BIG(*) FROM dbo.[UserProfiles]
UNION ALL
SELECT N'Categories', COUNT_BIG(*) FROM dbo.[Categories]
UNION ALL
SELECT N'SubCategories', COUNT_BIG(*) FROM dbo.[SubCategories]
UNION ALL
SELECT N'Skills', COUNT_BIG(*) FROM dbo.[Skills]
UNION ALL
SELECT N'UserSkills', COUNT_BIG(*) FROM dbo.[UserSkills]
UNION ALL
SELECT N'UserAvailability', COUNT_BIG(*) FROM dbo.[UserAvailability]
UNION ALL
SELECT N'ExpertApplications', COUNT_BIG(*) FROM dbo.[ExpertApplications]
UNION ALL
SELECT N'ExpertSkills', COUNT_BIG(*) FROM dbo.[ExpertSkills]
UNION ALL
SELECT N'SubscriptionPlans', COUNT_BIG(*) FROM dbo.[SubscriptionPlans]
UNION ALL
SELECT N'PlanPrices', COUNT_BIG(*) FROM dbo.[PlanPrices]
UNION ALL
SELECT N'UserSubscriptions', COUNT_BIG(*) FROM dbo.[UserSubscriptions]
UNION ALL
SELECT N'Payments', COUNT_BIG(*) FROM dbo.[Payments]
UNION ALL
SELECT N'ExpertCreditGrants', COUNT_BIG(*) FROM dbo.[ExpertCreditGrants]
UNION ALL
SELECT N'LearningRequests', COUNT_BIG(*) FROM dbo.[LearningRequests]
UNION ALL
SELECT N'ExpertCreditReservations', COUNT_BIG(*) FROM dbo.[ExpertCreditReservations]
UNION ALL
SELECT N'LearningSessions', COUNT_BIG(*) FROM dbo.[LearningSessions]
UNION ALL
SELECT N'SessionConfirmations', COUNT_BIG(*) FROM dbo.[SessionConfirmations]
UNION ALL
SELECT N'LearningSessionChanges', COUNT_BIG(*) FROM dbo.[LearningSessionChanges]
UNION ALL
SELECT N'Conversations', COUNT_BIG(*) FROM dbo.[Conversations]
UNION ALL
SELECT N'ConversationParticipants', COUNT_BIG(*) FROM dbo.[ConversationParticipants]
UNION ALL
SELECT N'Messages', COUNT_BIG(*) FROM dbo.[Messages]
UNION ALL
SELECT N'SessionReviews', COUNT_BIG(*) FROM dbo.[SessionReviews]
UNION ALL
SELECT N'CareerGoals', COUNT_BIG(*) FROM dbo.[CareerGoals]
UNION ALL
SELECT N'UserCareerPaths', COUNT_BIG(*) FROM dbo.[UserCareerPaths]
UNION ALL
SELECT N'CareerPathKnownSkills', COUNT_BIG(*) FROM dbo.[CareerPathKnownSkills]
UNION ALL
SELECT N'CareerPathSteps', COUNT_BIG(*) FROM dbo.[CareerPathSteps]
UNION ALL
SELECT N'CareerStepSessions', COUNT_BIG(*) FROM dbo.[CareerStepSessions]
UNION ALL
SELECT N'PaymentProviderEvents', COUNT_BIG(*) FROM dbo.[PaymentProviderEvents]
UNION ALL
SELECT N'PaymentRefunds', COUNT_BIG(*) FROM dbo.[PaymentRefunds];
SELECT e.TableName,e.ExpectedRecords,a.ActualRecords,
 CASE WHEN a.ActualRecords=e.ExpectedRecords THEN 'PASS' ELSE 'REVIEW' END AS Result
FROM @Expected e LEFT JOIN @Actual a ON a.TableName=e.TableName ORDER BY e.TableName;
SELECT SUM(ExpectedRecords) AS ExpectedTotal,
 (SELECT SUM(ActualRecords) FROM @Actual) AS ActualTotal FROM @Expected;

DECLARE @Rules TABLE(CheckName nvarchar(150),Violations bigint);
INSERT @Rules
SELECT N'Expert approval belongs to the same user and skill', COUNT_BIG(*)
FROM dbo.ExpertSkills e JOIN dbo.ExpertApplications a ON a.ExpertApplicationId=e.ApprovedApplicationId
WHERE a.ApplicantUserId<>e.UserId OR a.SkillId<>e.SkillId OR a.StatusCode<>'Approved'
UNION ALL
SELECT N'Session belongs to the request participants and direction',COUNT_BIG(*)
FROM dbo.LearningSessions s JOIN dbo.LearningRequests r ON r.RequestId=s.RequestId
WHERE NOT ((s.TeacherUserId=r.ProviderUserId AND s.LearnerUserId=r.RequesterUserId AND s.SkillId=r.SkillId)
 OR (r.RequestType='Exchange' AND s.TeacherUserId=r.RequesterUserId AND s.LearnerUserId=r.ProviderUserId AND s.SkillId=r.OfferedSkillId))
UNION ALL
SELECT N'Completed session confirmed by both participants',COUNT_BIG(*)
FROM dbo.LearningSessions s WHERE s.StatusCode='Completed' AND
 (SELECT COUNT(*) FROM dbo.SessionConfirmations c WHERE c.SessionId=s.SessionId AND c.UserId IN (s.TeacherUserId,s.LearnerUserId) AND c.Attended=1 AND c.CompletionConfirmedAtUtc IS NOT NULL)<>2
UNION ALL
SELECT N'Expert credit belongs to the correct request and subscriber',COUNT_BIG(*)
FROM dbo.ExpertCreditReservations c JOIN dbo.ExpertCreditGrants g ON g.CreditGrantId=c.CreditGrantId
JOIN dbo.UserSubscriptions u ON u.SubscriptionId=g.SubscriptionId JOIN dbo.LearningRequests r ON r.RequestId=c.RequestId
WHERE r.RequestType<>'Expert' OR u.UserId<>r.RequesterUserId
UNION ALL
SELECT N'No grant allocates more than its quantity',COUNT_BIG(*) FROM dbo.ExpertCreditGrants g
WHERE (SELECT COUNT(*) FROM dbo.ExpertCreditReservations c WHERE c.CreditGrantId=g.CreditGrantId AND c.StatusCode IN ('Reserved','Consumed'))>g.Quantity
UNION ALL
SELECT N'Reviews target the other participant after completion',COUNT_BIG(*)
FROM dbo.SessionReviews v JOIN dbo.LearningSessions s ON s.SessionId=v.SessionId
WHERE s.StatusCode<>'Completed' OR NOT ((v.ReviewerUserId=s.TeacherUserId AND v.ReviewedUserId=s.LearnerUserId) OR (v.ReviewerUserId=s.LearnerUserId AND v.ReviewedUserId=s.TeacherUserId))
UNION ALL
SELECT N'Career step session belongs to the learner and skill',COUNT_BIG(*)
FROM dbo.CareerStepSessions x JOIN dbo.CareerPathSteps st ON st.CareerPathStepId=x.CareerPathStepId
JOIN dbo.UserCareerPaths p ON p.CareerPathId=st.CareerPathId JOIN dbo.LearningSessions s ON s.SessionId=x.SessionId
WHERE s.LearnerUserId<>p.UserId OR s.SkillId<>st.SkillId OR s.StatusCode<>'Completed'
UNION ALL
SELECT N'Completed steps do not skip unfinished previous steps',COUNT_BIG(*) FROM dbo.CareerPathSteps st
WHERE st.CompletedAtUtc IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.CareerPathSteps previous WHERE previous.CareerPathId=st.CareerPathId AND previous.StepOrder<st.StepOrder AND previous.CompletedAtUtc IS NULL)
UNION ALL
SELECT N'Completed step has enough completed sessions',COUNT_BIG(*) FROM dbo.CareerPathSteps st
WHERE st.CompletedAtUtc IS NOT NULL AND (SELECT COUNT(*) FROM dbo.CareerStepSessions x JOIN dbo.LearningSessions s ON s.SessionId=x.SessionId WHERE x.CareerPathStepId=st.CareerPathStepId AND s.StatusCode='Completed')<st.RequiredCompletedSessions
UNION ALL
SELECT N'Conversation has exactly the two request participants',COUNT_BIG(*) FROM dbo.Conversations c JOIN dbo.LearningRequests r ON r.RequestId=c.RequestId
WHERE (SELECT COUNT(*) FROM dbo.ConversationParticipants p WHERE p.ConversationId=c.ConversationId)<>2
 OR EXISTS(SELECT 1 FROM dbo.ConversationParticipants p WHERE p.ConversationId=c.ConversationId AND p.UserId NOT IN (r.RequesterUserId,r.ProviderUserId))
UNION ALL
SELECT N'Refunds do not exceed the original payment',COUNT_BIG(*) FROM dbo.Payments p
WHERE (SELECT ISNULL(SUM(r.Amount),0) FROM dbo.PaymentRefunds r WHERE r.PaymentId=p.PaymentId AND r.StatusCode='Succeeded')>p.Amount
UNION ALL
SELECT N'No overlapping active sessions for a participant',COUNT_BIG(*) FROM dbo.LearningSessions a JOIN dbo.LearningSessions b ON a.SessionId<b.SessionId AND a.StartsAtUtc<b.EndsAtUtc AND b.StartsAtUtc<a.EndsAtUtc
WHERE a.StatusCode IN ('Scheduled','InProgress','AwaitingConfirmation') AND b.StatusCode IN ('Scheduled','InProgress','AwaitingConfirmation')
 AND (a.TeacherUserId IN (b.TeacherUserId,b.LearnerUserId) OR a.LearnerUserId IN (b.TeacherUserId,b.LearnerUserId));
SELECT CheckName,Violations,CASE WHEN Violations=0 THEN 'PASS' ELSE 'FAIL' END AS Result FROM @Rules;
PRINT N'Read-only checks finished. Share the Results tab; expected totals are 36 tables, 69 FKs, 188 sample rows, and zero violations.';
