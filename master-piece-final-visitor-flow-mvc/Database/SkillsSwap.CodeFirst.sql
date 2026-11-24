IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(120) NOT NULL,
        [Location] nvarchar(120) NULL,
        [Bio] nvarchar(1000) NULL,
        [ProfileImageUrl] nvarchar(500) NULL,
        [IsProfileComplete] bit NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [NameAr] nvarchar(80) NOT NULL,
        [NameEn] nvarchar(80) NOT NULL,
        [Slug] nvarchar(80) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [Plans] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [MonthlyPrice] decimal(10,2) NOT NULL,
        [YearlyPrice] decimal(10,2) NOT NULL,
        [MonthlyExpertCredits] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Plans] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [CareerPaths] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [Goal] nvarchar(200) NOT NULL,
        [CurrentLevel] nvarchar(30) NOT NULL,
        [WeeklyHours] int NOT NULL,
        [GeneratedByAi] bit NOT NULL,
        [Status] int NOT NULL,
        [AdminReviewNote] nvarchar(1500) NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ReviewedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_CareerPaths] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CareerPaths_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [SubCategories] (
        [Id] int NOT NULL IDENTITY,
        [CategoryId] int NOT NULL,
        [NameAr] nvarchar(80) NOT NULL,
        [NameEn] nvarchar(80) NOT NULL,
        [Slug] nvarchar(80) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_SubCategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [Subscriptions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [PlanId] int NOT NULL,
        [Status] int NOT NULL,
        [StartsAtUtc] datetime2 NOT NULL,
        [EndsAtUtc] datetime2 NOT NULL,
        [AutoRenew] bit NOT NULL,
        CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Subscriptions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Subscriptions_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [Skills] (
        [Id] int NOT NULL IDENTITY,
        [SubCategoryId] int NOT NULL,
        [NameAr] nvarchar(120) NOT NULL,
        [NameEn] nvarchar(120) NOT NULL,
        [Slug] nvarchar(120) NOT NULL,
        [Description] nvarchar(1500) NOT NULL,
        [Level] int NOT NULL,
        [AccessTier] int NOT NULL,
        [Status] int NOT NULL,
        CONSTRAINT [PK_Skills] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Skills_SubCategories_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [SubCategories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] bigint NOT NULL IDENTITY,
        [SubscriptionId] int NOT NULL,
        [Provider] nvarchar(100) NOT NULL,
        [ProviderReference] nvarchar(200) NOT NULL,
        [Amount] decimal(10,2) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Subscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [Subscriptions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [CareerPathSteps] (
        [Id] bigint NOT NULL IDENTITY,
        [CareerPathId] bigint NOT NULL,
        [Order] int NOT NULL,
        [SkillId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [LearningOutcome] nvarchar(2000) NOT NULL,
        [RequiredTier] int NOT NULL,
        [ProgressStatus] int NOT NULL,
        [CompletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_CareerPathSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CareerPathStep_Order] CHECK ([Order] > 0),
        CONSTRAINT [FK_CareerPathSteps_CareerPaths_CareerPathId] FOREIGN KEY ([CareerPathId]) REFERENCES [CareerPaths] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CareerPathSteps_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [ExpertApplications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [SkillId] int NOT NULL,
        [Experience] nvarchar(3000) NOT NULL,
        [TeachingApproach] nvarchar(2000) NOT NULL,
        [PortfolioUrl] nvarchar(500) NULL,
        [SampleUrl] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [AdminReason] nvarchar(1500) NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [SubmittedAtUtc] datetime2 NOT NULL,
        [ReviewedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ExpertApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExpertApplications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExpertApplications_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [LearningRequests] (
        [Id] bigint NOT NULL IDENTITY,
        [LearnerId] uniqueidentifier NOT NULL,
        [TeacherId] uniqueidentifier NOT NULL,
        [SkillId] int NOT NULL,
        [IsExpertBooking] bit NOT NULL,
        [Message] nvarchar(1500) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [RespondedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_LearningRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LearningRequest_NotSelf] CHECK ([LearnerId] <> [TeacherId]),
        CONSTRAINT [FK_LearningRequests_AspNetUsers_LearnerId] FOREIGN KEY ([LearnerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LearningRequests_AspNetUsers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LearningRequests_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [UserSkills] (
        [UserId] uniqueidentifier NOT NULL,
        [SkillId] int NOT NULL,
        [Intent] int NOT NULL,
        [Level] int NOT NULL,
        [Experience] nvarchar(1000) NULL,
        [AddedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UserSkills] PRIMARY KEY ([UserId], [SkillId], [Intent]),
        CONSTRAINT [FK_UserSkills_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserSkills_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [ExpertAvailabilities] (
        [Id] int NOT NULL IDENTITY,
        [ExpertApplicationId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartsAt] time NOT NULL,
        [EndsAt] time NOT NULL,
        [TimeZoneId] nvarchar(60) NOT NULL,
        CONSTRAINT [PK_ExpertAvailabilities] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ExpertAvailability_Time] CHECK ([StartsAt] < [EndsAt]),
        CONSTRAINT [FK_ExpertAvailabilities_ExpertApplications_ExpertApplicationId] FOREIGN KEY ([ExpertApplicationId]) REFERENCES [ExpertApplications] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [LearningSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [LearningRequestId] bigint NOT NULL,
        [StartsAt] datetimeoffset NOT NULL,
        [DurationMinutes] int NOT NULL,
        [MeetingJoinUrl] nvarchar(500) NULL,
        [MeetingProviderId] nvarchar(200) NULL,
        [Status] int NOT NULL,
        CONSTRAINT [PK_LearningSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LearningSessions_LearningRequests_LearningRequestId] FOREIGN KEY ([LearningRequestId]) REFERENCES [LearningRequests] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [Messages] (
        [Id] bigint NOT NULL IDENTITY,
        [LearningRequestId] bigint NOT NULL,
        [SenderId] uniqueidentifier NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [SentAtUtc] datetime2 NOT NULL,
        [ReadAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Messages_AspNetUsers_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Messages_LearningRequests_LearningRequestId] FOREIGN KEY ([LearningRequestId]) REFERENCES [LearningRequests] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] bigint NOT NULL IDENTITY,
        [LearningSessionId] bigint NOT NULL,
        [ReviewerId] uniqueidentifier NOT NULL,
        [ReviewedUserId] uniqueidentifier NOT NULL,
        [Rating] tinyint NOT NULL,
        [Comment] nvarchar(1500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Review_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
        CONSTRAINT [FK_Reviews_LearningSessions_LearningSessionId] FOREIGN KEY ([LearningSessionId]) REFERENCES [LearningSessions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'MonthlyExpertCredits', N'MonthlyPrice', N'Name', N'YearlyPrice') AND [object_id] = OBJECT_ID(N'[Plans]'))
        SET IDENTITY_INSERT [Plans] ON;
    EXEC(N'INSERT INTO [Plans] ([Id], [Code], [IsActive], [MonthlyExpertCredits], [MonthlyPrice], [Name], [YearlyPrice])
    VALUES (1, N''FREE'', CAST(1 AS bit), 0, 0.0, N''Free'', 0.0),
    (2, N''PLUS'', CAST(1 AS bit), 1, 9.0, N''Plus'', 84.0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'MonthlyExpertCredits', N'MonthlyPrice', N'Name', N'YearlyPrice') AND [object_id] = OBJECT_ID(N'[Plans]'))
        SET IDENTITY_INSERT [Plans] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CareerPaths_UserId_Status] ON [CareerPaths] ([UserId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CareerPathSteps_CareerPathId_Order] ON [CareerPathSteps] ([CareerPathId], [Order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CareerPathSteps_SkillId] ON [CareerPathSteps] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ExpertApplications_SkillId] ON [ExpertApplications] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExpertApplications_UserId_SkillId] ON [ExpertApplications] ([UserId], [SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ExpertAvailabilities_ExpertApplicationId] ON [ExpertAvailabilities] ([ExpertApplicationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LearningRequests_LearnerId] ON [LearningRequests] ([LearnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LearningRequests_SkillId] ON [LearningRequests] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LearningRequests_TeacherId] ON [LearningRequests] ([TeacherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LearningSessions_LearningRequestId] ON [LearningSessions] ([LearningRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Messages_LearningRequestId_SentAtUtc] ON [Messages] ([LearningRequestId], [SentAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_Provider_ProviderReference] ON [Payments] ([Provider], [ProviderReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_SubscriptionId] ON [Payments] ([SubscriptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Plans_Code] ON [Plans] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Reviews_LearningSessionId_ReviewerId] ON [Reviews] ([LearningSessionId], [ReviewerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Skills_Slug] ON [Skills] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Skills_SubCategoryId] ON [Skills] ([SubCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SubCategories_CategoryId_Slug] ON [SubCategories] ([CategoryId], [Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_PlanId] ON [Subscriptions] ([PlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_UserId] ON [Subscriptions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserSkills_SkillId] ON [UserSkills] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123165832_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123165832_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123171633_SeedCatalogueAndAuth'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsActive', N'NameAr', N'NameEn', N'Slug') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([Id], [Description], [IsActive], [NameAr], [NameEn], [Slug])
    VALUES (1, NULL, CAST(1 AS bit), N''التكنولوجيا'', N''Technology'', N''technology''),
    (2, NULL, CAST(1 AS bit), N''التصميم والإبداع'', N''Design & Creative'', N''design-creative''),
    (3, NULL, CAST(1 AS bit), N''الأعمال'', N''Business'', N''business''),
    (4, NULL, CAST(1 AS bit), N''اللغات والتعليم'', N''Languages & Education'', N''languages-education''),
    (5, NULL, CAST(1 AS bit), N''الصحة والعافية'', N''Health & Wellness'', N''health-wellness''),
    (6, NULL, CAST(1 AS bit), N''الإعلام والمهارات الحياتية'', N''Media & Lifestyle'', N''media-lifestyle'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsActive', N'NameAr', N'NameEn', N'Slug') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123171633_SeedCatalogueAndAuth'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'IsActive', N'NameAr', N'NameEn', N'Slug') AND [object_id] = OBJECT_ID(N'[SubCategories]'))
        SET IDENTITY_INSERT [SubCategories] ON;
    EXEC(N'INSERT INTO [SubCategories] ([Id], [CategoryId], [IsActive], [NameAr], [NameEn], [Slug])
    VALUES (1, 1, CAST(1 AS bit), N''تطوير الويب'', N''Web Development'', N''web-development''),
    (2, 1, CAST(1 AS bit), N''البرمجة والبيانات'', N''Programming & Data'', N''programming-data''),
    (3, 1, CAST(1 AS bit), N''الموبايل والسحابة والأمن'', N''Mobile, Cloud & Security'', N''mobile-cloud-security''),
    (4, 2, CAST(1 AS bit), N''تصميم تجربة المستخدم'', N''UI/UX Design'', N''ui-ux''),
    (5, 2, CAST(1 AS bit), N''الفنون البصرية'', N''Visual Arts'', N''visual-arts''),
    (6, 3, CAST(1 AS bit), N''التسويق والمبيعات'', N''Marketing & Sales'', N''marketing-sales''),
    (7, 3, CAST(1 AS bit), N''الإدارة والمالية'', N''Management & Finance'', N''management-finance''),
    (8, 4, CAST(1 AS bit), N''اللغات'', N''Languages'', N''languages''),
    (9, 4, CAST(1 AS bit), N''المهارات الأكاديمية'', N''Academic Skills'', N''academic-skills''),
    (10, 5, CAST(1 AS bit), N''اللياقة البدنية'', N''Fitness'', N''fitness''),
    (11, 5, CAST(1 AS bit), N''العافية'', N''Wellness'', N''wellness''),
    (12, 6, CAST(1 AS bit), N''التصوير والفيديو'', N''Photo & Video'', N''photo-video''),
    (13, 6, CAST(1 AS bit), N''الموسيقى'', N''Music'', N''music''),
    (14, 6, CAST(1 AS bit), N''الأعمال اليدوية'', N''Handmade'', N''handmade''),
    (15, 6, CAST(1 AS bit), N''التواصل'', N''Communication'', N''communication'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'IsActive', N'NameAr', N'NameEn', N'Slug') AND [object_id] = OBJECT_ID(N'[SubCategories]'))
        SET IDENTITY_INSERT [SubCategories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123171633_SeedCatalogueAndAuth'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessTier', N'Description', N'Level', N'NameAr', N'NameEn', N'Slug', N'Status', N'SubCategoryId') AND [object_id] = OBJECT_ID(N'[Skills]'))
        SET IDENTITY_INSERT [Skills] ON;
    EXEC(N'INSERT INTO [Skills] ([Id], [AccessTier], [Description], [Level], [NameAr], [NameEn], [Slug], [Status], [SubCategoryId])
    VALUES (1, 0, N''Learn HTML & CSS through community guidance and structured practice.'', 0, N''HTML وCSS'', N''HTML & CSS'', N''html-css'', 1, 1),
    (2, 0, N''Learn JavaScript through community guidance and structured practice.'', 0, N''جافاسكربت'', N''JavaScript'', N''javascript'', 1, 1),
    (3, 0, N''Learn Responsive Web Design through community guidance and structured practice.'', 1, N''تصميم الويب المتجاوب'', N''Responsive Web Design'', N''responsive-web-design'', 1, 1),
    (4, 1, N''Learn React through community guidance and structured practice.'', 2, N''رياكت'', N''React'', N''react'', 1, 1),
    (5, 1, N''Learn ASP.NET Core through community guidance and structured practice.'', 2, N''ASP.NET Core'', N''ASP.NET Core'', N''aspnet-core'', 1, 1),
    (6, 0, N''Learn Python through community guidance and structured practice.'', 0, N''بايثون'', N''Python'', N''python'', 1, 2),
    (7, 0, N''Learn C# through community guidance and structured practice.'', 1, N''سي شارب'', N''C#'', N''csharp'', 1, 2),
    (8, 0, N''Learn SQL & Databases through community guidance and structured practice.'', 1, N''SQL وقواعد البيانات'', N''SQL & Databases'', N''sql-databases'', 1, 2),
    (9, 1, N''Learn Data Analysis through community guidance and structured practice.'', 1, N''تحليل البيانات'', N''Data Analysis'', N''data-analysis'', 1, 2),
    (10, 1, N''Learn Machine Learning through community guidance and structured practice.'', 2, N''تعلم الآلة'', N''Machine Learning'', N''machine-learning'', 1, 2),
    (11, 1, N''Learn Flutter through community guidance and structured practice.'', 2, N''فلاتر'', N''Flutter'', N''flutter'', 1, 3),
    (12, 0, N''Learn Git & GitHub through community guidance and structured practice.'', 0, N''Git وGitHub'', N''Git & GitHub'', N''git-github'', 1, 3),
    (13, 0, N''Learn Cybersecurity Basics through community guidance and structured practice.'', 1, N''أساسيات الأمن السيبراني'', N''Cybersecurity Basics'', N''cybersecurity-basics'', 1, 3),
    (14, 1, N''Learn Cloud & DevOps through community guidance and structured practice.'', 2, N''الحوسبة السحابية وDevOps'', N''Cloud & DevOps'', N''cloud-devops'', 1, 3),
    (15, 0, N''Learn Figma through community guidance and structured practice.'', 0, N''فيجما'', N''Figma'', N''figma'', 1, 4),
    (16, 0, N''Learn UI/UX Design through community guidance and structured practice.'', 1, N''تصميم UI/UX'', N''UI/UX Design'', N''ui-ux-design'', 1, 4),
    (17, 1, N''Learn User Research through community guidance and structured practice.'', 2, N''بحث المستخدم'', N''User Research'', N''user-research'', 1, 4),
    (18, 0, N''Learn Graphic Design through community guidance and structured practice.'', 0, N''التصميم الجرافيكي'', N''Graphic Design'', N''graphic-design'', 1, 5),
    (19, 1, N''Learn Digital Illustration through community guidance and structured practice.'', 1, N''الرسم الرقمي'', N''Digital Illustration'', N''digital-illustration'', 1, 5),
    (20, 0, N''Learn Digital Marketing through community guidance and structured practice.'', 0, N''التسويق الرقمي'', N''Digital Marketing'', N''digital-marketing'', 1, 6),
    (21, 0, N''Learn Social Media Marketing through community guidance and structured practice.'', 1, N''التسويق عبر التواصل الاجتماعي'', N''Social Media Marketing'', N''social-media-marketing'', 1, 6),
    (22, 1, N''Learn SEO through community guidance and structured practice.'', 2, N''تحسين محركات البحث'', N''SEO'', N''seo'', 1, 6),
    (23, 0, N''Learn Project Management through community guidance and structured practice.'', 1, N''إدارة المشاريع'', N''Project Management'', N''project-management'', 1, 7),
    (24, 0, N''Learn Entrepreneurship through community guidance and structured practice.'', 0, N''ريادة الأعمال'', N''Entrepreneurship'', N''entrepreneurship'', 1, 7),
    (25, 1, N''Learn Excel for Finance through community guidance and structured practice.'', 1, N''Excel للمالية'', N''Excel for Finance'', N''excel-finance'', 1, 7),
    (26, 0, N''Learn English Conversation through community guidance and structured practice.'', 0, N''المحادثة الإنجليزية'', N''English Conversation'', N''english-conversation'', 1, 8),
    (27, 0, N''Learn Arabic for Non-Native Speakers through community guidance and structured practice.'', 0, N''العربية لغير الناطقين بها'', N''Arabic for Non-Native Speakers'', N''arabic-for-non-native'', 1, 8),
    (28, 1, N''Learn IELTS Preparation through community guidance and structured practice.'', 2, N''التحضير لآيلتس'', N''IELTS Preparation'', N''ielts-preparation'', 1, 8),
    (29, 0, N''Learn Mathematics through community guidance and structured practice.'', 0, N''الرياضيات'', N''Mathematics'', N''mathematics'', 1, 9),
    (30, 0, N''Learn Study Skills through community guidance and structured practice.'', 0, N''مهارات الدراسة'', N''Study Skills'', N''study-skills'', 1, 9),
    (31, 0, N''Learn Yoga through community guidance and structured practice.'', 0, N''اليوغا'', N''Yoga'', N''yoga'', 1, 10),
    (32, 1, N''Learn Personal Training through community guidance and structured practice.'', 2, N''التدريب الشخصي'', N''Personal Training'', N''personal-training'', 1, 10),
    (33, 0, N''Learn Nutrition Basics through community guidance and structured practice.'', 0, N''أساسيات التغذية'', N''Nutrition Basics'', N''nutrition-basics'', 1, 11),
    (34, 0, N''Learn Meditation through community guidance and structured practice.'', 0, N''التأمل'', N''Meditation'', N''meditation'', 1, 11),
    (35, 0, N''Learn Photography through community guidance and structured practice.'', 0, N''التصوير الفوتوغرافي'', N''Photography'', N''photography'', 1, 12),
    (36, 1, N''Learn Video Editing through community guidance and structured practice.'', 1, N''تحرير الفيديو'', N''Video Editing'', N''video-editing'', 1, 12),
    (37, 0, N''Learn Guitar through community guidance and structured practice.'', 0, N''الغيتار'', N''Guitar'', N''guitar'', 1, 13),
    (38, 1, N''Learn Music Production through community guidance and structured practice.'', 2, N''الإنتاج الموسيقي'', N''Music Production'', N''music-production'', 1, 13),
    (39, 0, N''Learn Crochet through community guidance and structured practice.'', 0, N''الكروشيه'', N''Crochet'', N''crochet'', 1, 14),
    (40, 0, N''Learn DIY Crafts through community guidance and structured practice.'', 0, N''الأعمال اليدوية المنزلية'', N''DIY Crafts'', N''diy-crafts'', 1, 14),
    (41, 0, N''Learn Public Speaking through community guidance and structured practice.'', 1, N''التحدث أمام الجمهور'', N''Public Speaking'', N''public-speaking'', 1, 15),
    (42, 1, N''Learn Presentation Skills through community guidance and structured practice.'', 1, N''مهارات العرض'', N''Presentation Skills'', N''presentation-skills'', 1, 15)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessTier', N'Description', N'Level', N'NameAr', N'NameEn', N'Slug', N'Status', N'SubCategoryId') AND [object_id] = OBJECT_ID(N'[Skills]'))
        SET IDENTITY_INSERT [Skills] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123171633_SeedCatalogueAndAuth'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123171633_SeedCatalogueAndAuth', N'10.0.14');
END;

COMMIT;
GO

