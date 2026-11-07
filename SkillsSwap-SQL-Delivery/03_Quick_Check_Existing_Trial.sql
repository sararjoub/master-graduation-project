USE [SkillsSwap_Trial];
GO
-- All three results should be zero.
SELECT
 (SELECT COUNT(*) FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) AS BadForeignKeys,
 (SELECT COUNT(*) FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) AS BadChecks,
 (SELECT COUNT(*) FROM sys.indexes WHERE is_disabled=1 AND is_hypothetical=0) AS DisabledIndexes;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS, NO_INFOMSGS;
