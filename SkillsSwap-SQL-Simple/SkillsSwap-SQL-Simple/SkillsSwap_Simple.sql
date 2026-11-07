/* =====================================================================
   Skills Swap - simplified database (SQL Server 2019+)
   Database: SkillsSwap
   Fitted to the current frontend (member pages + admin pages).
   - ASP.NET Core Identity tables with Guid keys (IdentityUser<Guid>).
   - Profile columns live directly on AspNetUsers (no separate profile table).
   - Counts shown on dashboards (ratings, swaps, users per skill, revenue)
     are calculated with queries, not stored.
   Run the whole file in SSMS (F5) on a server that has no SkillsSwap database,
   or on an empty SkillsSwap database.
   Demo password for all demo accounts: SkillsSwap_Demo!2026
   ===================================================================== */
USE [master];
GO
IF DB_ID(N'SkillsSwap') IS NULL
    CREATE DATABASE [SkillsSwap];
GO
USE [SkillsSwap];
GO
IF OBJECT_ID(N'dbo.Skills', N'U') IS NOT NULL
BEGIN
    RAISERROR(N'SkillsSwap already has tables. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* =====================================================================
   1. ACCOUNTS (ASP.NET Core Identity)
   ===================================================================== */
CREATE TABLE dbo.AspNetUsers (
    Id                    uniqueidentifier NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
    UserName              nvarchar(256)  NULL,
    NormalizedUserName    nvarchar(256)  NULL,
    Email                 nvarchar(256)  NULL,
    NormalizedEmail       nvarchar(256)  NULL,
    EmailConfirmed        bit            NOT NULL CONSTRAINT DF_AspNetUsers_EmailConfirmed DEFAULT (0),
    PasswordHash          nvarchar(max)  NULL,
    SecurityStamp         nvarchar(max)  NULL,
    ConcurrencyStamp      nvarchar(max)  NULL,
    PhoneNumber           nvarchar(max)  NULL,
    PhoneNumberConfirmed  bit            NOT NULL CONSTRAINT DF_AspNetUsers_PhoneNumberConfirmed DEFAULT (0),
    TwoFactorEnabled      bit            NOT NULL CONSTRAINT DF_AspNetUsers_TwoFactorEnabled DEFAULT (0),
    LockoutEnd            datetimeoffset NULL,
    LockoutEnabled        bit            NOT NULL CONSTRAINT DF_AspNetUsers_LockoutEnabled DEFAULT (1),
    AccessFailedCount     int            NOT NULL CONSTRAINT DF_AspNetUsers_AccessFailedCount DEFAULT (0),
    -- Profile (signup, profile setup, profile edit dialog, admin profile)
    FullName              nvarchar(120)  NOT NULL,
    Location              nvarchar(120)  NULL,      -- "Location" in setup, "Country" in edit dialog
    Bio                   nvarchar(1500) NULL,      -- "About"
    AvatarPath            nvarchar(500)  NULL,      -- JPEG/PNG/WebP up to 2 MB, file stored on disk
    -- Account state (admin members page and edit user page)
    AccountStatus         varchar(20)    NOT NULL CONSTRAINT DF_AspNetUsers_AccountStatus DEFAULT ('Active'),
    StatusReason          nvarchar(1000) NULL,      -- required by admin when suspending/banning
    CreatedAt             datetime2(0)   NOT NULL CONSTRAINT DF_AspNetUsers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_AspNetUsers_AccountStatus CHECK (AccountStatus IN ('Active', 'Suspended', 'Banned')),
    CONSTRAINT CK_AspNetUsers_StatusReason  CHECK (AccountStatus = 'Active' OR StatusReason IS NOT NULL)
);
CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);

CREATE TABLE dbo.AspNetRoles (
    Id               uniqueidentifier NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
    Name             nvarchar(256) NULL,
    NormalizedName   nvarchar(256) NULL,
    ConcurrencyStamp nvarchar(max) NULL
);
CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;

CREATE TABLE dbo.AspNetUserRoles (
    UserId uniqueidentifier NOT NULL CONSTRAINT FK_AspNetUserRoles_User REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    RoleId uniqueidentifier NOT NULL CONSTRAINT FK_AspNetUserRoles_Role REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId)
);

-- The next four are standard Identity tables. They stay empty, but Identity expects them.
CREATE TABLE dbo.AspNetUserClaims (
    Id         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
    UserId     uniqueidentifier NOT NULL CONSTRAINT FK_AspNetUserClaims_User REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    ClaimType  nvarchar(max) NULL,
    ClaimValue nvarchar(max) NULL
);
CREATE TABLE dbo.AspNetRoleClaims (
    Id         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
    RoleId     uniqueidentifier NOT NULL CONSTRAINT FK_AspNetRoleClaims_Role REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE,
    ClaimType  nvarchar(max) NULL,
    ClaimValue nvarchar(max) NULL
);
CREATE TABLE dbo.AspNetUserLogins (
    LoginProvider       nvarchar(128) NOT NULL,
    ProviderKey         nvarchar(128) NOT NULL,
    ProviderDisplayName nvarchar(max) NULL,
    UserId              uniqueidentifier NOT NULL CONSTRAINT FK_AspNetUserLogins_User REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey)
);
CREATE TABLE dbo.AspNetUserTokens (
    UserId        uniqueidentifier NOT NULL CONSTRAINT FK_AspNetUserTokens_User REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    LoginProvider nvarchar(128) NOT NULL,
    Name          nvarchar(128) NOT NULL,
    Value         nvarchar(max) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name)
);

/* =====================================================================
   2. CATALOGUE (categories, subcategories, skills)
   ===================================================================== */
