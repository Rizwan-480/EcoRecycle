-- EcoRecycle Database Schema Setup
-- Target: SQL Server / LocalDB

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EcoRecycleDb')
BEGIN
    CREATE DATABASE EcoRecycleDb;
END
GO

USE EcoRecycleDb;
GO

-- Drop existing tables in reverse dependency order for a clean install
IF OBJECT_ID('dbo.Settings', 'U') IS NOT NULL DROP TABLE dbo.Settings;
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID('dbo.Leaderboard', 'U') IS NOT NULL DROP TABLE dbo.Leaderboard;
IF OBJECT_ID('dbo.Badges', 'U') IS NOT NULL DROP TABLE dbo.Badges;
IF OBJECT_ID('dbo.EcoTips', 'U') IS NOT NULL DROP TABLE dbo.EcoTips;
IF OBJECT_ID('dbo.News', 'U') IS NOT NULL DROP TABLE dbo.News;
IF OBJECT_ID('dbo.Notifications', 'U') IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID('dbo.CampaignParticipants', 'U') IS NOT NULL DROP TABLE dbo.CampaignParticipants;
IF OBJECT_ID('dbo.Campaigns', 'U') IS NOT NULL DROP TABLE dbo.Campaigns;
IF OBJECT_ID('dbo.RewardTransactions', 'U') IS NOT NULL DROP TABLE dbo.RewardTransactions;
IF OBJECT_ID('dbo.Rewards', 'U') IS NOT NULL DROP TABLE dbo.Rewards;
IF OBJECT_ID('dbo.PickupItems', 'U') IS NOT NULL DROP TABLE dbo.PickupItems;
IF OBJECT_ID('dbo.PickupRequests', 'U') IS NOT NULL DROP TABLE dbo.PickupRequests;
IF OBJECT_ID('dbo.WasteCategories', 'U') IS NOT NULL DROP TABLE dbo.WasteCategories;
IF OBJECT_ID('dbo.Stores', 'U') IS NOT NULL DROP TABLE dbo.Stores;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

-- Roles
CREATE TABLE Roles (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

-- Users
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255) NULL,
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    RoleID INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleID),
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsBlocked BIT DEFAULT 0,
    RewardPoints INT DEFAULT 0,
    AvatarUrl NVARCHAR(255) NULL
);

-- Stores
CREATE TABLE Stores (
    StoreID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Users(UserID),
    StoreName NVARCHAR(100) NOT NULL,
    StoreAddress NVARCHAR(255) NOT NULL,
    Latitude DECIMAL(9,6) NOT NULL,
    Longitude DECIMAL(9,6) NOT NULL,
    IsApproved BIT DEFAULT 0,
    OperatingHours NVARCHAR(100) NULL,
    ContactNumber NVARCHAR(20) NULL
);

-- Waste Categories
CREATE TABLE WasteCategories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    PointsPerKg INT NOT NULL DEFAULT 10,
    IconUrl NVARCHAR(255) NULL,
    IsRecyclable BIT DEFAULT 1
);

-- Pickup Requests
CREATE TABLE PickupRequests (
    PickupID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL FOREIGN KEY REFERENCES Users(UserID),
    StoreID INT NULL FOREIGN KEY REFERENCES Stores(StoreID),
    Address NVARCHAR(255) NOT NULL,
    ScheduledDate DATETIME NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Accepted, Rejected, Scheduled, Completed
    TotalWeight DECIMAL(10,2) NULL,
    TotalPoints INT NULL,
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    Notes NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);

-- Pickup Request Items
CREATE TABLE PickupItems (
    ItemID INT PRIMARY KEY IDENTITY(1,1),
    PickupID INT NOT NULL FOREIGN KEY REFERENCES PickupRequests(PickupID) ON DELETE CASCADE,
    CategoryID INT NOT NULL FOREIGN KEY REFERENCES WasteCategories(CategoryID),
    EstimatedWeight DECIMAL(10,2) NULL,
    ActualWeight DECIMAL(10,2) NULL
);

