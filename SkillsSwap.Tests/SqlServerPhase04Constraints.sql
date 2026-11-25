SET NOCOUNT ON;
SET XACT_ABORT OFF;
IF (SELECT COUNT(*) FROM LearningRequests WHERE Message LIKE '04B legacy %') <> 4
 OR (SELECT COUNT(*) FROM Reviews WHERE Comment='04B legacy review') <> 1
 OR NOT EXISTS (SELECT 1 FROM LearningSessions WHERE Status=1 AND CreatedAtUtc IS NULL AND CompletedAtUtc IS NULL)
 THROW 51001,'Legacy preservation failed',1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('LearningRequests') AND name='IX_LearningRequests_LearnerId' AND has_filter=0 AND is_unique=0)
 THROW 51002,'General LearnerId index missing',1;
IF (SELECT COUNT(*) FROM sys.indexes WHERE name IN ('UX_LearningRequests_ActivePeer','UX_SessionScheduleProposals_Pending') AND has_filter=1 AND is_unique=1) <> 2
 THROW 51003,'Filtered indexes missing',1;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id IN (OBJECT_ID('LearningRequests'),OBJECT_ID('LearningSessions'),OBJECT_ID('SessionScheduleProposals'),OBJECT_ID('Reviews')) AND (is_disabled=1 OR is_not_trusted=1))
 THROW 51004,'Untrusted or disabled CHECK',1;