CREATE TABLE dbo.Categories (
    CategoryId  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Slug        varchar(60)   NOT NULL CONSTRAINT UQ_Categories_Slug UNIQUE,   -- used in page links
    Name        nvarchar(60)  NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
    Icon        nvarchar(12)  NOT NULL CONSTRAINT DF_Categories_Icon DEFAULT (N'📁'),
    Description nvarchar(240) NULL,
    IsDefault   bit           NOT NULL CONSTRAINT DF_Categories_IsDefault DEFAULT (0)  -- default categories cannot be deleted
);

CREATE TABLE dbo.SubCategories (
    SubCategoryId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SubCategories PRIMARY KEY,
    CategoryId    int           NOT NULL CONSTRAINT FK_SubCategories_Category REFERENCES dbo.Categories (CategoryId),
    Slug          varchar(60)   NOT NULL,
    Name          nvarchar(60)  NOT NULL,
    Description   nvarchar(240) NULL,
    CONSTRAINT UQ_SubCategories_Slug UNIQUE (CategoryId, Slug),
    CONSTRAINT UQ_SubCategories_Name UNIQUE (CategoryId, Name)
);

CREATE TABLE dbo.Skills (
    SkillId       int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Skills PRIMARY KEY,
    SubCategoryId int            NOT NULL CONSTRAINT FK_Skills_SubCategory REFERENCES dbo.SubCategories (SubCategoryId),
    Slug          varchar(80)    NOT NULL CONSTRAINT UQ_Skills_Slug UNIQUE,     -- used in page links (?skill=react)
    Name          nvarchar(120)  NOT NULL,
    Description   nvarchar(2000) NOT NULL,
    Icon          nvarchar(12)   NULL,
    Level         varchar(20)    NOT NULL,   -- difficulty
    AccessTier    varchar(20)    NOT NULL,   -- Basic = Free, Advanced = needs Plus
    Status        varchar(20)    NOT NULL CONSTRAINT DF_Skills_Status DEFAULT ('Active'),
    CreatedAt     datetime2(0)   NOT NULL CONSTRAINT DF_Skills_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_Skills_Level      CHECK (Level IN ('Beginner', 'Intermediate', 'Advanced')),
    CONSTRAINT CK_Skills_AccessTier CHECK (AccessTier IN ('Basic', 'Advanced')),
    CONSTRAINT CK_Skills_Status     CHECK (Status IN ('Active', 'Archived'))
);

/* =====================================================================
   3. MEMBER SKILLS (can teach / wants to learn)
   ===================================================================== */
CREATE TABLE dbo.UserSkills (
    UserId     uniqueidentifier NOT NULL CONSTRAINT FK_UserSkills_User  REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    SkillId    int              NOT NULL CONSTRAINT FK_UserSkills_Skill REFERENCES dbo.Skills (SkillId),
    SkillType  varchar(10)      NOT NULL,       -- Teach or Learn
    Level      varchar(20)      NOT NULL CONSTRAINT DF_UserSkills_Level DEFAULT ('Beginner'),
    Experience nvarchar(50)     NULL,           -- free text, e.g. "2 years"
    CONSTRAINT PK_UserSkills PRIMARY KEY (UserId, SkillId, SkillType),
    CONSTRAINT CK_UserSkills_SkillType CHECK (SkillType IN ('Teach', 'Learn')),
    CONSTRAINT CK_UserSkills_Level     CHECK (Level IN ('Beginner', 'Intermediate', 'Advanced'))
);
CREATE INDEX IX_UserSkills_Skill ON dbo.UserSkills (SkillId, SkillType);

/* =====================================================================
   4. EXPERTS (approval is per skill; an approved application = expert badge)
   ===================================================================== */
CREATE TABLE dbo.ExpertApplications (
    ApplicationId    int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExpertApplications PRIMARY KEY,
    UserId           uniqueidentifier NOT NULL CONSTRAINT FK_ExpertApplications_User  REFERENCES dbo.AspNetUsers (Id),
    SkillId          int              NOT NULL CONSTRAINT FK_ExpertApplications_Skill REFERENCES dbo.Skills (SkillId),
    Route            varchar(20)      NOT NULL,       -- External or Community
    Evidence         nvarchar(3000)   NOT NULL,
    PortfolioUrl     nvarchar(500)    NULL,
    Status           varchar(20)      NOT NULL CONSTRAINT DF_ExpertApplications_Status DEFAULT ('UnderReview'),
    AdminNote        nvarchar(1500)   NULL,           -- "Reason and feedback" from the admin decision form
    ReviewedByUserId uniqueidentifier NULL CONSTRAINT FK_ExpertApplications_Reviewer REFERENCES dbo.AspNetUsers (Id),
    SubmittedAt      datetime2(0)     NOT NULL CONSTRAINT DF_ExpertApplications_SubmittedAt DEFAULT (SYSUTCDATETIME()),
    ReviewedAt       datetime2(0)     NULL,
    CONSTRAINT CK_ExpertApplications_Route  CHECK (Route IN ('External', 'Community')),
    CONSTRAINT CK_ExpertApplications_Status CHECK (Status IN ('UnderReview', 'NeedsInfo', 'Approved', 'Rejected')),
    CONSTRAINT CK_ExpertApplications_Decision CHECK (Status = 'UnderReview' OR (AdminNote IS NOT NULL AND ReviewedByUserId IS NOT NULL AND ReviewedAt IS NOT NULL))
);
-- One application per member per skill (resubmitting after "Needs information" updates the same row)
CREATE UNIQUE INDEX UX_ExpertApplications_UserSkill ON dbo.ExpertApplications (UserId, SkillId);

CREATE TABLE dbo.ExpertAvailability (
    AvailabilityId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExpertAvailability PRIMARY KEY,
    UserId         uniqueidentifier NOT NULL CONSTRAINT FK_ExpertAvailability_User REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    Days           varchar(10)  NOT NULL,     -- Weekday or Weekend
    TimeZone       varchar(64)  NOT NULL,     -- e.g. Asia/Amman
    StartTime      time(0)      NOT NULL,
    EndTime        time(0)      NOT NULL,
    CONSTRAINT CK_ExpertAvailability_Days  CHECK (Days IN ('Weekday', 'Weekend')),
    CONSTRAINT CK_ExpertAvailability_Times CHECK (EndTime > StartTime)
);

