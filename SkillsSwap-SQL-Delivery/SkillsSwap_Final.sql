/* Skills Swap — DELIVERY SCHEMA + FICTIONAL DEMO DATA — v1.0
   Database: SkillsSwap_Final. Creates no objects in your other databases.
   SQL Server 2019+ / SSMS. Run the complete file with Execute (F5).
   @SeedDemoData=1 includes fictional trial accounts and business data.
   Demo password: SkillsSwap_Demo!2026 (NEVER use these accounts in production).
   Identity hashes were generated and verified with PasswordHasher<object>.
   No DROP, TRUNCATE, or destructive reset. A second run does not duplicate data. All inserts are transactional.
*/
USE [master];
GO
IF DB_ID(N'SkillsSwap_Final') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [SkillsSwap_Final]');
END;
GO
USE [SkillsSwap_Final];
GO
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

SET XACT_ABORT ON;
IF DB_NAME()<>N'SkillsSwap_Final' THROW 51000, 'Wrong database. Stop: SkillsSwap_Final is required.', 1;
DECLARE @SeedDemoData bit=1;
DECLARE @NowUtc datetime2(3)=SYSUTCDATETIME();
DECLARE @Day0Utc datetime2(3)=CONVERT(datetime2(3),CONVERT(date,@NowUtc));
DECLARE @SchemaVersion nvarchar(100)=CONVERT(nvarchar(100),(SELECT value FROM sys.extended_properties WHERE class=0 AND name=N'SkillsSwap.SchemaVersion'));
IF EXISTS(SELECT 1 FROM sys.tables WHERE is_ms_shipped=0) AND ISNULL(@SchemaVersion,N'')<>N'1.0'
    THROW 51001, 'Database already contains unrecognized objects. Use a new trial database; nothing was changed.', 1;
IF @SchemaVersion IS NULL
BEGIN
BEGIN TRY
    BEGIN TRANSACTION;
CREATE TABLE [dbo].[AspNetUsers] (
    [Id] uniqueidentifier NOT NULL,
    [UserName] nvarchar(256) NOT NULL,
    [NormalizedUserName] nvarchar(256) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [NormalizedEmail] nvarchar(256) NOT NULL,
    [EmailConfirmed] bit NOT NULL CONSTRAINT [DF_AspNetUsers_EmailConfirmed] DEFAULT (0),
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL CONSTRAINT [DF_AspNetUsers_PhoneNumberConfirmed] DEFAULT (0),
    [TwoFactorEnabled] bit NOT NULL CONSTRAINT [DF_AspNetUsers_TwoFactorEnabled] DEFAULT (0),
    [LockoutEnd] datetimeoffset(3) NULL,
    [LockoutEnabled] bit NOT NULL CONSTRAINT [DF_AspNetUsers_LockoutEnabled] DEFAULT (1),
    [AccessFailedCount] int NOT NULL CONSTRAINT [DF_AspNetUsers_AccessFailedCount] DEFAULT (0),
    [AccountStatus] varchar(24) NOT NULL,
    [StatusReason] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [StatusChangedAtUtc] datetime2(3) NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_AspNetUsers_1] UNIQUE ([NormalizedUserName]),
    CONSTRAINT [UQ_AspNetUsers_2] UNIQUE ([NormalizedEmail]),
    CONSTRAINT [CK_AspNetUsers_01] CHECK (AccountStatus IN ('Active', 'Suspended', 'Deactivated'))
);

CREATE TABLE [dbo].[AspNetRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NOT NULL,
    [NormalizedName] nvarchar(256) NOT NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_AspNetRoles_1] UNIQUE ([NormalizedName])
);

CREATE TABLE [dbo].[AspNetUserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId])
);

CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[AspNetRoleClaims] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider] nvarchar(128) NOT NULL,
    [ProviderKey] nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(256) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey])
);

CREATE TABLE [dbo].[AspNetUserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name])
);

CREATE TABLE [dbo].[UserProfiles] (
    [UserId] uniqueidentifier NOT NULL,
    [DisplayName] nvarchar(80) NOT NULL,
    [Bio] nvarchar(1500) NULL,
    [AvatarStorageKey] nvarchar(500) NULL,
    [CountryCode] char(2) NULL,
    [City] nvarchar(100) NULL,
    [TimeZoneId] nvarchar(100) NOT NULL,
    [AvailabilityNote] nvarchar(300) NULL,
    [UpdatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_UserProfiles_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([UserId])
);

CREATE TABLE [dbo].[Categories] (
    [CategoryId] int IDENTITY(1,1) NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [NameEn] nvarchar(100) NOT NULL,
    [NameAr] nvarchar(100) NOT NULL,
    [IconKey] nvarchar(100) NOT NULL,
    [SortOrder] int NOT NULL CONSTRAINT [DF_Categories_SortOrder] DEFAULT (0),
    [IsActive] bit NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT (1),
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [UQ_Categories_1] UNIQUE ([Code])
);

CREATE TABLE [dbo].[SubCategories] (
    [SubCategoryId] int IDENTITY(1,1) NOT NULL,
    [CategoryId] int NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [NameEn] nvarchar(120) NOT NULL,
    [NameAr] nvarchar(120) NOT NULL,
    [SortOrder] int NOT NULL CONSTRAINT [DF_SubCategories_SortOrder] DEFAULT (0),
    [IsActive] bit NOT NULL CONSTRAINT [DF_SubCategories_IsActive] DEFAULT (1),
    CONSTRAINT [PK_SubCategories] PRIMARY KEY ([SubCategoryId]),
    CONSTRAINT [UQ_SubCategories_1] UNIQUE ([CategoryId], [Code])
);

CREATE TABLE [dbo].[Skills] (
    [SkillId] int IDENTITY(1,1) NOT NULL,
    [SubCategoryId] int NOT NULL,
    [Slug] nvarchar(120) NOT NULL,
    [NameEn] nvarchar(150) NOT NULL,
    [NameAr] nvarchar(150) NOT NULL,
    [DescriptionEn] nvarchar(2000) NULL,
    [DescriptionAr] nvarchar(2000) NULL,
    [IconKey] nvarchar(100) NULL,
    [LevelCode] varchar(24) NOT NULL,
    [AccessTier] varchar(24) NOT NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF_Skills_IsActive] DEFAULT (1),
    [CreatedByUserId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_Skills_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_Skills_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Skills] PRIMARY KEY ([SkillId]),
    CONSTRAINT [UQ_Skills_1] UNIQUE ([Slug]),
    CONSTRAINT [CK_Skills_01] CHECK (LevelCode IN ('Beginner', 'Intermediate', 'Advanced')),
    CONSTRAINT [CK_Skills_02] CHECK (AccessTier IN ('Basic', 'Advanced'))
);

CREATE TABLE [dbo].[UserSkills] (
    [UserId] uniqueidentifier NOT NULL,
    [SkillId] int NOT NULL,
    [WantsToLearn] bit NOT NULL CONSTRAINT [DF_UserSkills_WantsToLearn] DEFAULT (0),
    [CanTeach] bit NOT NULL CONSTRAINT [DF_UserSkills_CanTeach] DEFAULT (0),
    [KnowledgeLevel] varchar(24) NOT NULL,
    [TeachingMode] varchar(16) NULL,
    [IsTeachingAvailable] bit NOT NULL CONSTRAINT [DF_UserSkills_IsTeachingAvailable] DEFAULT (0),
    [TeachingSummary] nvarchar(500) NULL,
    [UpdatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_UserSkills_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_UserSkills] PRIMARY KEY ([UserId], [SkillId]),
    CONSTRAINT [CK_UserSkills_01] CHECK (CanTeach=1 OR WantsToLearn=1 OR KnowledgeLevel<>'Unknown'),
    CONSTRAINT [CK_UserSkills_02] CHECK (CanTeach=0 OR (TeachingMode IS NOT NULL AND KnowledgeLevel<>'Unknown')),
    CONSTRAINT [CK_UserSkills_03] CHECK (IsTeachingAvailable=0 OR CanTeach=1),
    CONSTRAINT [CK_UserSkills_04] CHECK (CanTeach=1 OR TeachingMode IS NULL),
    CONSTRAINT [CK_UserSkills_05] CHECK (KnowledgeLevel IN ('Unknown', 'Beginner', 'Intermediate', 'Advanced')),
    CONSTRAINT [CK_UserSkills_06] CHECK ((TeachingMode IS NULL OR TeachingMode IN ('Exchange', 'Volunteer', 'Both')))
);

CREATE TABLE [dbo].[UserAvailability] (
    [AvailabilityId] bigint IDENTITY(1,1) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [DayOfWeek] tinyint NOT NULL,
    [StartLocalTime] time(0) NOT NULL,
    [EndLocalTime] time(0) NOT NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF_UserAvailability_IsActive] DEFAULT (1),
    CONSTRAINT [PK_UserAvailability] PRIMARY KEY ([AvailabilityId]),
    CONSTRAINT [UQ_UserAvailability_1] UNIQUE ([UserId], [DayOfWeek], [StartLocalTime], [EndLocalTime]),
    CONSTRAINT [CK_UserAvailability_01] CHECK (DayOfWeek BETWEEN 0 AND 6),
    CONSTRAINT [CK_UserAvailability_02] CHECK (StartLocalTime < EndLocalTime)
);

CREATE TABLE [dbo].[ExpertApplications] (
    [ExpertApplicationId] bigint IDENTITY(1,1) NOT NULL,
    [ApplicantUserId] uniqueidentifier NOT NULL,
    [SkillId] int NOT NULL,
    [EvidenceRoute] varchar(24) NOT NULL,
    [ExperienceYears] decimal(4,1) NOT NULL,
    [Qualifications] nvarchar(1500) NOT NULL,
    [PortfolioUrl] nvarchar(1000) NULL,
    [TeachingEvidence] nvarchar(2000) NOT NULL,
    [StatusCode] varchar(24) NOT NULL,
    [ReviewedByUserId] uniqueidentifier NULL,
    [ReviewNote] nvarchar(2000) NULL,
    [SubmittedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_ExpertApplications_SubmittedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ReviewedAtUtc] datetime2(3) NULL,
    [ClosedAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ExpertApplications] PRIMARY KEY ([ExpertApplicationId]),
    CONSTRAINT [UQ_ExpertApplications_2] UNIQUE ([ExpertApplicationId], [ApplicantUserId], [SkillId]),
    CONSTRAINT [CK_ExpertApplications_01] CHECK (ExperienceYears >= 0),
    CONSTRAINT [CK_ExpertApplications_02] CHECK ((StatusCode IN ('Pending','UnderReview','NeedsInfo') AND ClosedAtUtc IS NULL) OR (StatusCode IN ('Approved','Rejected','Withdrawn') AND ClosedAtUtc IS NOT NULL)),
    CONSTRAINT [CK_ExpertApplications_03] CHECK (StatusCode NOT IN ('Approved','Rejected') OR (ReviewedByUserId IS NOT NULL AND ReviewedAtUtc IS NOT NULL AND ReviewNote IS NOT NULL)),
    CONSTRAINT [CK_ExpertApplications_04] CHECK (EvidenceRoute IN ('Community', 'Portfolio')),
    CONSTRAINT [CK_ExpertApplications_05] CHECK (StatusCode IN ('Pending', 'UnderReview', 'NeedsInfo', 'Approved', 'Rejected', 'Withdrawn'))
);

CREATE TABLE [dbo].[ExpertSkills] (
    [ExpertSkillId] bigint IDENTITY(1,1) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [SkillId] int NOT NULL,
    [ApprovedApplicationId] bigint NOT NULL,
    [StatusCode] varchar(24) NOT NULL,
    [ApprovedByUserId] uniqueidentifier NOT NULL,
    [ApprovedAtUtc] datetime2(3) NOT NULL,
    [StatusReason] nvarchar(1000) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ExpertSkills] PRIMARY KEY ([ExpertSkillId]),
    CONSTRAINT [UQ_ExpertSkills_1] UNIQUE ([UserId], [SkillId]),
    CONSTRAINT [UQ_ExpertSkills_2] UNIQUE ([ApprovedApplicationId]),
    CONSTRAINT [CK_ExpertSkills_01] CHECK (StatusCode='Active' OR StatusReason IS NOT NULL),
    CONSTRAINT [CK_ExpertSkills_02] CHECK (StatusCode IN ('Active', 'Suspended', 'Revoked'))
);