PRINT 'PASS legacy preservation and index metadata';
BEGIN TRANSACTION;
DECLARE @l uniqueidentifier='04000000-0000-0000-0000-000000000001', @t uniqueidentifier='04000000-0000-0000-0000-000000000002';
DECLARE @r bigint=(SELECT Id FROM LearningRequests WHERE Message='04B legacy 1');
DECLARE @s bigint=(SELECT Id FROM LearningSessions WHERE LearningRequestId=@r);
INSERT LearningSessions(LearningRequestId,StartsAt,DurationMinutes,Status) VALUES (@r,'2030-01-01T09:00:00+00:00',30,0);
DECLARE @s2 bigint=SCOPE_IDENTITY();
DECLARE @old binary(8)=(SELECT RowVersion FROM LearningSessions WHERE Id=@s2);
UPDATE LearningSessions SET DurationMinutes=45 WHERE Id=@s2 AND RowVersion=@old;
IF @@ROWCOUNT<>1 THROW 51005,'Fresh version update failed',1;
UPDATE LearningSessions SET DurationMinutes=60 WHERE Id=@s2 AND RowVersion=@old;
IF @@ROWCOUNT<>0 THROW 51006,'Stale version updated the row',1;
PRINT 'PASS multiple sessions and rowversion compare-and-update';
INSERT SessionScheduleProposals(LearningSessionId,Revision,ProposedByUserId,StartsAtUtc,DurationMinutes,CreatedAtUtc,Status,ResolvedByUserId,ResolvedAtUtc)
VALUES (@s2,1,@l,'2030-01-02T09:00:00+00:00',30,'2029-01-01T00:00:00+00:00',3,@l,'2029-01-02T00:00:00+00:00'),
(@s2,2,@t,'2030-01-03T09:00:00+00:00',45,'2029-01-01T00:00:00+00:00',0,NULL,NULL);
DECLARE @cases TABLE(Id int IDENTITY, Label varchar(80), SqlText nvarchar(max), Expected int);
INSERT @cases(Label,SqlText,Expected) VALUES
('duplicate Pending','INSERT LearningRequests(LearnerId,TeacherId,SkillId,IsExpertBooking,Message,Status,CreatedAtUtc) SELECT LearnerId,TeacherId,SkillId,0,''duplicate'',0,CreatedAtUtc FROM LearningRequests WHERE Message=''04B legacy 0''',2601),
('duplicate Accepted','INSERT LearningRequests(LearnerId,TeacherId,SkillId,IsExpertBooking,Message,Status,CreatedAtUtc) SELECT LearnerId,TeacherId,SkillId,0,''duplicate'',0,CreatedAtUtc FROM LearningRequests WHERE Id=@r',2601),
('end metadata mismatch','UPDATE LearningRequests SET EndedAtUtc=SYSDATETIMEOFFSET() WHERE Id=@r',547),
('invalid duration','UPDATE LearningSessions SET DurationMinutes=0 WHERE Id=@s2',547),
('invalid session status','UPDATE LearningSessions SET Status=99 WHERE Id=@s2',547),
('invalid session FK','UPDATE LearningSessions SET LearningRequestId=-1 WHERE Id=@s2',547),
('invalid cancellation metadata','UPDATE LearningSessions SET Status=2,CancellationReason=''partial'' WHERE Id=@s2',547),
('invalid completion metadata','UPDATE LearningSessions SET Status=1,CompletedAtUtc=SYSDATETIMEOFFSET() WHERE Id=@s2',547),
('duplicate Pending proposal','INSERT SessionScheduleProposals(LearningSessionId,Revision,ProposedByUserId,StartsAtUtc,DurationMinutes,CreatedAtUtc,Status) SELECT LearningSessionId,3,ProposedByUserId,StartsAtUtc,DurationMinutes,CreatedAtUtc,0 FROM SessionScheduleProposals WHERE LearningSessionId=@s2 AND Revision=2',2601),
('duplicate proposal revision','UPDATE SessionScheduleProposals SET Revision=1 WHERE LearningSessionId=@s2 AND Revision=2',2601),
('non-positive revision','UPDATE SessionScheduleProposals SET Revision=0 WHERE LearningSessionId=@s2 AND Revision=2',547),
('invalid proposal duration','UPDATE SessionScheduleProposals SET DurationMinutes=31 WHERE LearningSessionId=@s2 AND Revision=2',547),
('invalid proposal status','UPDATE SessionScheduleProposals SET Status=99 WHERE LearningSessionId=@s2 AND Revision=2',547),
('invalid resolution metadata','UPDATE SessionScheduleProposals SET ResolvedAtUtc=SYSDATETIMEOFFSET() WHERE LearningSessionId=@s2 AND Revision=2',547),
('self confirmation','UPDATE SessionScheduleProposals SET Status=1,ResolvedByUserId=ProposedByUserId,ResolvedAtUtc=DATEADD(day,1,CreatedAtUtc) WHERE LearningSessionId=@s2 AND Revision=2',547),
('second review','INSERT Reviews(LearningSessionId,ReviewerId,ReviewedUserId,Rating,CreatedAtUtc) SELECT LearningSessionId,ReviewedUserId,ReviewerId,4,CreatedAtUtc FROM Reviews WHERE LearningSessionId=@s',2601),
('invalid rating','UPDATE Reviews SET Rating=6 WHERE LearningSessionId=@s',547),
('invalid review FK','UPDATE Reviews SET LearningSessionId=-1 WHERE LearningSessionId=@s',547);
DECLARE @i int=1,@label varchar(80),@sql nvarchar(max),@expected int;
WHILE @i <= (SELECT COUNT(*) FROM @cases)
BEGIN
 SELECT @label=Label,@sql=SqlText,@expected=Expected FROM @cases WHERE Id=@i;
 BEGIN TRY
  EXEC sp_executesql @sql,N'@r bigint,@s bigint,@s2 bigint',@r,@s,@s2;
  THROW 51007,'Expected rejection did not occur',1;
 END TRY
 BEGIN CATCH
  IF ERROR_NUMBER()<>@expected THROW;
  PRINT CONCAT('PASS ',@label,' (',ERROR_NUMBER(),')');
 END CATCH;
 SET @i+=1;
END;
UPDATE LearningRequests SET EndedAtUtc=SYSDATETIMEOFFSET(),EndedByUserId=@t WHERE Id=@r;
INSERT LearningRequests(LearnerId,TeacherId,SkillId,IsExpertBooking,Message,Status,CreatedAtUtc)
SELECT LearnerId,TeacherId,SkillId,0,'new after ended',0,CreatedAtUtc FROM LearningRequests WHERE Id=@r;
INSERT LearningRequests(LearnerId,TeacherId,SkillId,IsExpertBooking,Message,Status,CreatedAtUtc)
SELECT LearnerId,TeacherId,SkillId,1,'expert excluded',0,CreatedAtUtc FROM LearningRequests WHERE Id=@r;
PRINT 'PASS ended relationship can reopen; expert excluded';
ROLLBACK;