/* =====================================================================
   5. PLANS, SUBSCRIPTIONS, PAYMENTS, EXPERT CREDITS
   ===================================================================== */
CREATE TABLE dbo.Plans (
    PlanId               int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Plans PRIMARY KEY,
    Code                 varchar(10)   NOT NULL CONSTRAINT UQ_Plans_Code UNIQUE,   -- Free or Plus
    Name                 nvarchar(40)  NOT NULL,
    MonthlyPrice         decimal(10,2) NOT NULL,
    YearlyPrice          decimal(10,2) NOT NULL,
    MonthlyExpertCredits tinyint       NOT NULL,
    ExpertSessionMinutes smallint      NOT NULL,
    LastChangeReason     nvarchar(1000) NULL,     -- "Reason for the change" on the admin plans form
    UpdatedAt            datetime2(0)  NOT NULL CONSTRAINT DF_Plans_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_Plans_Code   CHECK (Code IN ('Free', 'Plus')),
    CONSTRAINT CK_Plans_Prices CHECK (MonthlyPrice >= 0 AND YearlyPrice >= MonthlyPrice),
    CONSTRAINT CK_Plans_Free   CHECK (Code <> 'Free' OR (MonthlyPrice = 0 AND YearlyPrice = 0 AND MonthlyExpertCredits = 0))
);

-- A member with no active subscription is on Free.
CREATE TABLE dbo.Subscriptions (
    SubscriptionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Subscriptions PRIMARY KEY,
    UserId         uniqueidentifier NOT NULL CONSTRAINT FK_Subscriptions_User REFERENCES dbo.AspNetUsers (Id),
    PlanId         int              NOT NULL CONSTRAINT FK_Subscriptions_Plan REFERENCES dbo.Plans (PlanId),
    BillingPeriod  varchar(10)      NOT NULL,        -- Monthly or Yearly
    Price          decimal(10,2)    NOT NULL,        -- price at purchase time (later price changes do not affect it)
    Status         varchar(20)      NOT NULL CONSTRAINT DF_Subscriptions_Status DEFAULT ('Pending'),
    StartDate      datetime2(0)     NULL,
    PaidThrough    datetime2(0)     NULL,            -- also the "Next billing" date
    AutoRenew      bit              NOT NULL CONSTRAINT DF_Subscriptions_AutoRenew DEFAULT (1),
    CancelReason   nvarchar(500)    NULL,            -- optional reason from "Cancel renewal"
    CreatedAt      datetime2(0)     NOT NULL CONSTRAINT DF_Subscriptions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_Subscriptions_BillingPeriod CHECK (BillingPeriod IN ('Monthly', 'Yearly')),
    CONSTRAINT CK_Subscriptions_Status        CHECK (Status IN ('Pending', 'Active', 'Expired')),
    CONSTRAINT CK_Subscriptions_Dates         CHECK (Status = 'Pending' OR (StartDate IS NOT NULL AND PaidThrough > StartDate))
);
-- At most one current (Pending or Active) subscription per member
CREATE UNIQUE INDEX UX_Subscriptions_Current ON dbo.Subscriptions (UserId) WHERE Status IN ('Pending', 'Active');

CREATE TABLE dbo.Payments (
    PaymentId      int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    SubscriptionId int            NOT NULL CONSTRAINT FK_Payments_Subscription REFERENCES dbo.Subscriptions (SubscriptionId),
    Reference      nvarchar(100)  NOT NULL CONSTRAINT UQ_Payments_Reference UNIQUE,  -- payment provider reference
    Amount         decimal(10,2)  NOT NULL,       -- USD
    Status         varchar(20)    NOT NULL CONSTRAINT DF_Payments_Status DEFAULT ('Pending'),
    CreatedAt      datetime2(0)   NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    PaidAt         datetime2(0)   NULL,
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Payments_Status CHECK (Status IN ('Pending', 'Paid', 'Failed')),
    CONSTRAINT CK_Payments_PaidAt CHECK (Status <> 'Paid' OR PaidAt IS NOT NULL)
);

/* =====================================================================
   6. REQUESTS, SESSIONS, CHAT, REVIEWS
   ===================================================================== */
-- Exchange / Volunteer: sent from exchange-request page, accepted or declined by the receiver.
-- Expert: sent from expert-booking page (Message = learning goal), needs Plus and one credit.
CREATE TABLE dbo.Requests (
    RequestId        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Requests PRIMARY KEY,
    SenderUserId     uniqueidentifier NOT NULL CONSTRAINT FK_Requests_Sender   REFERENCES dbo.AspNetUsers (Id),
    ReceiverUserId   uniqueidentifier NOT NULL CONSTRAINT FK_Requests_Receiver REFERENCES dbo.AspNetUsers (Id),
    RequestType      varchar(20)      NOT NULL,
    RequestedSkillId int              NOT NULL CONSTRAINT FK_Requests_RequestedSkill REFERENCES dbo.Skills (SkillId),
    OfferedSkillId   int              NULL     CONSTRAINT FK_Requests_OfferedSkill   REFERENCES dbo.Skills (SkillId),
    Message          nvarchar(2000)   NOT NULL,
    PreferredTime    varchar(20)      NULL,
    Status           varchar(20)      NOT NULL CONSTRAINT DF_Requests_Status DEFAULT ('Pending'),
    CreatedAt        datetime2(0)     NOT NULL CONSTRAINT DF_Requests_CreatedAt DEFAULT (SYSUTCDATETIME()),
    RespondedAt      datetime2(0)     NULL,
    CONSTRAINT CK_Requests_NotSelf       CHECK (SenderUserId <> ReceiverUserId),
    CONSTRAINT CK_Requests_Type          CHECK (RequestType IN ('Exchange', 'Volunteer', 'Expert')),
    CONSTRAINT CK_Requests_OfferedSkill  CHECK ((RequestType = 'Exchange' AND OfferedSkillId IS NOT NULL) OR (RequestType <> 'Exchange' AND OfferedSkillId IS NULL)),
    CONSTRAINT CK_Requests_PreferredTime CHECK (PreferredTime IS NULL OR PreferredTime IN ('WeekdaysEvening', 'Weekends', 'Flexible')),
    CONSTRAINT CK_Requests_Status        CHECK (Status IN ('Pending', 'Accepted', 'Rejected', 'Cancelled', 'Completed'))
);
CREATE INDEX IX_Requests_Sender   ON dbo.Requests (SenderUserId, Status);
CREATE INDEX IX_Requests_Receiver ON dbo.Requests (ReceiverUserId, Status);

