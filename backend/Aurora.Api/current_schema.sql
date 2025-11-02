CREATE TABLE "Users" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
    "Email" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "IsEmailVerified" INTEGER NOT NULL,
    "LastLoginAt" TEXT NULL,
    "Timezone" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "IsActive" INTEGER NOT NULL DEFAULT 1
);


CREATE TABLE "EventCategories" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_EventCategories" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Color" TEXT NOT NULL,
    "Icon" TEXT NULL,
    "IsSystemDefault" INTEGER NOT NULL DEFAULT 0,
    "SortOrder" INTEGER NOT NULL,
    "UserId" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "IsActive" INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT "FK_EventCategories_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "UserPreferences" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_UserPreferences" PRIMARY KEY,
    "TimeZone" TEXT NOT NULL,
    "DateFormat" TEXT NOT NULL DEFAULT 'DD/MM/YYYY',
    "TimeFormat" TEXT NOT NULL DEFAULT '24h',
    "FirstDayOfWeek" INTEGER NOT NULL DEFAULT 1,
    "Language" TEXT NOT NULL DEFAULT 'es',
    "Theme" TEXT NOT NULL DEFAULT 'light',
    "EmailNotifications" INTEGER NOT NULL,
    "DefaultReminderMinutes" INTEGER NOT NULL DEFAULT 15,
    "DefaultCalendarView" TEXT NOT NULL,
    "WorkStartTime" TEXT NULL,
    "WorkEndTime" TEXT NULL,
    "WorkDaysOfWeek" TEXT NULL,
    "ExerciseDaysOfWeek" TEXT NULL,
    "NlpKeywords" TEXT NULL,
    "NotificationsEnabled" INTEGER NOT NULL,
    "UserId" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "IsActive" INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT "FK_UserPreferences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "UserSessions" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_UserSessions" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "TokenId" TEXT NOT NULL,
    "ExpiresAtUtc" TEXT NOT NULL,
    "RevokedAtUtc" TEXT NULL,
    "RevokedReason" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_UserSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Events" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Events" PRIMARY KEY,
    "Title" TEXT NOT NULL,
    "Description" TEXT NULL,
    "StartDate" datetime NOT NULL,
    "EndDate" datetime NOT NULL,
    "EventCategoryId" TEXT NOT NULL,
    "IsAllDay" INTEGER NOT NULL DEFAULT 0,
    "Location" TEXT NULL,
    "Color" TEXT NULL,
    "Notes" TEXT NULL,
    "Priority" INTEGER NOT NULL DEFAULT 2,
    "IsRecurring" INTEGER NOT NULL,
    "RecurrencePattern" TEXT NULL,
    "MoodRating" INTEGER NULL,
    "MoodNotes" TEXT NULL,
    "UserId" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "IsActive" INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT "FK_Events_EventCategories_EventCategoryId" FOREIGN KEY ("EventCategoryId") REFERENCES "EventCategories" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Events_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE INDEX "IX_EventCategories_IsDefault" ON "EventCategories" ("IsSystemDefault");


CREATE INDEX "IX_EventCategories_UserId" ON "EventCategories" ("UserId");


CREATE UNIQUE INDEX "IX_EventCategories_UserId_Name" ON "EventCategories" ("UserId", "Name");


CREATE INDEX "IX_Events_CategoryId" ON "Events" ("EventCategoryId");


CREATE INDEX "IX_Events_StartDate" ON "Events" ("StartDate");


CREATE INDEX "IX_Events_UserId" ON "Events" ("UserId");


CREATE INDEX "IX_Events_UserId_StartDate" ON "Events" ("UserId", "StartDate");


CREATE UNIQUE INDEX "IX_UserPreferences_UserId" ON "UserPreferences" ("UserId");


CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");


CREATE UNIQUE INDEX "IX_UserSessions_TokenId" ON "UserSessions" ("TokenId");


CREATE INDEX "IX_UserSessions_UserId" ON "UserSessions" ("UserId");


