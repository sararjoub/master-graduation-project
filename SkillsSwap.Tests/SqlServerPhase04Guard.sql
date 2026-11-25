:ON ERROR EXIT
IF DB_NAME() <> N'SkillsSwap_Phase04_Integration'
    OR CAST(SERVERPROPERTY('ServerName') AS nvarchar(128)) <> N'DESKTOP-70O9F5H'
    THROW 51000, 'Integration database allowlist mismatch.', 1;
GO