CREATE TABLE dbo.Sessions (
    SessionId       int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sessions PRIMARY KEY,
    RequestId       int              NOT NULL CONSTRAINT FK_Sessions_Request REFERENCES dbo.Requests (RequestId),
    SkillId         int              NOT NULL CONSTRAINT FK_Sessions_Skill   REFERENCES dbo.Skills (SkillId),
    StartsAt        datetimeoffset(0) NOT NULL,     -- keeps the member's time zone offset, e.g. +03:00 Amman
    DurationMinutes smallint         NOT NULL CONSTRAINT DF_Sessions_Duration DEFAULT (30),
    MeetingUrl      nvarchar(500)    NULL,          -- Zoom link
    Status          varchar(20)      NOT NULL CONSTRAINT DF_Sessions_Status DEFAULT ('Scheduled'),
    CreatedAt       datetime2(0)     NOT NULL CONSTRAINT DF_Sessions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_Sessions_Duration CHECK (DurationMinutes > 0),
    CONSTRAINT CK_Sessions_Status   CHECK (Status IN ('Scheduled', 'Completed', 'Cancelled'))
);
CREATE INDEX IX_Sessions_Request ON dbo.Sessions (RequestId);

-- session-details page: propose another time / cancel / report absence
CREATE TABLE dbo.SessionChangeRequests (
    ChangeRequestId   int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SessionChangeRequests PRIMARY KEY,
    SessionId         int              NOT NULL CONSTRAINT FK_SessionChangeRequests_Session REFERENCES dbo.Sessions (SessionId),
    RequestedByUserId uniqueidentifier NOT NULL CONSTRAINT FK_SessionChangeRequests_User    REFERENCES dbo.AspNetUsers (Id),
    ChangeType        varchar(20)      NOT NULL,
    NewStartsAt       datetimeoffset(0) NULL,
    Reason            nvarchar(2000)   NOT NULL,
    Status            varchar(20)      NOT NULL CONSTRAINT DF_SessionChangeRequests_Status DEFAULT ('Pending'),
    CreatedAt         datetime2(0)     NOT NULL CONSTRAINT DF_SessionChangeRequests_CreatedAt DEFAULT (SYSUTCDATETIME()),
    RespondedAt       datetime2(0)     NULL,
    CONSTRAINT CK_SessionChangeRequests_Type   CHECK (ChangeType IN ('Reschedule', 'Cancel', 'Absence')),
    CONSTRAINT CK_SessionChangeRequests_Date   CHECK ((ChangeType = 'Reschedule' AND NewStartsAt IS NOT NULL) OR (ChangeType <> 'Reschedule' AND NewStartsAt IS NULL)),
    CONSTRAINT CK_SessionChangeRequests_Status CHECK (Status IN ('Pending', 'Accepted', 'Rejected'))
);

-- Expert credit ledger. Balance = SUM(Credits) per user.
-- Grant = monthly Plus allowance (+1), Use = expert booking (-1), Return = early cancellation (+1, once per session).
CREATE TABLE dbo.CreditTransactions (
    CreditTransactionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CreditTransactions PRIMARY KEY,
    UserId              uniqueidentifier NOT NULL CONSTRAINT FK_CreditTransactions_User         REFERENCES dbo.AspNetUsers (Id),
    SubscriptionId      int              NOT NULL CONSTRAINT FK_CreditTransactions_Subscription REFERENCES dbo.Subscriptions (SubscriptionId),
    SessionId           int              NULL     CONSTRAINT FK_CreditTransactions_Session      REFERENCES dbo.Sessions (SessionId),
    TransactionType     varchar(10)      NOT NULL,
    Credits             smallint         NOT NULL,
    CreatedAt           datetime2(0)     NOT NULL CONSTRAINT DF_CreditTransactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_CreditTransactions_Type    CHECK (TransactionType IN ('Grant', 'Use', 'Return')),
    CONSTRAINT CK_CreditTransactions_Credits CHECK ((TransactionType = 'Use' AND Credits < 0) OR (TransactionType <> 'Use' AND Credits > 0)),
    CONSTRAINT CK_CreditTransactions_Session CHECK ((TransactionType = 'Grant' AND SessionId IS NULL) OR (TransactionType <> 'Grant' AND SessionId IS NOT NULL))
);
-- A session can use a credit once and return it once
CREATE UNIQUE INDEX UX_CreditTransactions_Session ON dbo.CreditTransactions (SessionId, TransactionType) WHERE SessionId IS NOT NULL;
CREATE INDEX IX_CreditTransactions_User ON dbo.CreditTransactions (UserId);