CREATE TABLE [dbo].[SubscriptionPlans] (
    [PlanId] int IDENTITY(1,1) NOT NULL,
    [Code] varchar(24) NOT NULL,
    [NameEn] nvarchar(80) NOT NULL,
    [NameAr] nvarchar(80) NOT NULL,
    [CanAccessAdvanced] bit NOT NULL CONSTRAINT [DF_SubscriptionPlans_CanAccessAdvanced] DEFAULT (0),
    [CanBookExperts] bit NOT NULL CONSTRAINT [DF_SubscriptionPlans_CanBookExperts] DEFAULT (0),
    [ExpertCreditsPerMonth] smallint NOT NULL,
    [ExpertSessionMinutes] smallint NOT NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF_SubscriptionPlans_IsActive] DEFAULT (1),
    CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY ([PlanId]),
    CONSTRAINT [UQ_SubscriptionPlans_1] UNIQUE ([Code]),
    CONSTRAINT [CK_SubscriptionPlans_01] CHECK (ExpertCreditsPerMonth>=0),
    CONSTRAINT [CK_SubscriptionPlans_02] CHECK (ExpertSessionMinutes>=0),
    CONSTRAINT [CK_SubscriptionPlans_03] CHECK (Code<>'Free' OR (CanAccessAdvanced=0 AND CanBookExperts=0 AND ExpertCreditsPerMonth=0)),
    CONSTRAINT [CK_SubscriptionPlans_04] CHECK (CanBookExperts=0 OR (ExpertCreditsPerMonth>0 AND ExpertSessionMinutes>0)),
    CONSTRAINT [CK_SubscriptionPlans_05] CHECK (Code IN ('Free', 'Plus'))
);

CREATE TABLE [dbo].[PlanPrices] (
    [PlanPriceId] int IDENTITY(1,1) NOT NULL,
    [PlanId] int NOT NULL,
    [BillingMonths] tinyint NOT NULL,
    [Amount] decimal(10,2) NOT NULL,
    [Currency] char(3) NOT NULL,
    [ValidFromUtc] datetime2(3) NOT NULL CONSTRAINT [DF_PlanPrices_ValidFromUtc] DEFAULT (SYSUTCDATETIME()),
    [ValidUntilUtc] datetime2(3) NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF_PlanPrices_IsActive] DEFAULT (0),
    CONSTRAINT [PK_PlanPrices] PRIMARY KEY ([PlanPriceId]),
    CONSTRAINT [CK_PlanPrices_01] CHECK (BillingMonths IN (1,12)),
    CONSTRAINT [CK_PlanPrices_02] CHECK (Amount>=0),
    CONSTRAINT [CK_PlanPrices_03] CHECK (ValidUntilUtc IS NULL OR ValidUntilUtc>ValidFromUtc)
);

CREATE TABLE [dbo].[UserSubscriptions] (
    [SubscriptionId] bigint IDENTITY(1,1) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [PlanPriceId] int NOT NULL,
    [StatusCode] varchar(24) NOT NULL,
    [StartsAtUtc] datetime2(3) NOT NULL,
    [PaidThroughUtc] datetime2(3) NOT NULL,
    [CurrentBillingStartUtc] datetime2(3) NOT NULL,
    [CurrentBillingEndUtc] datetime2(3) NOT NULL,
    [CancelAtPeriodEnd] bit NOT NULL CONSTRAINT [DF_UserSubscriptions_CancelAtPeriodEnd] DEFAULT (0),
    [CancelledAtUtc] datetime2(3) NULL,
    [ProviderCode] nvarchar(40) NULL,
    [ProviderSubscriptionId] nvarchar(200) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_UserSubscriptions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    [ExpertCreditsPerMonthSnapshot] smallint NOT NULL,
    [ExpertSessionMinutesSnapshot] smallint NOT NULL,
    [CanAccessAdvancedSnapshot] bit NOT NULL CONSTRAINT [DF_UserSubscriptions_CanAccessAdvancedSnapshot] DEFAULT (0),
    [CanBookExpertsSnapshot] bit NOT NULL CONSTRAINT [DF_UserSubscriptions_CanBookExpertsSnapshot] DEFAULT (0),
    CONSTRAINT [PK_UserSubscriptions] PRIMARY KEY ([SubscriptionId]),
    CONSTRAINT [CK_UserSubscriptions_01] CHECK (PaidThroughUtc>=StartsAtUtc),
    CONSTRAINT [CK_UserSubscriptions_02] CHECK (CurrentBillingEndUtc>CurrentBillingStartUtc),
    CONSTRAINT [CK_UserSubscriptions_03] CHECK (ExpertCreditsPerMonthSnapshot>=0),
    CONSTRAINT [CK_UserSubscriptions_04] CHECK (ExpertSessionMinutesSnapshot>=0),
    CONSTRAINT [CK_UserSubscriptions_05] CHECK (CanBookExpertsSnapshot=0 OR (ExpertCreditsPerMonthSnapshot>0 AND ExpertSessionMinutesSnapshot>0)),
    CONSTRAINT [CK_UserSubscriptions_06] CHECK (StatusCode IN ('Pending', 'Active', 'PastDue', 'Expired', 'Cancelled'))
);

CREATE TABLE [dbo].[Payments] (
    [PaymentId] bigint IDENTITY(1,1) NOT NULL,
    [SubscriptionId] bigint NOT NULL,
    [Amount] decimal(10,2) NOT NULL,
    [Currency] char(3) NOT NULL,
    [StatusCode] varchar(24) NOT NULL,
    [ProviderCode] nvarchar(40) NOT NULL,
    [ProviderPaymentId] nvarchar(200) NULL,
    [IdempotencyKey] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_Payments_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [PaidAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [UQ_Payments_1] UNIQUE ([IdempotencyKey]),
    CONSTRAINT [CK_Payments_01] CHECK (Amount>0),
    CONSTRAINT [CK_Payments_02] CHECK (StatusCode<>'Succeeded' OR PaidAtUtc IS NOT NULL),
    CONSTRAINT [CK_Payments_03] CHECK (StatusCode IN ('Created', 'Pending', 'Succeeded', 'Failed', 'Cancelled'))
);

CREATE TABLE [dbo].[ExpertCreditGrants] (
    [CreditGrantId] bigint IDENTITY(1,1) NOT NULL,
    [SubscriptionId] bigint NOT NULL,
    [PeriodStartUtc] datetime2(3) NOT NULL,
    [PeriodEndUtc] datetime2(3) NOT NULL,
    [ExpiresAtUtc] datetime2(3) NOT NULL,
    [Quantity] smallint NOT NULL,
    [SessionMinutes] smallint NOT NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_ExpertCreditGrants_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ExpertCreditGrants] PRIMARY KEY ([CreditGrantId]),
    CONSTRAINT [UQ_ExpertCreditGrants_1] UNIQUE ([SubscriptionId], [PeriodStartUtc]),
    CONSTRAINT [CK_ExpertCreditGrants_01] CHECK (Quantity>0),
    CONSTRAINT [CK_ExpertCreditGrants_02] CHECK (SessionMinutes>0),
    CONSTRAINT [CK_ExpertCreditGrants_03] CHECK (PeriodEndUtc>PeriodStartUtc),
    CONSTRAINT [CK_ExpertCreditGrants_04] CHECK (ExpiresAtUtc>=PeriodEndUtc)
);

CREATE TABLE [dbo].[LearningRequests] (
    [RequestId] bigint IDENTITY(1,1) NOT NULL,
    [RequesterUserId] uniqueidentifier NOT NULL,
    [ProviderUserId] uniqueidentifier NOT NULL,
    [SkillId] int NOT NULL,
    [OfferedSkillId] int NULL,
    [ExpertSkillId] bigint NULL,
    [RequestType] varchar(24) NOT NULL,
    [StatusCode] varchar(24) NOT NULL,
    [GoalMessage] nvarchar(2000) NOT NULL,
    [PreferredTimeCode] varchar(24) NULL,
    [ProposedStartUtc] datetime2(3) NULL,
    [ProposedEndUtc] datetime2(3) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_LearningRequests_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [RespondedAtUtc] datetime2(3) NULL,
    [ClosedAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_LearningRequests] PRIMARY KEY ([RequestId]),
    CONSTRAINT [CK_LearningRequests_01] CHECK (RequesterUserId<>ProviderUserId),
    CONSTRAINT [CK_LearningRequests_02] CHECK ((RequestType='Exchange' AND OfferedSkillId IS NOT NULL) OR (RequestType<>'Exchange' AND OfferedSkillId IS NULL)),
    CONSTRAINT [CK_LearningRequests_03] CHECK ((RequestType='Expert' AND ExpertSkillId IS NOT NULL) OR (RequestType<>'Expert' AND ExpertSkillId IS NULL)),
    CONSTRAINT [CK_LearningRequests_04] CHECK ((ProposedStartUtc IS NULL AND ProposedEndUtc IS NULL) OR (ProposedStartUtc IS NOT NULL AND ProposedEndUtc IS NOT NULL AND ProposedEndUtc>ProposedStartUtc)),
    CONSTRAINT [CK_LearningRequests_05] CHECK (RequestType<>'Expert' OR (ProposedStartUtc IS NOT NULL AND ProposedEndUtc IS NOT NULL)),
    CONSTRAINT [CK_LearningRequests_06] CHECK ((StatusCode IN ('Pending','Accepted') AND ClosedAtUtc IS NULL) OR (StatusCode IN ('Rejected','Cancelled','Completed') AND ClosedAtUtc IS NOT NULL)),
    CONSTRAINT [CK_LearningRequests_07] CHECK (RequestType IN ('Exchange', 'Volunteer', 'Expert')),
    CONSTRAINT [CK_LearningRequests_08] CHECK (StatusCode IN ('Pending', 'Accepted', 'Rejected', 'Cancelled', 'Completed')),
    CONSTRAINT [CK_LearningRequests_09] CHECK ((PreferredTimeCode IS NULL OR PreferredTimeCode IN ('WeekdaysEvening', 'Weekends', 'Flexible')))
);

CREATE TABLE [dbo].[ExpertCreditReservations] (
    [CreditReservationId] bigint IDENTITY(1,1) NOT NULL,
    [CreditGrantId] bigint NOT NULL,
    [RequestId] bigint NOT NULL,
    [StatusCode] varchar(24) NOT NULL,
    [ReservedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_ExpertCreditReservations_ReservedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ReservationExpiresAtUtc] datetime2(3) NOT NULL,
    [ConsumedAtUtc] datetime2(3) NULL,
    [ReleasedAtUtc] datetime2(3) NULL,
    [ReleaseReason] nvarchar(500) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ExpertCreditReservations] PRIMARY KEY ([CreditReservationId]),
    CONSTRAINT [UQ_ExpertCreditReservations_1] UNIQUE ([RequestId]),
    CONSTRAINT [UQ_ExpertCreditReservations_2] UNIQUE ([CreditReservationId], [RequestId]),
    CONSTRAINT [CK_ExpertCreditReservations_01] CHECK ((StatusCode='Reserved' AND ConsumedAtUtc IS NULL AND ReleasedAtUtc IS NULL) OR (StatusCode='Consumed' AND ConsumedAtUtc IS NOT NULL AND ReleasedAtUtc IS NULL) OR (StatusCode='Released' AND ReleasedAtUtc IS NOT NULL AND ConsumedAtUtc IS NULL)),
    CONSTRAINT [CK_ExpertCreditReservations_02] CHECK (ReservationExpiresAtUtc>ReservedAtUtc),
    CONSTRAINT [CK_ExpertCreditReservations_03] CHECK (StatusCode IN ('Reserved', 'Consumed', 'Released'))
);

CREATE TABLE [dbo].[LearningSessions] (
    [SessionId] bigint IDENTITY(1,1) NOT NULL,
    [RequestId] bigint NOT NULL,
    [TeacherUserId] uniqueidentifier NOT NULL,
    [LearnerUserId] uniqueidentifier NOT NULL,
    [SkillId] int NOT NULL,
    [CreditReservationId] bigint NULL,
    [StartsAtUtc] datetime2(3) NOT NULL,
    [EndsAtUtc] datetime2(3) NOT NULL,
    [StatusCode] varchar(24) NOT NULL,
    [MeetingProvider] varchar(24) NOT NULL,
    [ProviderMeetingId] nvarchar(200) NULL,
    [ProtectedJoinUrl] varbinary(max) NULL,
    [CancelledByUserId] uniqueidentifier NULL,
    [CancellationReason] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_LearningSessions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CompletedAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,
    [IdempotencyKey] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_LearningSessions] PRIMARY KEY ([SessionId]),
    CONSTRAINT [UQ_LearningSessions_2] UNIQUE ([IdempotencyKey]),
    CONSTRAINT [CK_LearningSessions_01] CHECK (TeacherUserId<>LearnerUserId),
    CONSTRAINT [CK_LearningSessions_02] CHECK (EndsAtUtc>StartsAtUtc),
    CONSTRAINT [CK_LearningSessions_03] CHECK (StatusCode<>'Completed' OR CompletedAtUtc IS NOT NULL),
    CONSTRAINT [CK_LearningSessions_04] CHECK (StatusCode IN ('Scheduled', 'InProgress', 'AwaitingConfirmation', 'Completed', 'Cancelled', 'NoShow')),
    CONSTRAINT [CK_LearningSessions_05] CHECK (MeetingProvider IN ('Chat', 'Zoom', 'External'))
);