-- Rewards
CREATE TABLE Rewards (
    RewardID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    PointsCost INT NOT NULL,
    StockCount INT NOT NULL DEFAULT 0,
    IsActive BIT DEFAULT 1,
    ImageUrl NVARCHAR(255) NULL
);

-- Reward Transactions
CREATE TABLE RewardTransactions (
    TransactionID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL FOREIGN KEY REFERENCES Users(UserID),
    RewardID INT NOT NULL FOREIGN KEY REFERENCES Rewards(RewardID),
    RedeemDate DATETIME DEFAULT GETDATE(),
    PointsSpent INT NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Redeemed
    VerificationCode NVARCHAR(100) NOT NULL UNIQUE
);

-- Campaigns
CREATE TABLE Campaigns (
    CampaignID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(1000) NULL,
    TargetGoal DECIMAL(10,2) NOT NULL, -- in kg
    CurrentProgress DECIMAL(10,2) NOT NULL DEFAULT 0,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Campaign Participants
CREATE TABLE CampaignParticipants (
    ParticipantID INT PRIMARY KEY IDENTITY(1,1),
    CampaignID INT NOT NULL FOREIGN KEY REFERENCES Campaigns(CampaignID) ON DELETE CASCADE,
    UserID INT NOT NULL FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    JoinedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_Campaign_User UNIQUE (CampaignID, UserID)
);

-- Notifications
CREATE TABLE Notifications (
    NotificationID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    Message NVARCHAR(500) NOT NULL,
    Type NVARCHAR(50) NOT NULL, -- PickupAccepted, PickupRejected, PickupScheduled, PickupCompleted, RewardEarned, CampaignStarted, System
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- News
CREATE TABLE News (
    NewsID INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    ImageUrl NVARCHAR(255) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Eco Tips
CREATE TABLE EcoTips (
    TipID INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(200) NOT NULL,
    TipContent NVARCHAR(1000) NOT NULL,
    Category NVARCHAR(50) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Badges
CREATE TABLE Badges (
    BadgeID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    IconUrl NVARCHAR(255) NULL,
    ThresholdPoints INT NOT NULL
);

-- Leaderboard
CREATE TABLE Leaderboard (
    LeaderboardID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    TotalPoints INT NOT NULL DEFAULT 0,
    Rank INT NULL,
    LastUpdated DATETIME DEFAULT GETDATE()
);

-- Audit Logs
CREATE TABLE AuditLogs (
    LogID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NULL FOREIGN KEY REFERENCES Users(UserID) ON DELETE SET NULL,
    Action NVARCHAR(100) NOT NULL,
    TableName NVARCHAR(50) NULL,
    RecordID INT NULL,
    Details NVARCHAR(1000) NULL,
    Timestamp DATETIME DEFAULT GETDATE()
);

-- Settings
CREATE TABLE Settings (
    SettingKey NVARCHAR(50) PRIMARY KEY,
    SettingValue NVARCHAR(255) NOT NULL,
    Description NVARCHAR(255) NULL
);
GO

-- Create Indexes for performance optimization
CREATE INDEX IX_Users_RoleID ON Users(RoleID);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Stores_UserID ON Stores(UserID);
CREATE INDEX IX_PickupRequests_UserID ON PickupRequests(UserID);
CREATE INDEX IX_PickupRequests_StoreID ON PickupRequests(StoreID);
CREATE INDEX IX_PickupRequests_Status ON PickupRequests(Status);
CREATE INDEX IX_RewardTransactions_UserID ON RewardTransactions(UserID);
CREATE INDEX IX_RewardTransactions_VerificationCode ON RewardTransactions(VerificationCode);
CREATE INDEX IX_Notifications_UserID_IsRead ON Notifications(UserID, IsRead);
GO