-- Chat between the two members of an accepted request (the request is the conversation).
CREATE TABLE dbo.Messages (
    MessageId    int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Messages PRIMARY KEY,
    RequestId    int              NOT NULL CONSTRAINT FK_Messages_Request REFERENCES dbo.Requests (RequestId),
    SenderUserId uniqueidentifier NOT NULL CONSTRAINT FK_Messages_Sender  REFERENCES dbo.AspNetUsers (Id),
    Body         nvarchar(4000)   NOT NULL,
    SentAt       datetime2(0)     NOT NULL CONSTRAINT DF_Messages_SentAt DEFAULT (SYSUTCDATETIME()),
    IsRead       bit              NOT NULL CONSTRAINT DF_Messages_IsRead DEFAULT (0),
    CONSTRAINT CK_Messages_Body CHECK (LEN(LTRIM(RTRIM(Body))) > 0)
);
CREATE INDEX IX_Messages_Request ON dbo.Messages (RequestId, SentAt);

-- One review per participant per completed request
CREATE TABLE dbo.Reviews (
    ReviewId       int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY,
    RequestId      int              NOT NULL CONSTRAINT FK_Reviews_Request  REFERENCES dbo.Requests (RequestId),
    ReviewerUserId uniqueidentifier NOT NULL CONSTRAINT FK_Reviews_Reviewer REFERENCES dbo.AspNetUsers (Id),
    ReviewedUserId uniqueidentifier NOT NULL CONSTRAINT FK_Reviews_Reviewed REFERENCES dbo.AspNetUsers (Id),
    Rating         tinyint          NOT NULL,
    Comment        nvarchar(1000)   NOT NULL,
    IsReported     bit              NOT NULL CONSTRAINT DF_Reviews_IsReported DEFAULT (0),   -- admin "Reported" filter / Approve
    CreatedAt      datetime2(0)     NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_Reviews_RequestReviewer UNIQUE (RequestId, ReviewerUserId),
    CONSTRAINT CK_Reviews_Rating  CHECK (Rating BETWEEN 1 AND 5),
    CONSTRAINT CK_Reviews_NotSelf CHECK (ReviewerUserId <> ReviewedUserId)
);
CREATE INDEX IX_Reviews_Reviewed ON dbo.Reviews (ReviewedUserId);

/* =====================================================================
   7. CAREER PATH
   ===================================================================== */
CREATE TABLE dbo.CareerGoals (
    GoalId          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CareerGoals PRIMARY KEY,
    Slug            varchar(40)   NOT NULL CONSTRAINT UQ_CareerGoals_Slug UNIQUE,
    Name            nvarchar(100) NOT NULL,
    Description     nvarchar(500) NULL,
    EstimatedMonths tinyint       NOT NULL
);

-- Ordered skills of each goal (the timeline milestones)
CREATE TABLE dbo.CareerGoalSteps (
    GoalId    int     NOT NULL CONSTRAINT FK_CareerGoalSteps_Goal  REFERENCES dbo.CareerGoals (GoalId) ON DELETE CASCADE,
    StepOrder tinyint NOT NULL,
    SkillId   int     NOT NULL CONSTRAINT FK_CareerGoalSteps_Skill REFERENCES dbo.Skills (SkillId),
    CONSTRAINT PK_CareerGoalSteps PRIMARY KEY (GoalId, StepOrder),
    CONSTRAINT UQ_CareerGoalSteps_Skill UNIQUE (GoalId, SkillId),
    CONSTRAINT CK_CareerGoalSteps_Order CHECK (StepOrder > 0)
);

-- One current path per member. "Change Goal" replaces it and resets progress.
CREATE TABLE dbo.CareerPaths (
    CareerPathId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CareerPaths PRIMARY KEY,
    UserId       uniqueidentifier NOT NULL CONSTRAINT FK_CareerPaths_User REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    GoalId       int              NOT NULL CONSTRAINT FK_CareerPaths_Goal REFERENCES dbo.CareerGoals (GoalId),
    Level        varchar(20)      NOT NULL,       -- beginner / intermediate / advanced
    WeeklyTime   varchar(20)      NOT NULL,       -- light / moderate / intense
    CreatedAt    datetime2(0)     NOT NULL CONSTRAINT DF_CareerPaths_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_CareerPaths_User UNIQUE (UserId),
    CONSTRAINT CK_CareerPaths_Level      CHECK (Level IN ('Beginner', 'Intermediate', 'Advanced')),
    CONSTRAINT CK_CareerPaths_WeeklyTime CHECK (WeeklyTime IN ('Light', 'Moderate', 'Intense'))
);

-- Completed steps. Steps are completed in order; skills the member already knows
-- (chosen in setup) are saved here as completed when the path is created.
CREATE TABLE dbo.CareerPathProgress (
    CareerPathId int          NOT NULL CONSTRAINT FK_CareerPathProgress_Path REFERENCES dbo.CareerPaths (CareerPathId) ON DELETE CASCADE,
    StepOrder    tinyint      NOT NULL,
    CompletedAt  datetime2(0) NOT NULL CONSTRAINT DF_CareerPathProgress_CompletedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_CareerPathProgress PRIMARY KEY (CareerPathId, StepOrder)
);
GO

/* =====================================================================
   8. REFERENCE DATA
   ===================================================================== */
INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', N'Member', N'MEMBER', N'18520f39-cc22-4439-9c82-12c9dcbf9010'),
('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', N'Admin',  N'ADMIN',  N'bc108c83-ddc4-43b7-a938-26f6dacd8dd1');

SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories (CategoryId, Slug, Name, Icon, Description, IsDefault) VALUES
(1, 'education',      N'Education',        N'🎓', N'Learn academic and teaching skills', 1),
(2, 'technology',     N'Technology',       N'💻', N'Programming, design, and IT', 1),
(3, 'handmade',       N'Handmade',         N'🎨', N'Crafts, arts, and DIY projects', 1),
(4, 'entertainment',  N'Entertainment',    N'🎬', N'Music, video, and performance', 1),
(5, 'business',       N'Business',         N'💼', N'Marketing, management, and sales', 1),
(6, 'health-fitness', N'Health & Fitness', N'❤️', N'Sports, nutrition, and wellness', 1);
SET IDENTITY_INSERT dbo.Categories OFF;