CREATE TABLE [dbo].[SessionConfirmations] (
    [SessionId] bigint NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Attended] bit NULL,
    [CompletionConfirmedAtUtc] datetime2(3) NULL,
    [UpdatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_SessionConfirmations_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SessionConfirmations] PRIMARY KEY ([SessionId], [UserId]),
    CONSTRAINT [CK_SessionConfirmations_01] CHECK (CompletionConfirmedAtUtc IS NULL OR (Attended IS NOT NULL AND Attended=1))
);

CREATE TABLE [dbo].[LearningSessionChanges] (
    [SessionChangeId] bigint IDENTITY(1,1) NOT NULL,
    [SessionId] bigint NOT NULL,
    [RequestedByUserId] uniqueidentifier NOT NULL,
    [ChangeType] varchar(24) NOT NULL,
    [OriginalStartsAtUtc] datetime2(3) NOT NULL,
    [OriginalEndsAtUtc] datetime2(3) NOT NULL,
    [ProposedStartsAtUtc] datetime2(3) NULL,
    [ProposedEndsAtUtc] datetime2(3) NULL,
    [Reason] nvarchar(1500) NOT NULL,
    [StatusCode] varchar(24) NOT NULL,
    [DecisionSource] varchar(16) NULL,
    [RespondedByUserId] uniqueidentifier NULL,
    [ResponseNote] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_LearningSessionChanges_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [RespondedAtUtc] datetime2(3) NULL,
    [ClosedAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_LearningSessionChanges] PRIMARY KEY ([SessionChangeId]),
    CONSTRAINT [CK_LearningSessionChanges_01] CHECK (OriginalEndsAtUtc>OriginalStartsAtUtc),
    CONSTRAINT [CK_LearningSessionChanges_02] CHECK ((ChangeType='Reschedule' AND ProposedStartsAtUtc IS NOT NULL AND ProposedEndsAtUtc IS NOT NULL AND ProposedEndsAtUtc>ProposedStartsAtUtc) OR (ChangeType<>'Reschedule' AND ProposedStartsAtUtc IS NULL AND ProposedEndsAtUtc IS NULL)),
    CONSTRAINT [CK_LearningSessionChanges_03] CHECK ((StatusCode='Pending' AND ClosedAtUtc IS NULL) OR (StatusCode<>'Pending' AND ClosedAtUtc IS NOT NULL)),
    CONSTRAINT [CK_LearningSessionChanges_04] CHECK (StatusCode NOT IN ('Accepted','Rejected') OR (RespondedAtUtc IS NOT NULL AND DecisionSource IS NOT NULL)),
    CONSTRAINT [CK_LearningSessionChanges_05] CHECK (ChangeType IN ('Reschedule', 'Cancel', 'AttendanceIssue')),
    CONSTRAINT [CK_LearningSessionChanges_06] CHECK (StatusCode IN ('Pending', 'Accepted', 'Rejected', 'Withdrawn', 'Expired')),
    CONSTRAINT [CK_LearningSessionChanges_07] CHECK ((DecisionSource IS NULL OR DecisionSource IN ('Participant', 'Policy')))
);

CREATE TABLE [dbo].[Conversations] (
    [ConversationId] bigint IDENTITY(1,1) NOT NULL,
    [RequestId] bigint NOT NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_Conversations_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ClosedAtUtc] datetime2(3) NULL,
    CONSTRAINT [PK_Conversations] PRIMARY KEY ([ConversationId]),
    CONSTRAINT [UQ_Conversations_1] UNIQUE ([RequestId])
);

CREATE TABLE [dbo].[ConversationParticipants] (
    [ConversationId] bigint NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [JoinedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_ConversationParticipants_JoinedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [LastReadMessageId] bigint NULL,
    CONSTRAINT [PK_ConversationParticipants] PRIMARY KEY ([ConversationId], [UserId])
);

CREATE TABLE [dbo].[Messages] (
    [MessageId] bigint IDENTITY(1,1) NOT NULL,
    [ConversationId] bigint NOT NULL,
    [SenderUserId] uniqueidentifier NOT NULL,
    [Body] nvarchar(4000) NOT NULL,
    [SentAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_Messages_SentAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ClientMessageId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([MessageId]),
    CONSTRAINT [UQ_Messages_1] UNIQUE ([ConversationId], [SenderUserId], [ClientMessageId]),
    CONSTRAINT [UQ_Messages_2] UNIQUE ([ConversationId], [MessageId]),
    CONSTRAINT [CK_Messages_01] CHECK (LEN(LTRIM(RTRIM(Body)))>0)
);

CREATE TABLE [dbo].[SessionReviews] (
    [ReviewId] bigint IDENTITY(1,1) NOT NULL,
    [SessionId] bigint NOT NULL,
    [ReviewerUserId] uniqueidentifier NOT NULL,
    [ReviewedUserId] uniqueidentifier NOT NULL,
    [Rating] tinyint NOT NULL,
    [Comment] nvarchar(1500) NULL,
    [VisibilityCode] varchar(24) NOT NULL,
    [ModeratedByUserId] uniqueidentifier NULL,
    [ModerationReason] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_SessionReviews_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SessionReviews] PRIMARY KEY ([ReviewId]),
    CONSTRAINT [UQ_SessionReviews_1] UNIQUE ([SessionId], [ReviewerUserId]),
    CONSTRAINT [CK_SessionReviews_01] CHECK (Rating BETWEEN 1 AND 5),
    CONSTRAINT [CK_SessionReviews_02] CHECK (ReviewerUserId<>ReviewedUserId),
    CONSTRAINT [CK_SessionReviews_03] CHECK (VisibilityCode<>'Hidden' OR (ModeratedByUserId IS NOT NULL AND ModerationReason IS NOT NULL)),
    CONSTRAINT [CK_SessionReviews_04] CHECK (VisibilityCode IN ('Published', 'Hidden'))
);

CREATE TABLE [dbo].[CareerGoals] (
    [CareerGoalId] int IDENTITY(1,1) NOT NULL,
    [Code] nvarchar(60) NOT NULL,
    [NameEn] nvarchar(150) NOT NULL,
    [NameAr] nvarchar(150) NOT NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF_CareerGoals_IsActive] DEFAULT (1),
    CONSTRAINT [PK_CareerGoals] PRIMARY KEY ([CareerGoalId]),
    CONSTRAINT [UQ_CareerGoals_1] UNIQUE ([Code])
);

CREATE TABLE [dbo].[UserCareerPaths] (
    [CareerPathId] bigint IDENTITY(1,1) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [CareerGoalId] int NOT NULL,
    [StartingLevel] varchar(24) NOT NULL,
    [TimeCommitment] varchar(24) NOT NULL,
    [UserRequest] nvarchar(1500) NULL,
    [SourceCode] varchar(24) NOT NULL,
    [RecommendationVersion] nvarchar(80) NULL,
    [StatusCode] varchar(24) NOT NULL,
    [IsCurrent] bit NOT NULL CONSTRAINT [DF_UserCareerPaths_IsCurrent] DEFAULT (1),
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_UserCareerPaths_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CompletedAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_UserCareerPaths] PRIMARY KEY ([CareerPathId]),
    CONSTRAINT [CK_UserCareerPaths_01] CHECK (StatusCode<>'Archived' OR IsCurrent=0),
    CONSTRAINT [CK_UserCareerPaths_02] CHECK (StatusCode<>'Completed' OR CompletedAtUtc IS NOT NULL),
    CONSTRAINT [CK_UserCareerPaths_03] CHECK (StartingLevel IN ('Beginner', 'Intermediate', 'Advanced')),
    CONSTRAINT [CK_UserCareerPaths_04] CHECK (TimeCommitment IN ('Light', 'Moderate', 'Intense')),
    CONSTRAINT [CK_UserCareerPaths_05] CHECK (SourceCode IN ('RuleBased', 'AIAssisted')),
    CONSTRAINT [CK_UserCareerPaths_06] CHECK (StatusCode IN ('Active', 'Paused', 'Completed', 'Archived'))
);

CREATE TABLE [dbo].[CareerPathKnownSkills] (
    [CareerPathId] bigint NOT NULL,
    [SkillId] int NOT NULL,
    CONSTRAINT [PK_CareerPathKnownSkills] PRIMARY KEY ([CareerPathId], [SkillId])
);

CREATE TABLE [dbo].[CareerPathSteps] (
    [CareerPathStepId] bigint IDENTITY(1,1) NOT NULL,
    [CareerPathId] bigint NOT NULL,
    [SkillId] int NOT NULL,
    [StepOrder] smallint NOT NULL,
    [TitleEn] nvarchar(180) NOT NULL,
    [TitleAr] nvarchar(180) NOT NULL,
    [RecommendationReasonEn] nvarchar(1000) NULL,
    [RecommendationReasonAr] nvarchar(1000) NULL,
    [RequiredCompletedSessions] smallint NOT NULL CONSTRAINT [DF_CareerPathSteps_RequiredCompletedSessions] DEFAULT (1),
    [StartedAtUtc] datetime2(3) NULL,
    [CompletedAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_CareerPathSteps] PRIMARY KEY ([CareerPathStepId]),
    CONSTRAINT [UQ_CareerPathSteps_1] UNIQUE ([CareerPathId], [StepOrder]),
    CONSTRAINT [CK_CareerPathSteps_01] CHECK (StepOrder>0),
    CONSTRAINT [CK_CareerPathSteps_02] CHECK (RequiredCompletedSessions>0),
    CONSTRAINT [CK_CareerPathSteps_03] CHECK (CompletedAtUtc IS NULL OR (StartedAtUtc IS NOT NULL AND CompletedAtUtc>=StartedAtUtc))
);

CREATE TABLE [dbo].[CareerStepSessions] (
    [CareerPathStepId] bigint NOT NULL,
    [SessionId] bigint NOT NULL,
    [LinkedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_CareerStepSessions_LinkedAtUtc] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_CareerStepSessions] PRIMARY KEY ([CareerPathStepId], [SessionId]),
    CONSTRAINT [UQ_CareerStepSessions_1] UNIQUE ([SessionId])
);

CREATE TABLE [dbo].[PaymentProviderEvents] (
    [ProviderEventId] bigint IDENTITY(1,1) NOT NULL,
    [ProviderCode] nvarchar(40) NOT NULL,
    [ExternalEventId] nvarchar(200) NOT NULL,
    [EventType] nvarchar(100) NOT NULL,
    [PaymentId] bigint NULL,
    [ReceivedAtUtc] datetime2(3) NOT NULL,
    [ProcessedAtUtc] datetime2(3) NULL,
    [ProcessingState] varchar(24) NOT NULL,
    [ErrorCode] nvarchar(200) NULL,
    CONSTRAINT [PK_PaymentProviderEvents] PRIMARY KEY ([ProviderEventId]),
    CONSTRAINT [UQ_PaymentProviderEvents_1] UNIQUE ([ProviderCode], [ExternalEventId]),
    CONSTRAINT [CK_PaymentProviderEvents_01] CHECK (ProcessingState IN ('Pending', 'Processed', 'Failed'))
);

CREATE TABLE [dbo].[PaymentRefunds] (
    [RefundId] bigint IDENTITY(1,1) NOT NULL,
    [PaymentId] bigint NOT NULL,
    [Amount] decimal(10,2) NOT NULL,
    [ProviderCode] nvarchar(40) NOT NULL,
    [StatusCode] varchar(24) NOT NULL,
    [ProviderRefundId] nvarchar(200) NULL,
    [IdempotencyKey] uniqueidentifier NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL CONSTRAINT [DF_PaymentRefunds_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CompletedAtUtc] datetime2(3) NULL,
    CONSTRAINT [PK_PaymentRefunds] PRIMARY KEY ([RefundId]),
    CONSTRAINT [UQ_PaymentRefunds_1] UNIQUE ([IdempotencyKey]),
    CONSTRAINT [CK_PaymentRefunds_01] CHECK (Amount>0),
    CONSTRAINT [CK_PaymentRefunds_02] CHECK (StatusCode IN ('Pending', 'Succeeded', 'Failed'))
);

