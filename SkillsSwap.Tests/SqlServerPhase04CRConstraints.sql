:ON ERROR EXIT
IF DB_NAME()<>N'SkillsSwap_Phase04_Integration' OR CAST(SERVERPROPERTY('ServerName') AS nvarchar(128))<>N'DESKTOP-70O9F5H' THROW 51000,'Unexpected target',1;
IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261125193300_Phase04CRDirectSchedulingFoundation') THROW 51000,'Candidate missing',1;
SET NOCOUNT ON;
SET XACT_ABORT OFF;
BEGIN TRANSACTION;
DECLARE @r bigint,@r2 bigint,@s bigint,@s2 bigint,@l uniqueidentifier,@t uniqueidentifier;
DECLARE @now datetimeoffset='2090-01-01T09:00:00+00:00', @start datetimeoffset='2090-01-02T09:00:00+00:00', @later datetimeoffset='2090-01-03T09:00:00+00:00';
SELECT @r=Id,@l=LearnerId,@t=TeacherId FROM LearningRequests WHERE Message='04B legacy 1';
SELECT @r2=Id FROM LearningRequests WHERE Message='04B legacy 0';
IF @r IS NULL OR @r2 IS NULL THROW 51000,'Expected isolated fixtures missing',1;
INSERT LearningSessions(LearningRequestId,StartsAt,DurationMinutes,Status,CreatedAtUtc,Title,LearningObjectives)
VALUES(@r,@start,30,0,@now,'04CR validation','Exercise schema and rollback');
SET @s=SCOPE_IDENTITY();
INSERT LearningSessions(LearningRequestId,StartsAt,DurationMinutes,Status,CreatedAtUtc,Title,LearningObjectives)
VALUES(@r,@later,45,0,@now,'04CR second session','Exercise one-to-many');
SET @s2=SCOPE_IDENTITY();
IF EXISTS(SELECT 1 FROM SessionScheduleProposals WHERE LearningSessionId IN(@s,@s2)) OR EXISTS(SELECT 1 FROM LearningSessions WHERE Id IN(@s,@s2) AND (CompletedAtUtc IS NOT NULL OR CancelledAtUtc IS NOT NULL OR RowVersion IS NULL)) THROW 51000,'Unexpected direct session metadata',1;
UPDATE LearningSessions SET Status=2,CancelledAtUtc=@now,CancelledByUserId=@t,CancellationReason='Isolated test' WHERE Id=@s2;
PRINT 'PASS direct sessions, existing cancellation metadata and rowversion';
DECLARE @cases TABLE(Id int IDENTITY,Label varchar(100),SqlText nvarchar(max),Expected int);
INSERT @cases(Label,SqlText,Expected) VALUES
('request pending',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,0,NULL,NULL,NULL)',0),
('request duplicate pending',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,0,NULL,NULL,NULL)',2601),
('request independent relationship',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r2,@l,@now,0,NULL,NULL,NULL)',0),
('request invalid pending metadata',N'UPDATE LearningSessionRequests SET FulfilledSessionId=@s,ResolvedAtUtc=@now,ResolvedByUserId=@t WHERE LearningRequestId=@r AND Status=0',547),
('request fulfill missing session',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,1,NULL,@now,@t)',547),
('request missing resolver',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,1,@s,@now,NULL)',547),
('request fulfilled by requester',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,1,@s,@now,@l)',547),
('request invalid withdrawal actor',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,2,NULL,@now,@t)',547),
('request withdrawn with session',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,2,@s,@now,@l)',547),
('request closed with session',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,3,@s,@now,@t)',547),
('request invalid status',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,9,NULL,NULL,NULL)',547),
('request reverse chronology',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@start,2,NULL,@now,@l)',547),
('request invalid FK',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(-1,@l,@now,0,NULL,NULL,NULL)',547),
('request fulfilled',N'UPDATE LearningSessionRequests SET Status=1,FulfilledSessionId=@s,ResolvedAtUtc=@now,ResolvedByUserId=@t WHERE LearningRequestId=@r AND Status=0',0),
('request duplicate fulfillment',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,1,@s,@now,@t)',2601),
('request withdrawn',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,2,NULL,@now,@l)',0),
('request closed',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,3,NULL,@now,@t)',0),
('request new after resolution',N'INSERT LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId) VALUES(@r,@l,@now,0,NULL,NULL,NULL)',0),
('absence pending',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''Cannot attend'',@now,@start,0,NULL,NULL)',0),
('absence duplicate pending',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''Cannot attend'',@now,@start,0,NULL,NULL)',2601),
('absence null reason',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,NULL,@now,@start,1,@now,@l)',515),
('absence empty reason',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,'''',@now,@start,1,@now,@l)',547),
('absence spaces only',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''   '',@now,@start,1,@now,@l)',547),
('absence overlong',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,REPLICATE(N''x'',501),@now,@start,1,@now,@l)',2628),
('absence tab only accepted SQL gap',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,NCHAR(9),@now,@start,1,@now,@l)',0),
('absence newline only accepted SQL gap',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,NCHAR(10),@now,@start,1,@now,@l)',0),
('absence NBSP only accepted SQL gap',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,NCHAR(160),@now,@start,1,@now,@l)',0),
('absence invalid status',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''Cannot attend'',@now,@start,9,NULL,NULL)',547),
('absence creation at start',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@start,@start,1,@start,@l)',547),
('absence reverse chronology',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,1,DATEADD(minute,-1,@now),@l)',547),
('absence late withdrawal',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,1,@start,@l)',547),
('absence early expiry',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,4,@now,NULL)',547),
('absence expiry with actor',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,4,@start,@t)',547),
('absence pending with resolution',N'UPDATE SessionAbsenceRequests SET ResolvedAtUtc=@now,ResolvedByUserId=@t WHERE LearningSessionId=@s AND Status=0',547),
('absence withdrawal actor',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,1,@now,@t)',547),
('absence cancellation by requester',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,2,@now,@l)',547),
('absence reschedule by requester',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,3,@now,@l)',547),
('absence invalid FK',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(-1,@l,''x'',@now,@start,0,NULL,NULL)',547),
('absence withdrawn',N'UPDATE SessionAbsenceRequests SET Status=1,ResolvedAtUtc=@now,ResolvedByUserId=@l WHERE LearningSessionId=@s AND Status=0',0),
('absence cancelled',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,2,@now,@t)',0),
('absence rescheduled',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,3,@now,@t)',0),
('absence expired no actor',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''x'',@now,@start,4,@start,NULL)',0),
('absence new after resolution',N'INSERT SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status,ResolvedAtUtc,ResolvedByUserId) VALUES(@s,@l,''Cannot attend'',@now,@start,0,NULL,NULL)',0),
('history time only',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(@s,@t,@start,@later,30,30,@now)',0),
('history duration only',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(@s,@t,@start,@start,30,45,@now)',0),
('history simultaneous legacy previous duration',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(@s,@t,@start,@later,35,60,@now)',0),
('history no change',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(@s,@t,@start,@start,30,30,@now)',547),
('history unsupported duration',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(@s,@t,@start,@later,30,35,@now)',547),
('history zero previous',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(@s,@t,@start,@later,0,30,@now)',547),
('history negative previous',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(@s,@t,@start,@later,-1,30,@now)',547),
('history changed at new start',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(@s,@t,@start,@later,30,45,@later)',547),
('history invalid session',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(-1,@t,@start,@later,30,45,@now)',547),
('history invalid actor',N'INSERT SessionScheduleHistory(LearningSessionId,ChangedByUserId,PreviousStartsAtUtc,NewStartsAtUtc,PreviousDurationMinutes,NewDurationMinutes,ChangedAtUtc) VALUES(@s,''00000000-0000-0000-0000-000000000000'',@start,@later,30,45,@now)',547),
('restrict request deletion',N'DELETE FROM LearningRequests WHERE Id=@r2',547),
('restrict session deletion',N'DELETE FROM LearningSessions WHERE Id=@s',547),
('restrict actor deletion',N'DELETE FROM AspNetUsers WHERE Id=@l',547);
DECLARE @i int=1,@label varchar(100),@sql nvarchar(max),@expected int;
WHILE @i <= (SELECT COUNT(*) FROM @cases)
BEGIN
 SELECT @label=Label,@sql=SqlText,@expected=Expected FROM @cases WHERE Id=@i;
 BEGIN TRY
  EXEC sp_executesql @sql,N'@r bigint,@r2 bigint,@s bigint,@l uniqueidentifier,@t uniqueidentifier,@now datetimeoffset,@start datetimeoffset,@later datetimeoffset',@r,@r2,@s,@l,@t,@now,@start,@later;
  IF @expected<>0 THROW 51001,'Expected rejection did not occur',1;
 END TRY
 BEGIN CATCH
  IF ERROR_NUMBER()<>@expected AND NOT (@expected=2628 AND ERROR_NUMBER()=8152)
  BEGIN
   PRINT @label;
   THROW;
  END;
 END CATCH;
 PRINT CONCAT('PASS ',@label);
 SET @i+=1;
END;
IF (SELECT COUNT(*) FROM SessionScheduleHistory WHERE LearningSessionId=@s)<>3 THROW 51000,'Missing schedule history',1;
SELECT Id FROM SessionScheduleHistory WHERE LearningSessionId=@s ORDER BY ChangedAtUtc,Id;
ROLLBACK;
IF EXISTS(SELECT 1 FROM LearningSessions WHERE Id IN(@s,@s2)) OR EXISTS(SELECT 1 FROM LearningSessionRequests) OR EXISTS(SELECT 1 FROM SessionAbsenceRequests) OR EXISTS(SELECT 1 FROM SessionScheduleHistory) THROW 51000,'Fixture transaction rollback failed',1;
PRINT 'PASS complete fixture transaction rollback';