SET IDENTITY_INSERT dbo.SubCategories ON;
INSERT INTO dbo.SubCategories (SubCategoryId, CategoryId, Slug, Name) VALUES
(1, 1, 'mathematics',        N'Mathematics'),
(2, 1, 'physics',            N'Physics'),
(3, 1, 'chemistry',          N'Chemistry'),
(4, 1, 'english',            N'English'),
(5, 1, 'arabic',             N'Arabic Language'),
(6, 2, 'web-development',    N'Web Development'),
(7, 2, 'ui-ux',              N'UI/UX Design'),
(8, 2, 'mobile-development', N'Mobile Development'),
(9, 2, 'cybersecurity',      N'Cybersecurity'),
(10, 2, 'programming',       N'Programming'),
(11, 3, 'painting',          N'Painting'),
(12, 3, 'knitting',          N'Knitting'),
(13, 3, 'crafts',            N'Crafts'),
(14, 3, 'diy',               N'DIY Projects'),
(15, 3, 'arts',              N'Arts'),
(16, 4, 'music',             N'Music'),
(17, 4, 'video-editing',     N'Video Editing'),
(18, 4, 'photography',       N'Photography'),
(19, 5, 'marketing',         N'Marketing'),
(20, 5, 'management',        N'Management'),
(21, 5, 'sales',             N'Sales'),
(22, 5, 'entrepreneurship',  N'Entrepreneurship'),
(23, 6, 'yoga',              N'Yoga'),
(24, 6, 'nutrition',         N'Nutrition'),
(25, 6, 'personal-training', N'Personal Training');
SET IDENTITY_INSERT dbo.SubCategories OFF;

SET IDENTITY_INSERT dbo.Skills ON;
INSERT INTO dbo.Skills (SkillId, SubCategoryId, Slug, Name, Description, Level, AccessTier) VALUES
(1,  6,  'html',                N'HTML Fundamentals',       N'Structure web pages with semantic HTML.', 'Beginner', 'Basic'),
(2,  6,  'css',                 N'CSS & Responsive Design', N'Style pages and make them work on every screen size.', 'Beginner', 'Basic'),
(3,  6,  'javascript',          N'JavaScript',              N'Add interactivity to web pages with JavaScript.', 'Intermediate', 'Basic'),
(4,  6,  'git-github',          N'Git & GitHub',            N'Track changes and collaborate with Git and GitHub.', 'Beginner', 'Basic'),
(5,  6,  'web-development',     N'Web Development',         N'Build complete websites from the ground up.', 'Beginner', 'Basic'),
(6,  6,  'react',               N'React',                   N'Build modern user interfaces with React.', 'Advanced', 'Advanced'),
(7,  6,  'backend-development', N'Backend Development',     N'Build server-side logic and APIs.', 'Advanced', 'Advanced'),
(8,  6,  'database-apis',       N'Database & APIs',         N'Design databases and connect them through APIs.', 'Advanced', 'Advanced'),
(9,  6,  'full-stack-project',  N'Full Stack Project',      N'Combine front end and back end in one project.', 'Advanced', 'Advanced'),
(10, 7,  'ui-ux-design',        N'UI/UX Design',            N'Design user interfaces and experiences.', 'Beginner', 'Basic'),
(11, 7,  'figma',               N'Figma Basics',            N'Create designs and prototypes in Figma.', 'Beginner', 'Basic'),
(12, 7,  'user-research',       N'User Research',           N'Understand users through interviews and testing.', 'Intermediate', 'Advanced'),
(13, 15, 'graphic-design',      N'Graphic Design',          N'Create visual designs for print and screen.', 'Beginner', 'Basic'),
(14, 8,  'react-native',        N'React Native',            N'Build mobile apps with React Native.', 'Advanced', 'Advanced'),
(15, 10, 'python',              N'Python',                  N'Start programming with Python.', 'Beginner', 'Basic'),
(16, 10, 'advanced-python',     N'Advanced Python',         N'Go deeper into Python for real projects.', 'Advanced', 'Advanced'),
(17, 10, 'machine-learning',    N'Machine Learning',        N'Train models that learn from data.', 'Advanced', 'Advanced'),
(18, 10, 'devops',              N'DevOps',                  N'Automate building, testing, and deploying software.', 'Advanced', 'Advanced'),
(19, 18, 'photography',         N'Photography',             N'Take better photos with any camera.', 'Beginner', 'Basic');
SET IDENTITY_INSERT dbo.Skills OFF;

SET IDENTITY_INSERT dbo.Plans ON;
INSERT INTO dbo.Plans (PlanId, Code, Name, MonthlyPrice, YearlyPrice, MonthlyExpertCredits, ExpertSessionMinutes) VALUES
(1, 'Free', N'Free', 0, 0, 0, 0),
(2, 'Plus', N'Plus', 9, 84, 1, 30);
SET IDENTITY_INSERT dbo.Plans OFF;

SET IDENTITY_INSERT dbo.CareerGoals ON;
INSERT INTO dbo.CareerGoals (GoalId, Slug, Name, EstimatedMonths) VALUES
(1, 'full-stack', N'Full Stack Web Developer', 6),
(2, 'ui-ux',      N'UI/UX Designer',           4),
(3, 'mobile',     N'Mobile App Developer',     5),
(4, 'data',       N'Data Scientist',           8),
(5, 'devops',     N'DevOps Engineer',          7),
(6, 'graphic',    N'Graphic Designer',         3);
SET IDENTITY_INSERT dbo.CareerGoals OFF;