ALTER TABLE [dbo].[AspNetUserRoles] WITH CHECK ADD CONSTRAINT [FK_AspNetUserRoles_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[AspNetUserRoles] WITH CHECK ADD CONSTRAINT [FK_AspNetUserRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[AspNetUserClaims] WITH CHECK ADD CONSTRAINT [FK_AspNetUserClaims_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[AspNetRoleClaims] WITH CHECK ADD CONSTRAINT [FK_AspNetRoleClaims_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[AspNetUserLogins] WITH CHECK ADD CONSTRAINT [FK_AspNetUserLogins_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[AspNetUserTokens] WITH CHECK ADD CONSTRAINT [FK_AspNetUserTokens_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[UserProfiles] WITH CHECK ADD CONSTRAINT [FK_UserProfiles_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[SubCategories] WITH CHECK ADD CONSTRAINT [FK_SubCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([CategoryId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[Skills] WITH CHECK ADD CONSTRAINT [FK_Skills_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [dbo].[SubCategories] ([SubCategoryId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[Skills] WITH CHECK ADD CONSTRAINT [FK_Skills_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[UserSkills] WITH CHECK ADD CONSTRAINT [FK_UserSkills_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[UserSkills] WITH CHECK ADD CONSTRAINT [FK_UserSkills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[UserAvailability] WITH CHECK ADD CONSTRAINT [FK_UserAvailability_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertApplications] WITH CHECK ADD CONSTRAINT [FK_ExpertApplications_ApplicantUserId] FOREIGN KEY ([ApplicantUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertApplications] WITH CHECK ADD CONSTRAINT [FK_ExpertApplications_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertApplications] WITH CHECK ADD CONSTRAINT [FK_ExpertApplications_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertSkills] WITH CHECK ADD CONSTRAINT [FK_ExpertSkills_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertSkills] WITH CHECK ADD CONSTRAINT [FK_ExpertSkills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertSkills] WITH CHECK ADD CONSTRAINT [FK_ExpertSkills_ApprovedApplicationId] FOREIGN KEY ([ApprovedApplicationId]) REFERENCES [dbo].[ExpertApplications] ([ExpertApplicationId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertSkills] WITH CHECK ADD CONSTRAINT [FK_ExpertSkills_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[PlanPrices] WITH CHECK ADD CONSTRAINT [FK_PlanPrices_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [dbo].[SubscriptionPlans] ([PlanId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[UserSubscriptions] WITH CHECK ADD CONSTRAINT [FK_UserSubscriptions_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[UserSubscriptions] WITH CHECK ADD CONSTRAINT [FK_UserSubscriptions_PlanPriceId] FOREIGN KEY ([PlanPriceId]) REFERENCES [dbo].[PlanPrices] ([PlanPriceId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[Payments] WITH CHECK ADD CONSTRAINT [FK_Payments_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [dbo].[UserSubscriptions] ([SubscriptionId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertCreditGrants] WITH CHECK ADD CONSTRAINT [FK_ExpertCreditGrants_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [dbo].[UserSubscriptions] ([SubscriptionId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningRequests] WITH CHECK ADD CONSTRAINT [FK_LearningRequests_RequesterUserId] FOREIGN KEY ([RequesterUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningRequests] WITH CHECK ADD CONSTRAINT [FK_LearningRequests_ProviderUserId] FOREIGN KEY ([ProviderUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningRequests] WITH CHECK ADD CONSTRAINT [FK_LearningRequests_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningRequests] WITH CHECK ADD CONSTRAINT [FK_LearningRequests_OfferedSkillId] FOREIGN KEY ([OfferedSkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningRequests] WITH CHECK ADD CONSTRAINT [FK_LearningRequests_ExpertSkillId] FOREIGN KEY ([ExpertSkillId]) REFERENCES [dbo].[ExpertSkills] ([ExpertSkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertCreditReservations] WITH CHECK ADD CONSTRAINT [FK_ExpertCreditReservations_CreditGrantId] FOREIGN KEY ([CreditGrantId]) REFERENCES [dbo].[ExpertCreditGrants] ([CreditGrantId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertCreditReservations] WITH CHECK ADD CONSTRAINT [FK_ExpertCreditReservations_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[LearningRequests] ([RequestId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessions] WITH CHECK ADD CONSTRAINT [FK_LearningSessions_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[LearningRequests] ([RequestId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessions] WITH CHECK ADD CONSTRAINT [FK_LearningSessions_TeacherUserId] FOREIGN KEY ([TeacherUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessions] WITH CHECK ADD CONSTRAINT [FK_LearningSessions_LearnerUserId] FOREIGN KEY ([LearnerUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessions] WITH CHECK ADD CONSTRAINT [FK_LearningSessions_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessions] WITH CHECK ADD CONSTRAINT [FK_LearningSessions_CreditReservationId] FOREIGN KEY ([CreditReservationId]) REFERENCES [dbo].[ExpertCreditReservations] ([CreditReservationId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessions] WITH CHECK ADD CONSTRAINT [FK_LearningSessions_CancelledByUserId] FOREIGN KEY ([CancelledByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[SessionConfirmations] WITH CHECK ADD CONSTRAINT [FK_SessionConfirmations_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[LearningSessions] ([SessionId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[SessionConfirmations] WITH CHECK ADD CONSTRAINT [FK_SessionConfirmations_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessionChanges] WITH CHECK ADD CONSTRAINT [FK_LearningSessionChanges_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[LearningSessions] ([SessionId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessionChanges] WITH CHECK ADD CONSTRAINT [FK_LearningSessionChanges_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessionChanges] WITH CHECK ADD CONSTRAINT [FK_LearningSessionChanges_RespondedByUserId] FOREIGN KEY ([RespondedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[Conversations] WITH CHECK ADD CONSTRAINT [FK_Conversations_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[LearningRequests] ([RequestId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ConversationParticipants] WITH CHECK ADD CONSTRAINT [FK_ConversationParticipants_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations] ([ConversationId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ConversationParticipants] WITH CHECK ADD CONSTRAINT [FK_ConversationParticipants_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ConversationParticipants] WITH CHECK ADD CONSTRAINT [FK_ConversationParticipants_LastReadMessageId] FOREIGN KEY ([LastReadMessageId]) REFERENCES [dbo].[Messages] ([MessageId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[Messages] WITH CHECK ADD CONSTRAINT [FK_Messages_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations] ([ConversationId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[Messages] WITH CHECK ADD CONSTRAINT [FK_Messages_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[SessionReviews] WITH CHECK ADD CONSTRAINT [FK_SessionReviews_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[LearningSessions] ([SessionId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[SessionReviews] WITH CHECK ADD CONSTRAINT [FK_SessionReviews_ReviewerUserId] FOREIGN KEY ([ReviewerUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[SessionReviews] WITH CHECK ADD CONSTRAINT [FK_SessionReviews_ReviewedUserId] FOREIGN KEY ([ReviewedUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[SessionReviews] WITH CHECK ADD CONSTRAINT [FK_SessionReviews_ModeratedByUserId] FOREIGN KEY ([ModeratedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[UserCareerPaths] WITH CHECK ADD CONSTRAINT [FK_UserCareerPaths_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[UserCareerPaths] WITH CHECK ADD CONSTRAINT [FK_UserCareerPaths_CareerGoalId] FOREIGN KEY ([CareerGoalId]) REFERENCES [dbo].[CareerGoals] ([CareerGoalId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[CareerPathKnownSkills] WITH CHECK ADD CONSTRAINT [FK_CareerPathKnownSkills_CareerPathId] FOREIGN KEY ([CareerPathId]) REFERENCES [dbo].[UserCareerPaths] ([CareerPathId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[CareerPathKnownSkills] WITH CHECK ADD CONSTRAINT [FK_CareerPathKnownSkills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[CareerPathSteps] WITH CHECK ADD CONSTRAINT [FK_CareerPathSteps_CareerPathId] FOREIGN KEY ([CareerPathId]) REFERENCES [dbo].[UserCareerPaths] ([CareerPathId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[CareerPathSteps] WITH CHECK ADD CONSTRAINT [FK_CareerPathSteps_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[CareerStepSessions] WITH CHECK ADD CONSTRAINT [FK_CareerStepSessions_CareerPathStepId] FOREIGN KEY ([CareerPathStepId]) REFERENCES [dbo].[CareerPathSteps] ([CareerPathStepId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[CareerStepSessions] WITH CHECK ADD CONSTRAINT [FK_CareerStepSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[LearningSessions] ([SessionId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[PaymentProviderEvents] WITH CHECK ADD CONSTRAINT [FK_PaymentProviderEvents_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [dbo].[Payments] ([PaymentId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[PaymentRefunds] WITH CHECK ADD CONSTRAINT [FK_PaymentRefunds_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [dbo].[Payments] ([PaymentId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[Messages] WITH CHECK ADD CONSTRAINT [FK_Messages_Composite_1] FOREIGN KEY ([ConversationId], [SenderUserId]) REFERENCES [dbo].[ConversationParticipants] ([ConversationId], [UserId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertSkills] WITH CHECK ADD CONSTRAINT [FK_ExpertSkills_Composite_2] FOREIGN KEY ([UserId], [SkillId]) REFERENCES [dbo].[UserSkills] ([UserId], [SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertApplications] WITH CHECK ADD CONSTRAINT [FK_ExpertApplications_Composite_3] FOREIGN KEY ([ApplicantUserId], [SkillId]) REFERENCES [dbo].[UserSkills] ([UserId], [SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ExpertSkills] WITH CHECK ADD CONSTRAINT [FK_ExpertSkills_Composite_4] FOREIGN KEY ([ApprovedApplicationId], [UserId], [SkillId]) REFERENCES [dbo].[ExpertApplications] ([ExpertApplicationId], [ApplicantUserId], [SkillId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[LearningSessions] WITH CHECK ADD CONSTRAINT [FK_LearningSessions_Composite_5] FOREIGN KEY ([CreditReservationId], [RequestId]) REFERENCES [dbo].[ExpertCreditReservations] ([CreditReservationId], [RequestId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE [dbo].[ConversationParticipants] WITH CHECK ADD CONSTRAINT [FK_ConversationParticipants_Composite_6] FOREIGN KEY ([ConversationId], [LastReadMessageId]) REFERENCES [dbo].[Messages] ([ConversationId], [MessageId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
CREATE UNIQUE INDEX [UX_ExpertApplications_1] ON [dbo].[ExpertApplications] ([ApplicantUserId], [SkillId]) WHERE ClosedAtUtc IS NULL;
CREATE UNIQUE INDEX [UX_UserSubscriptions_1] ON [dbo].[UserSubscriptions] ([ProviderCode], [ProviderSubscriptionId]) WHERE ProviderSubscriptionId IS NOT NULL;
CREATE UNIQUE INDEX [UX_Payments_2] ON [dbo].[Payments] ([ProviderCode], [ProviderPaymentId]) WHERE ProviderPaymentId IS NOT NULL;
CREATE UNIQUE INDEX [UX_LearningRequests_1] ON [dbo].[LearningRequests] ([RequesterUserId], [ProviderUserId], [SkillId]) WHERE ClosedAtUtc IS NULL;
CREATE UNIQUE INDEX [UX_LearningSessions_1] ON [dbo].[LearningSessions] ([CreditReservationId]) WHERE CreditReservationId IS NOT NULL;
CREATE UNIQUE INDEX [UX_LearningSessionChanges_1] ON [dbo].[LearningSessionChanges] ([SessionId]) WHERE ClosedAtUtc IS NULL;
CREATE UNIQUE INDEX [UX_UserCareerPaths_1] ON [dbo].[UserCareerPaths] ([UserId]) WHERE IsCurrent=1;
CREATE UNIQUE INDEX [UX_PaymentRefunds_2] ON [dbo].[PaymentRefunds] ([ProviderCode], [ProviderRefundId]) WHERE ProviderRefundId IS NOT NULL;
CREATE INDEX [IX_Skills_1] ON [dbo].[Skills] ([SubCategoryId], [IsActive], [AccessTier]);
CREATE INDEX [IX_UserSkills_2] ON [dbo].[UserSkills] ([SkillId], [CanTeach], [IsTeachingAvailable]);
CREATE INDEX [IX_UserAvailability_3] ON [dbo].[UserAvailability] ([UserId], [DayOfWeek]);
CREATE INDEX [IX_ExpertApplications_4] ON [dbo].[ExpertApplications] ([StatusCode], [SubmittedAtUtc]);
CREATE INDEX [IX_ExpertSkills_5] ON [dbo].[ExpertSkills] ([SkillId], [StatusCode]);
CREATE INDEX [IX_UserSubscriptions_6] ON [dbo].[UserSubscriptions] ([UserId], [StatusCode], [PaidThroughUtc]);
CREATE INDEX [IX_Payments_7] ON [dbo].[Payments] ([SubscriptionId], [StatusCode], [CreatedAtUtc]);
CREATE INDEX [IX_ExpertCreditGrants_8] ON [dbo].[ExpertCreditGrants] ([SubscriptionId], [ExpiresAtUtc]);
CREATE INDEX [IX_ExpertCreditReservations_9] ON [dbo].[ExpertCreditReservations] ([CreditGrantId], [StatusCode]);
CREATE INDEX [IX_LearningRequests_10] ON [dbo].[LearningRequests] ([ProviderUserId], [StatusCode], [CreatedAtUtc]);
CREATE INDEX [IX_LearningRequests_11] ON [dbo].[LearningRequests] ([RequesterUserId], [StatusCode], [CreatedAtUtc]);
CREATE INDEX [IX_LearningSessions_12] ON [dbo].[LearningSessions] ([TeacherUserId], [StartsAtUtc], [EndsAtUtc]);
CREATE INDEX [IX_LearningSessions_13] ON [dbo].[LearningSessions] ([LearnerUserId], [StartsAtUtc], [EndsAtUtc]);
CREATE INDEX [IX_LearningSessions_14] ON [dbo].[LearningSessions] ([RequestId], [StatusCode]);
CREATE INDEX [IX_LearningSessionChanges_15] ON [dbo].[LearningSessionChanges] ([SessionId], [CreatedAtUtc]);
CREATE INDEX [IX_ConversationParticipants_16] ON [dbo].[ConversationParticipants] ([UserId], [ConversationId]);
CREATE INDEX [IX_SessionReviews_18] ON [dbo].[SessionReviews] ([ReviewedUserId], [VisibilityCode]);
CREATE INDEX [IX_CareerPathSteps_20] ON [dbo].[CareerPathSteps] ([SkillId], [CompletedAtUtc]);

-- Reference data. Draft prices remain inactive.

INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES
(N'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', N'Member', N'MEMBER', N'18520f39-cc22-4439-9c82-12c9dcbf9010'),
(N'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', N'Admin', N'ADMIN', N'bc108c83-ddc4-43b7-a938-26f6dacd8dd1');

SET IDENTITY_INSERT [dbo].[Categories] ON;
INSERT INTO [dbo].[Categories] ([CategoryId], [Code], [NameEn], [NameAr], [IconKey], [SortOrder], [IsActive]) VALUES
(1, N'education', N'Education', N'التعليم', N'education', 1, 1),
(2, N'technology', N'Technology', N'التكنولوجيا', N'technology', 2, 1),
(3, N'handmade', N'Handmade', N'الأعمال اليدوية', N'handmade', 3, 1),
(4, N'entertainment', N'Entertainment', N'الترفيه', N'entertainment', 4, 1),
(5, N'business', N'Business', N'الأعمال', N'business', 5, 1),
(6, N'health-fitness', N'Health & Fitness', N'الصحة واللياقة', N'health-fitness', 6, 1);
SET IDENTITY_INSERT [dbo].[Categories] OFF;

SET IDENTITY_INSERT [dbo].[SubCategories] ON;
INSERT INTO [dbo].[SubCategories] ([SubCategoryId], [CategoryId], [Code], [NameEn], [NameAr], [SortOrder], [IsActive]) VALUES
(1, 2, N'web-development', N'Web Development', N'تطوير الويب', 1, 1),
(2, 2, N'ui-ux', N'UI/UX', N'تصميم تجربة المستخدم', 2, 1),
(3, 2, N'data-programming', N'Data & Programming', N'البيانات والبرمجة', 3, 1),
(4, 2, N'mobile-development', N'Mobile Development', N'تطوير الجوال', 4, 1),
(5, 2, N'devops', N'DevOps', N'التشغيل والتطوير', 5, 1),
(6, 3, N'visual-arts', N'Visual Arts', N'الفنون البصرية', 6, 1),
(7, 3, N'photography', N'Photography', N'التصوير', 7, 1);
SET IDENTITY_INSERT [dbo].[SubCategories] OFF;

SET IDENTITY_INSERT [dbo].[Skills] ON;
INSERT INTO [dbo].[Skills] ([SkillId], [SubCategoryId], [Slug], [NameEn], [NameAr], [LevelCode], [AccessTier], [IsActive]) VALUES
(1, 1, N'html', N'HTML Fundamentals', N'أساسيات HTML', N'Beginner', N'Basic', 1),
(2, 1, N'css', N'CSS & Responsive Design', N'CSS والتصميم المتجاوب', N'Beginner', N'Basic', 1),
(3, 1, N'javascript', N'JavaScript Basics', N'أساسيات JavaScript', N'Beginner', N'Basic', 1),
(4, 1, N'git-github', N'Git & GitHub', N'Git وGitHub', N'Beginner', N'Basic', 1),
(5, 1, N'react', N'Frontend Framework (React)', N'تطوير الواجهات باستخدام React', N'Advanced', N'Advanced', 1),
(6, 1, N'backend-development', N'Backend Development', N'تطوير الباك إند', N'Advanced', N'Advanced', 1),
(7, 1, N'database-apis', N'Database & APIs', N'قواعد البيانات وواجهات البرمجة', N'Advanced', N'Advanced', 1),
(8, 1, N'full-stack-project', N'Full Stack Project', N'مشروع متكامل', N'Advanced', N'Advanced', 1),
(9, 1, N'web-development', N'Web Development', N'تطوير الويب', N'Beginner', N'Basic', 1),
(10, 2, N'ui-ux-design', N'UI/UX Design', N'تصميم الواجهات وتجربة المستخدم', N'Beginner', N'Basic', 1),
(11, 6, N'graphic-design', N'Graphic Design', N'التصميم الجرافيكي', N'Beginner', N'Basic', 1),
(12, 2, N'user-research', N'User Research', N'بحث المستخدم', N'Intermediate', N'Advanced', 1),
(13, 3, N'python', N'Python', N'بايثون', N'Beginner', N'Basic', 1),
(14, 3, N'advanced-python', N'Advanced Python', N'بايثون متقدم', N'Advanced', N'Advanced', 1),
(15, 3, N'machine-learning', N'Machine Learning', N'تعلم الآلة', N'Advanced', N'Advanced', 1),
(16, 4, N'react-native', N'React Native', N'React Native', N'Advanced', N'Advanced', 1),
(17, 5, N'devops', N'DevOps', N'DevOps', N'Advanced', N'Advanced', 1),
(18, 7, N'photography', N'Photography', N'التصوير', N'Beginner', N'Basic', 1),
(19, 2, N'figma', N'Figma Basics', N'أساسيات Figma', N'Beginner', N'Basic', 1);
SET IDENTITY_INSERT [dbo].[Skills] OFF;

SET IDENTITY_INSERT [dbo].[SubscriptionPlans] ON;
INSERT INTO [dbo].[SubscriptionPlans] ([PlanId], [Code], [NameEn], [NameAr], [CanAccessAdvanced], [CanBookExperts], [ExpertCreditsPerMonth], [ExpertSessionMinutes], [IsActive]) VALUES
(1, N'Free', N'Free', N'مجاني', 0, 0, 0, 0, 1),
(2, N'Plus', N'Plus', N'بلس', 1, 1, 1, 30, 1);
SET IDENTITY_INSERT [dbo].[SubscriptionPlans] OFF;

SET IDENTITY_INSERT [dbo].[PlanPrices] ON;
INSERT INTO [dbo].[PlanPrices] ([PlanPriceId], [PlanId], [BillingMonths], [Amount], [Currency], [ValidFromUtc], [ValidUntilUtc], [IsActive]) VALUES
(1, 2, 1, N'9.00', N'USD', DATEADD(minute,0,DATEADD(day,-365,@NowUtc)), NULL, 0),
(2, 2, 12, N'84.00', N'USD', DATEADD(minute,0,DATEADD(day,-365,@NowUtc)), NULL, 0);
SET IDENTITY_INSERT [dbo].[PlanPrices] OFF;

SET IDENTITY_INSERT [dbo].[CareerGoals] ON;
INSERT INTO [dbo].[CareerGoals] ([CareerGoalId], [Code], [NameEn], [NameAr], [IsActive]) VALUES
(1, N'full-stack', N'Full Stack Web Developer', N'مطور ويب متكامل', 1),
(2, N'ui-ux', N'UI/UX Designer', N'مصمم واجهات وتجربة مستخدم', 1),
(3, N'data', N'Data Scientist', N'عالم بيانات', 1),
(4, N'graphic', N'Graphic Designer', N'مصمم جرافيك', 1),
(5, N'mobile', N'Mobile App Developer', N'مطور تطبيقات جوال', 1),
(6, N'devops', N'DevOps Engineer', N'مهندس تشغيل وتطوير', 1);
SET IDENTITY_INSERT [dbo].[CareerGoals] OFF;

IF @SeedDemoData=1
BEGIN

INSERT INTO [dbo].[AspNetUsers] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [AccountStatus], [CreatedAtUtc]) VALUES
(N'11111111-1111-4111-8111-111111111111', N'sara.free@example.test', N'SARA.FREE@EXAMPLE.TEST', N'sara.free@example.test', N'SARA.FREE@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAEOJuuyVvtPeSa16bZnHQLItNeoOtmSWncwnl03TNSJojzpkUfESXe98NDeuwG+B0RA==', N'27ca60e2-b321-4aba-866f-c5e68e32df12', N'ca747ffa-fe6d-4504-8321-fcf8bcfbc3f8', N'Active', DATEADD(minute,0,DATEADD(day,-120,@NowUtc))),
(N'22222222-2222-4222-8222-222222222222', N'omar.plus@example.test', N'OMAR.PLUS@EXAMPLE.TEST', N'omar.plus@example.test', N'OMAR.PLUS@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAEER++vlZFo6l3eiQRzeXaKd0rhHPCpMt8sgiefEEXZNFkEwyxnTHD7c4+4V7GPNOMw==', N'2af856e8-d3b6-4cf9-ad0e-fabf63ba0787', N'9f051ff9-db4d-4a9f-9c6a-8276c4311397', N'Active', DATEADD(minute,0,DATEADD(day,-120,@NowUtc))),
(N'33333333-3333-4333-8333-333333333333', N'lana.expert@example.test', N'LANA.EXPERT@EXAMPLE.TEST', N'lana.expert@example.test', N'LANA.EXPERT@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAENwEvdtzSnWOnLqKzgHKeyfglXs8EzJF7l9yEgQUbQAGoiCr8jL823XDbgWWSdiOmg==', N'afff42df-5c67-480b-8257-615a2511604d', N'2d3af9e2-d866-4405-8fd1-a34bfeaccd64', N'Active', DATEADD(minute,0,DATEADD(day,-120,@NowUtc))),
(N'44444444-4444-4444-8444-444444444444', N'admin@example.test', N'ADMIN@EXAMPLE.TEST', N'admin@example.test', N'ADMIN@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAEBx1GvCTY4x877NIgiExyYp6u8HO8bWHbJgNsvuvY3QoVOXeyFc5ontiWeRE4K+g4w==', N'90834337-1939-43c2-9aa0-71c2976da30d', N'7a70297a-ac9e-4b59-b4d8-1e4e735eca54', N'Active', DATEADD(minute,0,DATEADD(day,-120,@NowUtc))),
(N'55555555-5555-4555-8555-555555555555', N'ahmad.partner@example.test', N'AHMAD.PARTNER@EXAMPLE.TEST', N'ahmad.partner@example.test', N'AHMAD.PARTNER@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAEFgLWrnfqMPySlaoICiyv9H30CoqPp7CyWbdlXgctwpGaljV23iOKl/WOKhjvww/UQ==', N'5f3e5fc2-490d-4ef9-b610-d20fe6a0b98c', N'30aa305d-4fa5-44ce-97cc-553ee09f999d', N'Active', DATEADD(minute,0,DATEADD(day,-120,@NowUtc)));


INSERT INTO [dbo].[AspNetUserRoles] ([UserId], [RoleId]) VALUES
(N'11111111-1111-4111-8111-111111111111', N'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'),
(N'22222222-2222-4222-8222-222222222222', N'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'),
(N'33333333-3333-4333-8333-333333333333', N'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'),
(N'44444444-4444-4444-8444-444444444444', N'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'),
(N'55555555-5555-4555-8555-555555555555', N'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa');


INSERT INTO [dbo].[UserProfiles] ([UserId], [DisplayName], [Bio], [CountryCode], [City], [TimeZoneId], [AvailabilityNote], [UpdatedAtUtc]) VALUES
(N'11111111-1111-4111-8111-111111111111', N'Sara Free', N'حساب تجريبي لتجربة Skills Swap، وليس مستخدمًا حقيقيًا.', N'JO', N'Amman', N'Asia/Amman', N'Demo availability', @NowUtc),
(N'22222222-2222-4222-8222-222222222222', N'Omar Plus', N'حساب تجريبي لتجربة Skills Swap، وليس مستخدمًا حقيقيًا.', N'JO', N'Amman', N'Asia/Amman', N'Demo availability', @NowUtc),
(N'33333333-3333-4333-8333-333333333333', N'Lana Expert', N'حساب تجريبي لتجربة Skills Swap، وليس مستخدمًا حقيقيًا.', N'JO', N'Amman', N'Asia/Amman', N'Demo availability', @NowUtc),
(N'44444444-4444-4444-8444-444444444444', N'Trial Admin', N'حساب تجريبي لتجربة Skills Swap، وليس مستخدمًا حقيقيًا.', N'JO', N'Amman', N'Asia/Amman', N'Demo availability', @NowUtc),
(N'55555555-5555-4555-8555-555555555555', N'Ahmad Partner', N'حساب تجريبي لتجربة Skills Swap، وليس مستخدمًا حقيقيًا.', N'JO', N'Amman', N'Asia/Amman', N'Demo availability', @NowUtc);


INSERT INTO [dbo].[UserSkills] ([UserId], [SkillId], [WantsToLearn], [CanTeach], [KnowledgeLevel], [TeachingMode], [IsTeachingAvailable], [UpdatedAtUtc]) VALUES
(N'11111111-1111-4111-8111-111111111111', 1, 1, 0, N'Beginner', NULL, 0, @NowUtc),
(N'11111111-1111-4111-8111-111111111111', 3, 1, 0, N'Beginner', NULL, 0, @NowUtc),
(N'11111111-1111-4111-8111-111111111111', 11, 0, 1, N'Intermediate', N'Exchange', 1, @NowUtc),
(N'22222222-2222-4222-8222-222222222222', 5, 1, 0, N'Beginner', NULL, 0, @NowUtc),
(N'22222222-2222-4222-8222-222222222222', 3, 1, 0, N'Beginner', NULL, 0, @NowUtc),
(N'22222222-2222-4222-8222-222222222222', 13, 0, 0, N'Intermediate', NULL, 0, @NowUtc),
(N'22222222-2222-4222-8222-222222222222', 10, 1, 0, N'Unknown', NULL, 0, @NowUtc),
(N'33333333-3333-4333-8333-333333333333', 1, 0, 1, N'Advanced', N'Both', 1, @NowUtc),
(N'33333333-3333-4333-8333-333333333333', 3, 0, 1, N'Advanced', N'Both', 1, @NowUtc),
(N'33333333-3333-4333-8333-333333333333', 5, 0, 1, N'Advanced', N'Both', 1, @NowUtc),
(N'33333333-3333-4333-8333-333333333333', 10, 0, 1, N'Advanced', N'Both', 1, @NowUtc),
(N'55555555-5555-4555-8555-555555555555', 3, 0, 1, N'Advanced', N'Both', 1, @NowUtc),
(N'55555555-5555-4555-8555-555555555555', 9, 0, 1, N'Advanced', N'Both', 1, @NowUtc),
(N'55555555-5555-4555-8555-555555555555', 11, 1, 0, N'Unknown', NULL, 0, @NowUtc),
(N'55555555-5555-4555-8555-555555555555', 18, 0, 1, N'Beginner', N'Volunteer', 0, @NowUtc);


INSERT INTO [dbo].[UserAvailability] ([UserId], [DayOfWeek], [StartLocalTime], [EndLocalTime], [IsActive]) VALUES
(N'11111111-1111-4111-8111-111111111111', 0, N'09:00:00', N'22:00:00', 1),
(N'11111111-1111-4111-8111-111111111111', 1, N'09:00:00', N'22:00:00', 1),
(N'11111111-1111-4111-8111-111111111111', 2, N'09:00:00', N'22:00:00', 1),
(N'11111111-1111-4111-8111-111111111111', 3, N'09:00:00', N'22:00:00', 1),
(N'11111111-1111-4111-8111-111111111111', 4, N'09:00:00', N'22:00:00', 1),
(N'11111111-1111-4111-8111-111111111111', 5, N'09:00:00', N'22:00:00', 1),
(N'11111111-1111-4111-8111-111111111111', 6, N'09:00:00', N'22:00:00', 1),
(N'22222222-2222-4222-8222-222222222222', 0, N'09:00:00', N'22:00:00', 1),
(N'22222222-2222-4222-8222-222222222222', 1, N'09:00:00', N'22:00:00', 1),
(N'22222222-2222-4222-8222-222222222222', 2, N'09:00:00', N'22:00:00', 1),
(N'22222222-2222-4222-8222-222222222222', 3, N'09:00:00', N'22:00:00', 1),
(N'22222222-2222-4222-8222-222222222222', 4, N'09:00:00', N'22:00:00', 1),
(N'22222222-2222-4222-8222-222222222222', 5, N'09:00:00', N'22:00:00', 1),
(N'22222222-2222-4222-8222-222222222222', 6, N'09:00:00', N'22:00:00', 1),
(N'33333333-3333-4333-8333-333333333333', 0, N'09:00:00', N'22:00:00', 1),
(N'33333333-3333-4333-8333-333333333333', 1, N'09:00:00', N'22:00:00', 1),
(N'33333333-3333-4333-8333-333333333333', 2, N'09:00:00', N'22:00:00', 1),
(N'33333333-3333-4333-8333-333333333333', 3, N'09:00:00', N'22:00:00', 1),
(N'33333333-3333-4333-8333-333333333333', 4, N'09:00:00', N'22:00:00', 1),
(N'33333333-3333-4333-8333-333333333333', 5, N'09:00:00', N'22:00:00', 1),
(N'33333333-3333-4333-8333-333333333333', 6, N'09:00:00', N'22:00:00', 1),
(N'55555555-5555-4555-8555-555555555555', 0, N'09:00:00', N'22:00:00', 1),
(N'55555555-5555-4555-8555-555555555555', 1, N'09:00:00', N'22:00:00', 1),
(N'55555555-5555-4555-8555-555555555555', 2, N'09:00:00', N'22:00:00', 1),
(N'55555555-5555-4555-8555-555555555555', 3, N'09:00:00', N'22:00:00', 1),
(N'55555555-5555-4555-8555-555555555555', 4, N'09:00:00', N'22:00:00', 1),
(N'55555555-5555-4555-8555-555555555555', 5, N'09:00:00', N'22:00:00', 1),
(N'55555555-5555-4555-8555-555555555555', 6, N'09:00:00', N'22:00:00', 1);

SET IDENTITY_INSERT [dbo].[ExpertApplications] ON;
INSERT INTO [dbo].[ExpertApplications] ([ExpertApplicationId], [ApplicantUserId], [SkillId], [EvidenceRoute], [ExperienceYears], [Qualifications], [PortfolioUrl], [TeachingEvidence], [StatusCode], [ReviewedByUserId], [ReviewNote], [SubmittedAtUtc], [ReviewedAtUtc], [ClosedAtUtc]) VALUES
(1001, N'33333333-3333-4333-8333-333333333333', 5, N'Portfolio', 5, N'Practical teaching and portfolio evidence.', N'https://example.test/portfolio', N'Demo practical assessment completed.', N'Approved', N'44444444-4444-4444-8444-444444444444', N'Approved for this skill in the trial dataset.', DATEADD(minute,0,DATEADD(day,-100,@NowUtc)), DATEADD(minute,0,DATEADD(day,-98,@NowUtc)), DATEADD(minute,0,DATEADD(day,-98,@NowUtc))),
(1002, N'33333333-3333-4333-8333-333333333333', 3, N'Portfolio', 5, N'Practical teaching and portfolio evidence.', N'https://example.test/portfolio', N'Demo practical assessment completed.', N'Approved', N'44444444-4444-4444-8444-444444444444', N'Approved for this skill in the trial dataset.', DATEADD(minute,0,DATEADD(day,-100,@NowUtc)), DATEADD(minute,0,DATEADD(day,-98,@NowUtc)), DATEADD(minute,0,DATEADD(day,-98,@NowUtc))),
(1003, N'55555555-5555-4555-8555-555555555555', 9, N'Community', 3, N'Community teaching experience.', NULL, N'Pending manual review.', N'UnderReview', NULL, NULL, DATEADD(minute,0,DATEADD(day,-2,@NowUtc)), NULL, NULL),
(1004, N'55555555-5555-4555-8555-555555555555', 18, N'Portfolio', 0.5, N'Introductory photography.', NULL, N'Demo evidence requires improvement.', N'Rejected', N'44444444-4444-4444-8444-444444444444', N'Insufficient evidence in the sample.', DATEADD(minute,0,DATEADD(day,-20,@NowUtc)), DATEADD(minute,0,DATEADD(day,-18,@NowUtc)), DATEADD(minute,0,DATEADD(day,-18,@NowUtc)));
SET IDENTITY_INSERT [dbo].[ExpertApplications] OFF;

SET IDENTITY_INSERT [dbo].[ExpertSkills] ON;
INSERT INTO [dbo].[ExpertSkills] ([ExpertSkillId], [UserId], [SkillId], [ApprovedApplicationId], [StatusCode], [ApprovedByUserId], [ApprovedAtUtc]) VALUES
(101, N'33333333-3333-4333-8333-333333333333', 5, 1001, N'Active', N'44444444-4444-4444-8444-444444444444', DATEADD(minute,0,DATEADD(day,-98,@NowUtc))),
(102, N'33333333-3333-4333-8333-333333333333', 3, 1002, N'Active', N'44444444-4444-4444-8444-444444444444', DATEADD(minute,0,DATEADD(day,-98,@NowUtc)));
SET IDENTITY_INSERT [dbo].[ExpertSkills] OFF;

SET IDENTITY_INSERT [dbo].[UserSubscriptions] ON;
INSERT INTO [dbo].[UserSubscriptions] ([SubscriptionId], [UserId], [PlanPriceId], [StatusCode], [StartsAtUtc], [PaidThroughUtc], [CurrentBillingStartUtc], [CurrentBillingEndUtc], [CancelAtPeriodEnd], [ProviderCode], [ProviderSubscriptionId], [CreatedAtUtc], [ExpertCreditsPerMonthSnapshot], [ExpertSessionMinutesSnapshot], [CanAccessAdvancedSnapshot], [CanBookExpertsSnapshot]) VALUES
(901, N'22222222-2222-4222-8222-222222222222', 2, N'Active', DATEADD(month,-2,DATEADD(minute,-10,@NowUtc)), DATEADD(month,10,DATEADD(minute,-10,@NowUtc)), DATEADD(month,-2,DATEADD(minute,-10,@NowUtc)), DATEADD(month,10,DATEADD(minute,-10,@NowUtc)), 0, N'Demo', N'trial-sub-901', DATEADD(minute,-20,DATEADD(month,-2,DATEADD(minute,-10,@NowUtc))), 1, 30, 1, 1),
(902, N'55555555-5555-4555-8555-555555555555', 1, N'Pending', @NowUtc, @NowUtc, @NowUtc, DATEADD(month,1,@NowUtc), 0, N'Demo', N'trial-sub-902', @NowUtc, 1, 30, 1, 1);
SET IDENTITY_INSERT [dbo].[UserSubscriptions] OFF;

SET IDENTITY_INSERT [dbo].[Payments] ON;
INSERT INTO [dbo].[Payments] ([PaymentId], [SubscriptionId], [Amount], [Currency], [StatusCode], [ProviderCode], [ProviderPaymentId], [IdempotencyKey], [CreatedAtUtc], [PaidAtUtc]) VALUES
(801, 901, 84, N'USD', N'Succeeded', N'Demo', N'trial-pay-801', N'd7c9e048-577d-4036-a1d8-005257e3811f', DATEADD(month,-2,DATEADD(minute,-10,@NowUtc)), DATEADD(month,-2,DATEADD(minute,-10,@NowUtc))),
(802, 901, 84, N'USD', N'Failed', N'Demo', N'trial-pay-802', N'f254d547-14fa-45f5-81df-3dde0947ebab', DATEADD(minute,-10,DATEADD(month,-2,DATEADD(minute,-10,@NowUtc))), NULL),
(803, 902, 9, N'USD', N'Pending', N'Demo', N'trial-pay-803', N'e9140957-85c8-425d-b411-eda98198b11a', @NowUtc, NULL);
SET IDENTITY_INSERT [dbo].[Payments] OFF;

SET IDENTITY_INSERT [dbo].[ExpertCreditGrants] ON;
INSERT INTO [dbo].[ExpertCreditGrants] ([CreditGrantId], [SubscriptionId], [PeriodStartUtc], [PeriodEndUtc], [ExpiresAtUtc], [Quantity], [SessionMinutes], [CreatedAtUtc]) VALUES
(501, 901, DATEADD(month,-2,DATEADD(minute,-10,@NowUtc)), DATEADD(month,-1,DATEADD(minute,-10,@NowUtc)), DATEADD(month,-1,DATEADD(minute,-10,@NowUtc)), 1, 30, DATEADD(month,-2,DATEADD(minute,-10,@NowUtc))),
(502, 901, DATEADD(month,-1,DATEADD(minute,-10,@NowUtc)), DATEADD(month,0,DATEADD(minute,-10,@NowUtc)), DATEADD(month,0,DATEADD(minute,-10,@NowUtc)), 1, 30, DATEADD(month,-1,DATEADD(minute,-10,@NowUtc))),
(503, 901, DATEADD(month,0,DATEADD(minute,-10,@NowUtc)), DATEADD(month,1,DATEADD(minute,-10,@NowUtc)), DATEADD(month,1,DATEADD(minute,-10,@NowUtc)), 1, 30, DATEADD(month,0,DATEADD(minute,-10,@NowUtc)));
SET IDENTITY_INSERT [dbo].[ExpertCreditGrants] OFF;

SET IDENTITY_INSERT [dbo].[LearningRequests] ON;
INSERT INTO [dbo].[LearningRequests] ([RequestId], [RequesterUserId], [ProviderUserId], [SkillId], [OfferedSkillId], [ExpertSkillId], [RequestType], [StatusCode], [GoalMessage], [PreferredTimeCode], [ProposedStartUtc], [ProposedEndUtc], [CreatedAtUtc], [RespondedAtUtc], [ClosedAtUtc]) VALUES
(2001, N'11111111-1111-4111-8111-111111111111', N'33333333-3333-4333-8333-333333333333', 1, NULL, NULL, N'Volunteer', N'Completed', N'Trial request: practise one clear learning goal with a partner.', N'Flexible', NULL, NULL, DATEADD(minute,0,DATEADD(day,-10,@NowUtc)), DATEADD(minute,0,DATEADD(day,-10,@NowUtc)), DATEADD(minute,0,DATEADD(hour,11,DATEADD(day,-7,@Day0Utc)))),
(2002, N'11111111-1111-4111-8111-111111111111', N'55555555-5555-4555-8555-555555555555', 3, 11, NULL, N'Exchange', N'Accepted', N'Trial request: practise one clear learning goal with a partner.', N'Flexible', NULL, NULL, DATEADD(minute,0,DATEADD(day,-6,@NowUtc)), DATEADD(minute,0,DATEADD(day,-6,@NowUtc)), NULL),
(2003, N'22222222-2222-4222-8222-222222222222', N'33333333-3333-4333-8333-333333333333', 5, NULL, 101, N'Expert', N'Completed', N'Trial request: practise one clear learning goal with a partner.', NULL, DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,-50,@Day0Utc))), DATEADD(minute,30,DATEADD(hour,10,DATEADD(day,-50,@Day0Utc))), DATEADD(minute,0,DATEADD(day,-55,@NowUtc)), DATEADD(minute,0,DATEADD(day,-55,@NowUtc)), DATEADD(minute,0,DATEADD(hour,11,DATEADD(day,-50,@Day0Utc)))),
(2004, N'22222222-2222-4222-8222-222222222222', N'33333333-3333-4333-8333-333333333333', 3, NULL, 102, N'Expert', N'Pending', N'Trial request: practise one clear learning goal with a partner.', NULL, DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,3,@Day0Utc))), DATEADD(minute,30,DATEADD(hour,10,DATEADD(day,3,@Day0Utc))), DATEADD(minute,-5,@NowUtc), NULL, NULL),
(2005, N'22222222-2222-4222-8222-222222222222', N'33333333-3333-4333-8333-333333333333', 5, NULL, 101, N'Expert', N'Cancelled', N'Trial request: practise one clear learning goal with a partner.', NULL, DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,-20,@Day0Utc))), DATEADD(minute,30,DATEADD(hour,10,DATEADD(day,-20,@Day0Utc))), DATEADD(minute,0,DATEADD(day,-25,@NowUtc)), DATEADD(minute,0,DATEADD(day,-25,@NowUtc)), DATEADD(minute,0,DATEADD(day,-24,@NowUtc)));
SET IDENTITY_INSERT [dbo].[LearningRequests] OFF;

SET IDENTITY_INSERT [dbo].[ExpertCreditReservations] ON;
INSERT INTO [dbo].[ExpertCreditReservations] ([CreditReservationId], [CreditGrantId], [RequestId], [StatusCode], [ReservedAtUtc], [ReservationExpiresAtUtc], [ConsumedAtUtc], [ReleasedAtUtc], [ReleaseReason]) VALUES
(601, 501, 2003, N'Consumed', DATEADD(minute,0,DATEADD(day,-55,@NowUtc)), DATEADD(minute,20,DATEADD(day,-55,@NowUtc)), DATEADD(minute,0,DATEADD(hour,11,DATEADD(day,-50,@Day0Utc))), NULL, NULL),
(602, 502, 2005, N'Released', DATEADD(minute,0,DATEADD(day,-25,@NowUtc)), DATEADD(minute,20,DATEADD(day,-25,@NowUtc)), NULL, DATEADD(minute,0,DATEADD(day,-24,@NowUtc)), N'Early cancellation in trial'),
(603, 503, 2004, N'Reserved', DATEADD(minute,-5,@NowUtc), DATEADD(minute,15,@NowUtc), NULL, NULL, NULL);
SET IDENTITY_INSERT [dbo].[ExpertCreditReservations] OFF;

SET IDENTITY_INSERT [dbo].[LearningSessions] ON;
INSERT INTO [dbo].[LearningSessions] ([SessionId], [RequestId], [TeacherUserId], [LearnerUserId], [SkillId], [CreditReservationId], [StartsAtUtc], [EndsAtUtc], [StatusCode], [MeetingProvider], [CancelledByUserId], [CancellationReason], [CreatedAtUtc], [CompletedAtUtc], [IdempotencyKey]) VALUES
(3001, 2001, N'33333333-3333-4333-8333-333333333333', N'11111111-1111-4111-8111-111111111111', 1, NULL, DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,-7,@Day0Utc))), DATEADD(minute,30,DATEADD(hour,10,DATEADD(day,-7,@Day0Utc))), N'Completed', N'Chat', NULL, NULL, DATEADD(minute,0,DATEADD(day,-9,@NowUtc)), DATEADD(minute,0,DATEADD(hour,11,DATEADD(day,-7,@Day0Utc))), N'55478922-a04b-41da-8375-c424c3e48c4c'),
(3002, 2002, N'55555555-5555-4555-8555-555555555555', N'11111111-1111-4111-8111-111111111111', 3, NULL, DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,-4,@Day0Utc))), DATEADD(minute,45,DATEADD(hour,10,DATEADD(day,-4,@Day0Utc))), N'Completed', N'Chat', NULL, NULL, DATEADD(minute,0,DATEADD(day,-5,@NowUtc)), DATEADD(minute,0,DATEADD(hour,11,DATEADD(day,-4,@Day0Utc))), N'd1305933-9f1f-4416-bcf6-607abc86e9d3'),
(3003, 2002, N'11111111-1111-4111-8111-111111111111', N'55555555-5555-4555-8555-555555555555', 11, NULL, DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,5,@Day0Utc))), DATEADD(minute,45,DATEADD(hour,10,DATEADD(day,5,@Day0Utc))), N'Scheduled', N'Chat', NULL, NULL, DATEADD(minute,0,DATEADD(day,-5,@NowUtc)), NULL, N'10207dca-7fa4-4fe0-aca6-3270016e1050'),
(3004, 2003, N'33333333-3333-4333-8333-333333333333', N'22222222-2222-4222-8222-222222222222', 5, 601, DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,-50,@Day0Utc))), DATEADD(minute,30,DATEADD(hour,10,DATEADD(day,-50,@Day0Utc))), N'Completed', N'Chat', NULL, NULL, DATEADD(minute,0,DATEADD(day,-54,@NowUtc)), DATEADD(minute,0,DATEADD(hour,11,DATEADD(day,-50,@Day0Utc))), N'291f5581-c58f-4bbc-b01d-ab603db0b172'),
(3005, 2005, N'33333333-3333-4333-8333-333333333333', N'22222222-2222-4222-8222-222222222222', 5, 602, DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,-20,@Day0Utc))), DATEADD(minute,30,DATEADD(hour,10,DATEADD(day,-20,@Day0Utc))), N'Cancelled', N'Chat', N'22222222-2222-4222-8222-222222222222', N'Trial early cancellation; reservation released once.', DATEADD(minute,0,DATEADD(day,-25,@NowUtc)), NULL, N'86482080-e865-448c-905f-004fe2b333bd');
SET IDENTITY_INSERT [dbo].[LearningSessions] OFF;


INSERT INTO [dbo].[SessionConfirmations] ([SessionId], [UserId], [Attended], [CompletionConfirmedAtUtc], [UpdatedAtUtc]) VALUES
(3001, N'33333333-3333-4333-8333-333333333333', 1, DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-7,@Day0Utc))), DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-7,@Day0Utc)))),
(3001, N'11111111-1111-4111-8111-111111111111', 1, DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-7,@Day0Utc))), DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-7,@Day0Utc)))),
(3002, N'55555555-5555-4555-8555-555555555555', 1, DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-4,@Day0Utc))), DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-4,@Day0Utc)))),
(3002, N'11111111-1111-4111-8111-111111111111', 1, DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-4,@Day0Utc))), DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-4,@Day0Utc)))),
(3004, N'33333333-3333-4333-8333-333333333333', 1, DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-50,@Day0Utc))), DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-50,@Day0Utc)))),
(3004, N'22222222-2222-4222-8222-222222222222', 1, DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-50,@Day0Utc))), DATEADD(minute,50,DATEADD(hour,10,DATEADD(day,-50,@Day0Utc))));

SET IDENTITY_INSERT [dbo].[LearningSessionChanges] ON;
INSERT INTO [dbo].[LearningSessionChanges] ([SessionChangeId], [SessionId], [RequestedByUserId], [ChangeType], [OriginalStartsAtUtc], [OriginalEndsAtUtc], [ProposedStartsAtUtc], [ProposedEndsAtUtc], [Reason], [StatusCode], [DecisionSource], [RespondedByUserId], [ResponseNote], [CreatedAtUtc], [RespondedAtUtc], [ClosedAtUtc]) VALUES
(9001, 3003, N'55555555-5555-4555-8555-555555555555', N'Reschedule', DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,4,@Day0Utc))), DATEADD(minute,45,DATEADD(hour,10,DATEADD(day,4,@Day0Utc))), DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,5,@Day0Utc))), DATEADD(minute,45,DATEADD(hour,10,DATEADD(day,5,@Day0Utc))), N'Please move the session by one day.', N'Accepted', N'Participant', N'11111111-1111-4111-8111-111111111111', N'Agreed.', DATEADD(minute,0,DATEADD(day,-2,@NowUtc)), DATEADD(minute,0,DATEADD(day,-1,@NowUtc)), DATEADD(minute,0,DATEADD(day,-1,@NowUtc))),
(9002, 3003, N'11111111-1111-4111-8111-111111111111', N'Cancel', DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,5,@Day0Utc))), DATEADD(minute,45,DATEADD(hour,10,DATEADD(day,5,@Day0Utc))), NULL, NULL, N'Trial pending cancellation request.', N'Pending', NULL, NULL, NULL, @NowUtc, NULL, NULL),
(9003, 3005, N'22222222-2222-4222-8222-222222222222', N'Cancel', DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,-20,@Day0Utc))), DATEADD(minute,30,DATEADD(hour,10,DATEADD(day,-20,@Day0Utc))), NULL, NULL, N'Early cancellation in sample.', N'Accepted', N'Policy', NULL, N'Credit released once.', DATEADD(minute,0,DATEADD(day,-24,@NowUtc)), DATEADD(minute,0,DATEADD(day,-24,@NowUtc)), DATEADD(minute,0,DATEADD(day,-24,@NowUtc)));
SET IDENTITY_INSERT [dbo].[LearningSessionChanges] OFF;

SET IDENTITY_INSERT [dbo].[Conversations] ON;
INSERT INTO [dbo].[Conversations] ([ConversationId], [RequestId], [CreatedAtUtc]) VALUES
(4001, 2001, DATEADD(minute,0,DATEADD(day,-9,@NowUtc))),
(4002, 2002, DATEADD(minute,0,DATEADD(day,-5,@NowUtc))),
(4003, 2003, DATEADD(minute,0,DATEADD(day,-54,@NowUtc))),
(4004, 2005, DATEADD(minute,0,DATEADD(day,-25,@NowUtc)));
SET IDENTITY_INSERT [dbo].[Conversations] OFF;


INSERT INTO [dbo].[ConversationParticipants] ([ConversationId], [UserId], [JoinedAtUtc], [LastReadMessageId]) VALUES
(4001, N'11111111-1111-4111-8111-111111111111', DATEADD(minute,0,DATEADD(day,-9,@NowUtc)), NULL),
(4001, N'33333333-3333-4333-8333-333333333333', DATEADD(minute,0,DATEADD(day,-9,@NowUtc)), NULL),
(4002, N'11111111-1111-4111-8111-111111111111', DATEADD(minute,0,DATEADD(day,-5,@NowUtc)), NULL),
(4002, N'55555555-5555-4555-8555-555555555555', DATEADD(minute,0,DATEADD(day,-5,@NowUtc)), NULL),
(4003, N'22222222-2222-4222-8222-222222222222', DATEADD(minute,0,DATEADD(day,-54,@NowUtc)), NULL),
(4003, N'33333333-3333-4333-8333-333333333333', DATEADD(minute,0,DATEADD(day,-54,@NowUtc)), NULL),
(4004, N'22222222-2222-4222-8222-222222222222', DATEADD(minute,0,DATEADD(day,-25,@NowUtc)), NULL),
(4004, N'33333333-3333-4333-8333-333333333333', DATEADD(minute,0,DATEADD(day,-25,@NowUtc)), NULL);

SET IDENTITY_INSERT [dbo].[Messages] ON;
INSERT INTO [dbo].[Messages] ([MessageId], [ConversationId], [SenderUserId], [Body], [SentAtUtc], [ClientMessageId]) VALUES
(5001, 4001, N'11111111-1111-4111-8111-111111111111', N'مرحبًا، أود الاتفاق على هدف واضح للجلسة.', DATEADD(minute,5,DATEADD(day,-9,@NowUtc)), N'd0bf7a59-b20f-4fc7-a164-d6d52192fa74'),
(5002, 4001, N'33333333-3333-4333-8333-333333333333', N'Welcome. Let us agree on a suitable time.', DATEADD(minute,10,DATEADD(day,-9,@NowUtc)), N'684009f7-c16d-41af-bc78-60be58caefbe'),
(5003, 4002, N'11111111-1111-4111-8111-111111111111', N'مرحبًا، أود الاتفاق على هدف واضح للجلسة.', DATEADD(minute,5,DATEADD(day,-5,@NowUtc)), N'4d8f8373-a786-43bf-933c-2eb0ac4a67b9'),
(5004, 4002, N'55555555-5555-4555-8555-555555555555', N'Welcome. Let us agree on a suitable time.', DATEADD(minute,10,DATEADD(day,-5,@NowUtc)), N'8f7992c1-0918-4b7f-a178-a46f0ca40a72'),
(5005, 4003, N'22222222-2222-4222-8222-222222222222', N'مرحبًا، أود الاتفاق على هدف واضح للجلسة.', DATEADD(minute,5,DATEADD(day,-54,@NowUtc)), N'99dab24e-3e6d-495a-9420-fd6f707fdcfd'),
(5006, 4003, N'33333333-3333-4333-8333-333333333333', N'Welcome. Let us agree on a suitable time.', DATEADD(minute,10,DATEADD(day,-54,@NowUtc)), N'50b5deb3-767b-46dc-b98f-ba0881e94112'),
(5007, 4004, N'22222222-2222-4222-8222-222222222222', N'مرحبًا، أود الاتفاق على هدف واضح للجلسة.', DATEADD(minute,5,DATEADD(day,-25,@NowUtc)), N'e61c9b68-9ea3-4275-a5c3-08b0f0547a17'),
(5008, 4004, N'33333333-3333-4333-8333-333333333333', N'Welcome. Let us agree on a suitable time.', DATEADD(minute,10,DATEADD(day,-25,@NowUtc)), N'be18cc8d-31bf-4d0f-9c52-afb53fe4dfb1');
SET IDENTITY_INSERT [dbo].[Messages] OFF;

SET IDENTITY_INSERT [dbo].[SessionReviews] ON;
INSERT INTO [dbo].[SessionReviews] ([ReviewId], [SessionId], [ReviewerUserId], [ReviewedUserId], [Rating], [Comment], [VisibilityCode], [ModeratedByUserId], [ModerationReason], [CreatedAtUtc]) VALUES
(6001, 3001, N'11111111-1111-4111-8111-111111111111', N'33333333-3333-4333-8333-333333333333', 5, N'تجربة تعليمية مفيدة وواضحة — بيانات تجريبية.', N'Published', NULL, NULL, DATEADD(minute,0,DATEADD(hour,12,DATEADD(day,-7,@Day0Utc)))),
(6002, 3001, N'33333333-3333-4333-8333-333333333333', N'11111111-1111-4111-8111-111111111111', 5, N'تجربة تعليمية مفيدة وواضحة — بيانات تجريبية.', N'Hidden', N'44444444-4444-4444-8444-444444444444', N'Trial moderation example', DATEADD(minute,0,DATEADD(hour,12,DATEADD(day,-7,@Day0Utc)))),
(6003, 3002, N'11111111-1111-4111-8111-111111111111', N'55555555-5555-4555-8555-555555555555', 4, N'تجربة تعليمية مفيدة وواضحة — بيانات تجريبية.', N'Published', NULL, NULL, DATEADD(minute,0,DATEADD(hour,12,DATEADD(day,-4,@Day0Utc)))),
(6004, 3002, N'55555555-5555-4555-8555-555555555555', N'11111111-1111-4111-8111-111111111111', 4, N'تجربة تعليمية مفيدة وواضحة — بيانات تجريبية.', N'Published', NULL, NULL, DATEADD(minute,0,DATEADD(hour,12,DATEADD(day,-4,@Day0Utc)))),
(6005, 3004, N'22222222-2222-4222-8222-222222222222', N'33333333-3333-4333-8333-333333333333', 5, N'تجربة تعليمية مفيدة وواضحة — بيانات تجريبية.', N'Published', NULL, NULL, DATEADD(minute,0,DATEADD(hour,12,DATEADD(day,-50,@Day0Utc)))),
(6006, 3004, N'33333333-3333-4333-8333-333333333333', N'22222222-2222-4222-8222-222222222222', 5, N'تجربة تعليمية مفيدة وواضحة — بيانات تجريبية.', N'Published', NULL, NULL, DATEADD(minute,0,DATEADD(hour,12,DATEADD(day,-50,@Day0Utc))));
SET IDENTITY_INSERT [dbo].[SessionReviews] OFF;

SET IDENTITY_INSERT [dbo].[UserCareerPaths] ON;
INSERT INTO [dbo].[UserCareerPaths] ([CareerPathId], [UserId], [CareerGoalId], [StartingLevel], [TimeCommitment], [SourceCode], [RecommendationVersion], [StatusCode], [IsCurrent], [CreatedAtUtc]) VALUES
(701, N'11111111-1111-4111-8111-111111111111', 1, N'Beginner', N'Moderate', N'RuleBased', N'trial-v1', N'Active', 1, DATEADD(minute,0,DATEADD(day,-12,@NowUtc))),
(702, N'22222222-2222-4222-8222-222222222222', 2, N'Beginner', N'Moderate', N'RuleBased', N'trial-v1', N'Active', 1, DATEADD(minute,0,DATEADD(day,-15,@NowUtc))),
(703, N'11111111-1111-4111-8111-111111111111', 4, N'Beginner', N'Moderate', N'RuleBased', N'trial-v1', N'Archived', 0, DATEADD(minute,0,DATEADD(day,-30,@NowUtc)));
SET IDENTITY_INSERT [dbo].[UserCareerPaths] OFF;


INSERT INTO [dbo].[CareerPathKnownSkills] ([CareerPathId], [SkillId]) VALUES
(702, 13),
(703, 3);

SET IDENTITY_INSERT [dbo].[CareerPathSteps] ON;
INSERT INTO [dbo].[CareerPathSteps] ([CareerPathStepId], [CareerPathId], [SkillId], [StepOrder], [TitleEn], [TitleAr], [RequiredCompletedSessions], [StartedAtUtc], [CompletedAtUtc]) VALUES
(7101, 701, 1, 1, N'HTML Fundamentals', N'أساسيات HTML', 1, DATEADD(minute,0,DATEADD(hour,10,DATEADD(day,-7,@Day0Utc))), DATEADD(minute,5,DATEADD(hour,11,DATEADD(day,-7,@Day0Utc)))),
(7102, 701, 2, 2, N'CSS & Responsive Design', N'CSS والتصميم المتجاوب', 1, NULL, NULL),
(7103, 701, 3, 3, N'JavaScript Basics', N'أساسيات JavaScript', 1, NULL, NULL),
(7104, 701, 4, 4, N'Git & GitHub', N'Git وGitHub', 1, NULL, NULL),
(7105, 701, 5, 5, N'Frontend Framework (React)', N'تطوير الواجهات باستخدام React', 1, NULL, NULL),
(7106, 701, 6, 6, N'Backend Development', N'تطوير الباك إند', 1, NULL, NULL),
(7107, 701, 7, 7, N'Database & APIs', N'قواعد البيانات وواجهات البرمجة', 1, NULL, NULL),
(7108, 701, 8, 8, N'Full Stack Project', N'مشروع متكامل', 1, NULL, NULL),
(7201, 702, 10, 1, N'UI/UX Design', N'تصميم الواجهات وتجربة المستخدم', 1, NULL, NULL),
(7202, 702, 11, 2, N'Graphic Design', N'التصميم الجرافيكي', 1, NULL, NULL),
(7203, 702, 12, 3, N'User Research', N'بحث المستخدم', 1, NULL, NULL),
(7301, 703, 11, 1, N'Graphic Design', N'التصميم الجرافيكي', 1, NULL, NULL),
(7302, 703, 10, 2, N'UI/UX Design', N'تصميم الواجهات وتجربة المستخدم', 1, NULL, NULL),
(7303, 703, 12, 3, N'User Research', N'بحث المستخدم', 1, NULL, NULL);
SET IDENTITY_INSERT [dbo].[CareerPathSteps] OFF;


INSERT INTO [dbo].[CareerStepSessions] ([CareerPathStepId], [SessionId], [LinkedAtUtc]) VALUES
(7101, 3001, DATEADD(minute,5,DATEADD(hour,11,DATEADD(day,-7,@Day0Utc))));

SET IDENTITY_INSERT [dbo].[PaymentProviderEvents] ON;
INSERT INTO [dbo].[PaymentProviderEvents] ([ProviderEventId], [ProviderCode], [ExternalEventId], [EventType], [PaymentId], [ReceivedAtUtc], [ProcessedAtUtc], [ProcessingState]) VALUES
(10001, N'Demo', N'trial-event-10001', N'payment.succeeded', 801, @NowUtc, @NowUtc, N'Processed'),
(10002, N'Demo', N'trial-event-10002', N'payment.failed', 802, @NowUtc, @NowUtc, N'Processed'),
(10003, N'Demo', N'trial-event-10003', N'payment.pending', 803, @NowUtc, NULL, N'Pending');
SET IDENTITY_INSERT [dbo].[PaymentProviderEvents] OFF;

SET IDENTITY_INSERT [dbo].[PaymentRefunds] ON;
INSERT INTO [dbo].[PaymentRefunds] ([RefundId], [PaymentId], [Amount], [ProviderCode], [StatusCode], [ProviderRefundId], [IdempotencyKey], [Reason], [CreatedAtUtc], [CompletedAtUtc]) VALUES
(11001, 801, 5, N'Demo', N'Succeeded', N'trial-refund-11001', N'de6c7d13-4e9e-4379-a7f6-9e77cd8829d2', N'Partial refund example for database testing only.', DATEADD(day,2,DATEADD(month,-2,DATEADD(minute,-10,@NowUtc))), DATEADD(day,2,DATEADD(month,-2,DATEADD(minute,-10,@NowUtc))));
SET IDENTITY_INSERT [dbo].[PaymentRefunds] OFF;
UPDATE p SET LastReadMessageId=x.LastMessageId FROM dbo.ConversationParticipants AS p CROSS APPLY (SELECT MAX(MessageId) AS LastMessageId FROM dbo.Messages AS m WHERE m.ConversationId=p.ConversationId) AS x WHERE p.UserId IN (N'11111111-1111-4111-8111-111111111111',N'22222222-2222-4222-8222-222222222222',N'33333333-3333-4333-8333-333333333333',N'44444444-4444-4444-8444-444444444444',N'55555555-5555-4555-8555-555555555555');

END;


    -- Automatic verification. Failure rolls back this new schema and its sample rows.
    IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>36
        THROW 51201,'Unexpected table count. Creation rolled back.',1;
    IF (SELECT COUNT(*) FROM sys.foreign_keys)<>69
        THROW 51202,'Unexpected foreign key count. Creation rolled back.',1;
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
        THROW 51203,'A foreign key is disabled or not trusted.',1;
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1)
        THROW 51204,'A CHECK constraint is disabled or not trusted.',1;
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

    IF EXISTS(SELECT 1 FROM @Rules WHERE Violations<>0)
    BEGIN
        SELECT CheckName,Violations FROM @Rules WHERE Violations<>0;
        THROW 51205,'Data consistency check failed. Creation rolled back.',1;
    END;
    IF @SeedDemoData=1
    BEGIN
        DECLARE @InitialCounts TABLE(TableName sysname,RecordsCount bigint);
        INSERT @InitialCounts
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
        IF (SELECT SUM(RecordsCount) FROM @InitialCounts)<>188
            THROW 51206,'Unexpected demo record total. Creation rolled back.',1;
    END;

    EXEC sys.sp_addextendedproperty @name=N'SkillsSwap.SchemaVersion',@value=N'1.0';
    COMMIT TRANSACTION;
    PRINT N'SkillsSwap_Final created successfully. Database checks passed.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
END
ELSE PRINT N'Schema already exists. No data was duplicated or overwritten.';


-- Read-only summary, also returned when this file is run a second time.
SELECT DB_NAME() AS DatabaseName,
       (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) AS TablesCount,
       (SELECT COUNT(*) FROM sys.foreign_keys) AS ForeignKeysCount,
       (SELECT COUNT(*) FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) AS BadForeignKeys,
       (SELECT COUNT(*) FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) AS BadCheckConstraints;