INSERT INTO dbo.CareerGoalSteps (GoalId, StepOrder, SkillId) VALUES
(1, 1, 1), (1, 2, 2), (1, 3, 3), (1, 4, 4), (1, 5, 6), (1, 6, 7), (1, 7, 8), (1, 8, 9),  -- full-stack
(2, 1, 10), (2, 2, 13), (2, 3, 12),                                                      -- ui-ux
(3, 1, 5), (3, 2, 3), (3, 3, 14),                                                        -- mobile
(4, 1, 15), (4, 2, 16), (4, 3, 17),                                                      -- data
(5, 1, 15), (5, 2, 16), (5, 3, 18),                                                      -- devops
(6, 1, 13), (6, 2, 10), (6, 3, 12);                                                      -- graphic

/* =====================================================================
   9. DEMO DATA (fictional; password for all: SkillsSwap_Demo!2026)
   ===================================================================== */
INSERT INTO dbo.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, FullName, Location, Bio) VALUES
('11111111-1111-4111-8111-111111111111', N'sara.free@example.test', N'SARA.FREE@EXAMPLE.TEST', N'sara.free@example.test', N'SARA.FREE@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAEOJuuyVvtPeSa16bZnHQLItNeoOtmSWncwnl03TNSJojzpkUfESXe98NDeuwG+B0RA==', N'27ca60e2-b321-4aba-866f-c5e68e32df12', N'ca747ffa-fe6d-4504-8321-fcf8bcfbc3f8', N'Sara Khalil', N'Amman, Jordan', N'Graphic designer who wants to learn web development.'),
('22222222-2222-4222-8222-222222222222', N'omar.plus@example.test', N'OMAR.PLUS@EXAMPLE.TEST', N'omar.plus@example.test', N'OMAR.PLUS@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAEER++vlZFo6l3eiQRzeXaKd0rhHPCpMt8sgiefEEXZNFkEwyxnTHD7c4+4V7GPNOMw==', N'2af856e8-d3b6-4cf9-ad0e-fabf63ba0787', N'9f051ff9-db4d-4a9f-9c6a-8276c4311397', N'Omar Saleh', N'Irbid, Jordan', N'Plus member learning React.'),
('33333333-3333-4333-8333-333333333333', N'lana.expert@example.test', N'LANA.EXPERT@EXAMPLE.TEST', N'lana.expert@example.test', N'LANA.EXPERT@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAENwEvdtzSnWOnLqKzgHKeyfglXs8EzJF7l9yEgQUbQAGoiCr8jL823XDbgWWSdiOmg==', N'afff42df-5c67-480b-8257-615a2511604d', N'2d3af9e2-d866-4405-8fd1-a34bfeaccd64', N'Lana Ali', N'Amman, Jordan', N'Front-end developer and approved React expert.'),
('44444444-4444-4444-8444-444444444444', N'admin@example.test', N'ADMIN@EXAMPLE.TEST', N'admin@example.test', N'ADMIN@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAEBx1GvCTY4x877NIgiExyYp6u8HO8bWHbJgNsvuvY3QoVOXeyFc5ontiWeRE4K+g4w==', N'90834337-1939-43c2-9aa0-71c2976da30d', N'7a70297a-ac9e-4b59-b4d8-1e4e735eca54', N'Site Admin', N'Jordan', NULL),
('55555555-5555-4555-8555-555555555555', N'ahmad.partner@example.test', N'AHMAD.PARTNER@EXAMPLE.TEST', N'ahmad.partner@example.test', N'AHMAD.PARTNER@EXAMPLE.TEST', 1, N'AQAAAAIAAYagAAAAEFgLWrnfqMPySlaoICiyv9H30CoqPp7CyWbdlXgctwpGaljV23iOKl/WOKhjvww/UQ==', N'5f3e5fc2-490d-4ef9-b610-d20fe6a0b98c', N'30aa305d-4fa5-44ce-97cc-553ee09f999d', N'Ahmad Hassan', N'Amman, Jordan', N'Web development mentor.');

INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES
('11111111-1111-4111-8111-111111111111', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'),
('22222222-2222-4222-8222-222222222222', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'),
('33333333-3333-4333-8333-333333333333', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'),
('44444444-4444-4444-8444-444444444444', 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'),
('55555555-5555-4555-8555-555555555555', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa');

INSERT INTO dbo.UserSkills (UserId, SkillId, SkillType, Level, Experience) VALUES
('11111111-1111-4111-8111-111111111111', 13, 'Teach', 'Intermediate', N'3 years'),
('11111111-1111-4111-8111-111111111111', 5,  'Learn', 'Beginner', NULL),
('22222222-2222-4222-8222-222222222222', 19, 'Teach', 'Intermediate', N'2 years'),
('22222222-2222-4222-8222-222222222222', 6,  'Learn', 'Beginner', NULL),
('33333333-3333-4333-8333-333333333333', 6,  'Teach', 'Advanced', N'6 years'),
('33333333-3333-4333-8333-333333333333', 3,  'Teach', 'Advanced', N'6 years'),
('33333333-3333-4333-8333-333333333333', 12, 'Learn', 'Beginner', NULL),
('55555555-5555-4555-8555-555555555555', 5,  'Teach', 'Advanced', N'5+ years'),
('55555555-5555-4555-8555-555555555555', 3,  'Teach', 'Intermediate', N'4 years'),
('55555555-5555-4555-8555-555555555555', 13, 'Learn', 'Beginner', NULL);

SET IDENTITY_INSERT dbo.ExpertApplications ON;
INSERT INTO dbo.ExpertApplications (ApplicationId, UserId, SkillId, Route, Evidence, PortfolioUrl, Status, AdminNote, ReviewedByUserId, SubmittedAt, ReviewedAt) VALUES
(1, '33333333-3333-4333-8333-333333333333', 6, 'External', N'Six years building React apps, portfolio and a recorded sample lesson for the practical teaching assessment.', N'https://example.test/lana', 'Approved', N'Portfolio and practical assessment verified for React.', '44444444-4444-4444-8444-444444444444', '2026-08-01', '2026-08-03'),
(2, '55555555-5555-4555-8555-555555555555', 3, 'External', N'Portfolio of JavaScript projects and a sample lesson plan for beginners, waiting for the practical teaching assessment.', NULL, 'UnderReview', NULL, NULL, '2026-11-01', NULL);
SET IDENTITY_INSERT dbo.ExpertApplications OFF;

INSERT INTO dbo.ExpertAvailability (UserId, Days, TimeZone, StartTime, EndTime) VALUES
('33333333-3333-4333-8333-333333333333', 'Weekday', 'Asia/Amman', '17:00', '20:00'),
('33333333-3333-4333-8333-333333333333', 'Weekend', 'Asia/Amman', '10:00', '14:00');

SET IDENTITY_INSERT dbo.Subscriptions ON;
INSERT INTO dbo.Subscriptions (SubscriptionId, UserId, PlanId, BillingPeriod, Price, Status, StartDate, PaidThrough, AutoRenew) VALUES
(1, '22222222-2222-4222-8222-222222222222', 2, 'Monthly', 9, 'Active', '2026-11-01', '2026-12-01', 1);
SET IDENTITY_INSERT dbo.Subscriptions OFF;

INSERT INTO dbo.Payments (SubscriptionId, Reference, Amount, Status, CreatedAt, PaidAt) VALUES
(1, N'SAMPLE-001', 9, 'Paid', '2026-11-01', '2026-11-01');

SET IDENTITY_INSERT dbo.Requests ON;
INSERT INTO dbo.Requests (RequestId, SenderUserId, ReceiverUserId, RequestType, RequestedSkillId, OfferedSkillId, Message, PreferredTime, Status, CreatedAt, RespondedAt) VALUES
-- Sara offers Graphic Design to Ahmad in exchange for Web Development (accepted, chat open)
(1, '11111111-1111-4111-8111-111111111111', '55555555-5555-4555-8555-555555555555', 'Exchange', 5, 13, N'Hi Ahmad, I would love to learn web development and can teach you graphic design in return.', 'WeekdaysEvening', 'Accepted', '2026-10-20', '2026-10-21'),
-- Omar books Lana as a React expert (Plus, uses one credit)
(2, '22222222-2222-4222-8222-222222222222', '33333333-3333-4333-8333-333333333333', 'Expert', 6, NULL, N'I want help structuring components and state in my first React project.', NULL, 'Accepted', '2026-11-02', '2026-11-02'),
-- Omar asked Ahmad for volunteer JavaScript help (completed and reviewed)
(3, '22222222-2222-4222-8222-222222222222', '55555555-5555-4555-8555-555555555555', 'Volunteer', 3, NULL, N'Could you help me understand JavaScript basics before I start React?', 'Weekends', 'Completed', '2026-09-10', '2026-09-11'),
-- Pending request waiting for Lana
(4, '11111111-1111-4111-8111-111111111111', '33333333-3333-4333-8333-333333333333', 'Exchange', 3, 13, N'Hi Lana, I can teach graphic design if you can help me with JavaScript.', 'Flexible', 'Pending', '2026-11-05', NULL);
SET IDENTITY_INSERT dbo.Requests OFF;

SET IDENTITY_INSERT dbo.Sessions ON;
INSERT INTO dbo.Sessions (SessionId, RequestId, SkillId, StartsAt, DurationMinutes, MeetingUrl, Status) VALUES
(1, 1, 5, '2026-11-10 17:00:00 +03:00', 45, N'https://zoom.us/j/0000000001', 'Scheduled'),
(2, 2, 6, '2026-11-11 18:00:00 +03:00', 30, N'https://zoom.us/j/0000000002', 'Scheduled'),
(3, 3, 3, '2026-09-13 11:00:00 +03:00', 45, NULL, 'Completed');
SET IDENTITY_INSERT dbo.Sessions OFF;

INSERT INTO dbo.CreditTransactions (UserId, SubscriptionId, SessionId, TransactionType, Credits, CreatedAt) VALUES
('22222222-2222-4222-8222-222222222222', 1, NULL, 'Grant', 1, '2026-11-01'),
('22222222-2222-4222-8222-222222222222', 1, 2, 'Use', -1, '2026-11-02');

INSERT INTO dbo.Messages (RequestId, SenderUserId, Body, SentAt, IsRead) VALUES
(1, '55555555-5555-4555-8555-555555555555', N'Hi Sara! Happy to start. Does Monday at 5 PM work?', '2026-10-21 14:00', 1),
(1, '11111111-1111-4111-8111-111111111111', N'Monday works, see you then.', '2026-10-21 14:10', 1),
(2, '33333333-3333-4333-8333-333333333333', N'Hi Omar, please bring your project repo to the session.', '2026-11-02 19:00', 0);

INSERT INTO dbo.Reviews (RequestId, ReviewerUserId, ReviewedUserId, Rating, Comment, CreatedAt) VALUES
(3, '22222222-2222-4222-8222-222222222222', '55555555-5555-4555-8555-555555555555', 5, N'Very clear explanations and patient with questions.', '2026-09-14'),
(3, '55555555-5555-4555-8555-555555555555', '22222222-2222-4222-8222-222222222222', 4, N'Came prepared and asked good questions.', '2026-09-14');

SET IDENTITY_INSERT dbo.CareerPaths ON;
INSERT INTO dbo.CareerPaths (CareerPathId, UserId, GoalId, Level, WeeklyTime, CreatedAt) VALUES
(1, '11111111-1111-4111-8111-111111111111', 1, 'Beginner', 'Moderate', '2026-09-01');
SET IDENTITY_INSERT dbo.CareerPaths OFF;

INSERT INTO dbo.CareerPathProgress (CareerPathId, StepOrder, CompletedAt) VALUES
(1, 1, '2026-09-15'),
(1, 2, '2026-10-05');
GO

COMMIT TRANSACTION;
PRINT N'SkillsSwap created successfully.';
GO
SET NOEXEC OFF;
GO
