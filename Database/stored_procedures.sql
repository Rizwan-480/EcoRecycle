-- EcoRecycle Stored Procedures Script
-- Target: SQL Server / LocalDB

USE EcoRecycleDb;
GO

-- Drop existing procedures to allow clean recreate
IF OBJECT_ID('sp_RegisterUser', 'P') IS NOT NULL DROP PROCEDURE sp_RegisterUser;
IF OBJECT_ID('sp_CreateStore', 'P') IS NOT NULL DROP PROCEDURE sp_CreateStore;
IF OBJECT_ID('sp_GetUserByEmail', 'P') IS NOT NULL DROP PROCEDURE sp_GetUserByEmail;
IF OBJECT_ID('sp_GetUserById', 'P') IS NOT NULL DROP PROCEDURE sp_GetUserById;
IF OBJECT_ID('sp_UpdateUserProfile', 'P') IS NOT NULL DROP PROCEDURE sp_UpdateUserProfile;
IF OBJECT_ID('sp_GetApprovedStores', 'P') IS NOT NULL DROP PROCEDURE sp_GetApprovedStores;
IF OBJECT_ID('sp_GetPendingStores', 'P') IS NOT NULL DROP PROCEDURE sp_GetPendingStores;
IF OBJECT_ID('sp_ApproveStore', 'P') IS NOT NULL DROP PROCEDURE sp_ApproveStore;
IF OBJECT_ID('sp_BlockUser', 'P') IS NOT NULL DROP PROCEDURE sp_BlockUser;
IF OBJECT_ID('sp_CreatePickupRequest', 'P') IS NOT NULL DROP PROCEDURE sp_CreatePickupRequest;
IF OBJECT_ID('sp_AddPickupItem', 'P') IS NOT NULL DROP PROCEDURE sp_AddPickupItem;
IF OBJECT_ID('sp_GetPickupRequestsByUser', 'P') IS NOT NULL DROP PROCEDURE sp_GetPickupRequestsByUser;
IF OBJECT_ID('sp_GetPickupRequestsByStore', 'P') IS NOT NULL DROP PROCEDURE sp_GetPickupRequestsByStore;
IF OBJECT_ID('sp_GetPendingPickupRequests', 'P') IS NOT NULL DROP PROCEDURE sp_GetPendingPickupRequests;
IF OBJECT_ID('sp_UpdatePickupStatus', 'P') IS NOT NULL DROP PROCEDURE sp_UpdatePickupStatus;
IF OBJECT_ID('sp_CompletePickup', 'P') IS NOT NULL DROP PROCEDURE sp_CompletePickup;
IF OBJECT_ID('sp_GetPickupItems', 'P') IS NOT NULL DROP PROCEDURE sp_GetPickupItems;
IF OBJECT_ID('sp_UpdatePickupItemActualWeight', 'P') IS NOT NULL DROP PROCEDURE sp_UpdatePickupItemActualWeight;
IF OBJECT_ID('sp_GetLeaderboard', 'P') IS NOT NULL DROP PROCEDURE sp_GetLeaderboard;
IF OBJECT_ID('sp_GetRewards', 'P') IS NOT NULL DROP PROCEDURE sp_GetRewards;
IF OBJECT_ID('sp_RedeemReward', 'P') IS NOT NULL DROP PROCEDURE sp_RedeemReward;
IF OBJECT_ID('sp_VerifyQRCode', 'P') IS NOT NULL DROP PROCEDURE sp_VerifyQRCode;
IF OBJECT_ID('sp_GetCampaigns', 'P') IS NOT NULL DROP PROCEDURE sp_GetCampaigns;
IF OBJECT_ID('sp_JoinCampaign', 'P') IS NOT NULL DROP PROCEDURE sp_JoinCampaign;
IF OBJECT_ID('sp_GetNotifications', 'P') IS NOT NULL DROP PROCEDURE sp_GetNotifications;
IF OBJECT_ID('sp_MarkNotificationsRead', 'P') IS NOT NULL DROP PROCEDURE sp_MarkNotificationsRead;
IF OBJECT_ID('sp_GetAdminDashboardStats', 'P') IS NOT NULL DROP PROCEDURE sp_GetAdminDashboardStats;
IF OBJECT_ID('sp_GetStoreDashboardStats', 'P') IS NOT NULL DROP PROCEDURE sp_GetStoreDashboardStats;
IF OBJECT_ID('sp_AddAuditLog', 'P') IS NOT NULL DROP PROCEDURE sp_AddAuditLog;
IF OBJECT_ID('sp_GetSystemSettings', 'P') IS NOT NULL DROP PROCEDURE sp_GetSystemSettings;
IF OBJECT_ID('sp_UpdateSystemSetting', 'P') IS NOT NULL DROP PROCEDURE sp_UpdateSystemSetting;
GO

-- User registration
CREATE PROCEDURE sp_RegisterUser
    @Username NVARCHAR(50),
    @Email NVARCHAR(100),
    @PasswordHash NVARCHAR(255),
    @FullName NVARCHAR(100),
    @Address NVARCHAR(255) = NULL,
    @Latitude DECIMAL(9,6) = NULL,
    @Longitude DECIMAL(9,6) = NULL,
    @RoleID INT,
    @NewUserID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Users (Username, Email, PasswordHash, FullName, Address, Latitude, Longitude, RoleID, CreatedAt, IsBlocked, RewardPoints)
    VALUES (@Username, @Email, @PasswordHash, @FullName, @Address, @Latitude, @Longitude, @RoleID, GETDATE(), 0, 0);
    
    SET @NewUserID = SCOPE_IDENTITY();
    
    -- If it's a standard user (RoleName = 'User'), create a leaderboard entry
    IF EXISTS (SELECT 1 FROM Roles WHERE RoleID = @RoleID AND RoleName = 'User')
    BEGIN
        INSERT INTO Leaderboard (UserID, TotalPoints, Rank, LastUpdated)
        VALUES (@NewUserID, 0, NULL, GETDATE());
    END
END
GO

-- Create Store specific profile details
CREATE PROCEDURE sp_CreateStore
    @UserID INT,
    @StoreName NVARCHAR(100),
    @StoreAddress NVARCHAR(255),
    @Latitude DECIMAL(9,6),
    @Longitude DECIMAL(9,6),
    @OperatingHours NVARCHAR(100) = NULL,
    @ContactNumber NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Stores (UserID, StoreName, StoreAddress, Latitude, Longitude, IsApproved, OperatingHours, ContactNumber)
    VALUES (@UserID, @StoreName, @StoreAddress, @Latitude, @Longitude, 0, @OperatingHours, @ContactNumber);
END
GO

-- User lookup by email
CREATE PROCEDURE sp_GetUserByEmail
    @Email NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*, r.RoleName 
    FROM Users u
    INNER JOIN Roles r ON u.RoleID = r.RoleID
    WHERE u.Email = @Email;
END
GO

-- User details by ID (including store if applicable)
CREATE PROCEDURE sp_GetUserById
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*, r.RoleName, s.StoreID, s.StoreName, s.StoreAddress, s.IsApproved, s.OperatingHours, s.ContactNumber
    FROM Users u
    INNER JOIN Roles r ON u.RoleID = r.RoleID
    LEFT JOIN Stores s ON u.UserID = s.UserID
    WHERE u.UserID = @UserID;
END
GO

-- Profile update
CREATE PROCEDURE sp_UpdateUserProfile
    @UserID INT,
    @FullName NVARCHAR(100),
    @Address NVARCHAR(255) = NULL,
    @Latitude DECIMAL(9,6) = NULL,
    @Longitude DECIMAL(9,6) = NULL,
    @AvatarUrl NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Users 
    SET FullName = @FullName,
        Address = COALESCE(@Address, Address),
        Latitude = COALESCE(@Latitude, Latitude),
        Longitude = COALESCE(@Longitude, Longitude),
        AvatarUrl = @AvatarUrl
    WHERE UserID = @UserID;
END
GO

-- Retrieve approved stores with coords
CREATE PROCEDURE sp_GetApprovedStores
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.*, u.Email, u.FullName
    FROM Stores s
    INNER JOIN Users u ON s.UserID = u.UserID
    WHERE s.IsApproved = 1;
END
GO

-- Retrieve stores waiting for admin approval
CREATE PROCEDURE sp_GetPendingStores
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.*, u.Email, u.FullName
    FROM Stores s
    INNER JOIN Users u ON s.UserID = u.UserID
    WHERE s.IsApproved = 0;
END
GO

-- Admin approval of a store
CREATE PROCEDURE sp_ApproveStore
    @StoreID INT,
    @IsApproved BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Stores
    SET IsApproved = @IsApproved
    WHERE StoreID = @StoreID;
    
    DECLARE @UserID INT;
    SELECT @UserID = UserID FROM Stores WHERE StoreID = @StoreID;
    
    DECLARE @Msg NVARCHAR(250) = CASE WHEN @IsApproved = 1 THEN 'Your store registration has been approved! You are now live.' ELSE 'Your store approval request was rejected.' END;
    
    INSERT INTO Notifications (UserID, Message, Type, IsRead, CreatedAt)
    VALUES (@UserID, @Msg, 'System', 0, GETDATE());
END
GO

-- Block or unblock a user/store
CREATE PROCEDURE sp_BlockUser
    @UserID INT,
    @IsBlocked BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Users
    SET IsBlocked = @IsBlocked
    WHERE UserID = @UserID;
END
GO

-- Create pickup request
CREATE PROCEDURE sp_CreatePickupRequest
    @UserID INT,
    @Address NVARCHAR(255),
    @Latitude DECIMAL(9,6) = NULL,
    @Longitude DECIMAL(9,6) = NULL,
    @Notes NVARCHAR(500) = NULL,
    @StoreID INT = NULL,
    @NewPickupID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO PickupRequests (UserID, StoreID, Address, ScheduledDate, Status, TotalWeight, TotalPoints, Latitude, Longitude, Notes, CreatedAt, UpdatedAt)
    VALUES (@UserID, @StoreID, @Address, NULL, 'Pending', NULL, NULL, @Latitude, @Longitude, @Notes, GETDATE(), GETDATE());
    
    SET @NewPickupID = SCOPE_IDENTITY();
END
GO

-- Add pickup items
CREATE PROCEDURE sp_AddPickupItem
    @PickupID INT,
    @CategoryID INT,
    @EstimatedWeight DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO PickupItems (PickupID, CategoryID, EstimatedWeight, ActualWeight)
    VALUES (@PickupID, @CategoryID, @EstimatedWeight, NULL);
END
GO

-- Get pickup requests for a specific user
CREATE PROCEDURE sp_GetPickupRequestsByUser
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.*, s.StoreName, s.ContactNumber AS StoreContact
    FROM PickupRequests p
    LEFT JOIN Stores s ON p.StoreID = s.StoreID
    WHERE p.UserID = @UserID
    ORDER BY p.CreatedAt DESC;
END
GO

-- Get pickup requests assigned to a store
CREATE PROCEDURE sp_GetPickupRequestsByStore
    @StoreID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.*, u.FullName AS UserFullName, u.Email AS UserEmail
    FROM PickupRequests p
    INNER JOIN Users u ON p.UserID = u.UserID
    WHERE p.StoreID = @StoreID
    ORDER BY p.UpdatedAt DESC;
END
GO

-- Get unassigned pending requests
CREATE PROCEDURE sp_GetPendingPickupRequests
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.*, u.FullName AS UserFullName, u.Email AS UserEmail
    FROM PickupRequests p
    INNER JOIN Users u ON p.UserID = u.UserID
    WHERE p.Status = 'Pending' AND p.StoreID IS NULL
    ORDER BY p.CreatedAt ASC;
END
GO

-- Update pickup status (accept/schedule/reject)
CREATE PROCEDURE sp_UpdatePickupStatus
    @PickupID INT,
    @StoreID INT = NULL,
    @Status NVARCHAR(50),
    @ScheduledDate DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE PickupRequests
    SET Status = @Status,
        StoreID = COALESCE(@StoreID, StoreID),
        ScheduledDate = COALESCE(@ScheduledDate, ScheduledDate),
        UpdatedAt = GETDATE()
    WHERE PickupID = @PickupID;
    
    DECLARE @UserID INT;
    SELECT @UserID = UserID FROM PickupRequests WHERE PickupID = @PickupID;
    
    DECLARE @Msg NVARCHAR(500);
    DECLARE @StoreName NVARCHAR(100);
    SELECT @StoreName = StoreName FROM Stores WHERE StoreID = COALESCE(@StoreID, (SELECT StoreID FROM PickupRequests WHERE PickupID = @PickupID));
    
    IF @Status = 'Accepted'
        SET @Msg = 'Your pickup request has been accepted by ' + COALESCE(@StoreName, 'a Store') + '.';
    ELSE IF @Status = 'Rejected'
        SET @Msg = 'Your pickup request has been declined.';
    ELSE IF @Status = 'Scheduled'
        SET @Msg = 'Your pickup request is scheduled for ' + CONVERT(NVARCHAR, @ScheduledDate, 100) + ' by ' + COALESCE(@StoreName, 'the Store') + '.';
        
    IF @Msg IS NOT NULL
    BEGIN
        INSERT INTO Notifications (UserID, Message, Type, IsRead, CreatedAt)
        VALUES (@UserID, @Msg, 'Pickup' + @Status, 0, GETDATE());
    END
END
GO

-- Complete pickup and reward points
CREATE PROCEDURE sp_CompletePickup
    @PickupID INT,
    @ActualWeight DECIMAL(10,2),
    @TotalPoints INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE PickupRequests
    SET Status = 'Completed',
        TotalWeight = @ActualWeight,
        TotalPoints = @TotalPoints,
        UpdatedAt = GETDATE()
    WHERE PickupID = @PickupID;
    
    DECLARE @UserID INT;
    DECLARE @StoreID INT;
    SELECT @UserID = UserID, @StoreID = StoreID FROM PickupRequests WHERE PickupID = @PickupID;
    
    -- Credit points to user
    UPDATE Users
    SET RewardPoints = RewardPoints + @TotalPoints
    WHERE UserID = @UserID;
    
    -- Update Leaderboard
    UPDATE Leaderboard
    SET TotalPoints = TotalPoints + @TotalPoints,
        LastUpdated = GETDATE()
    WHERE UserID = @UserID;
    
    -- Recalculate ranks
    WITH Ranked AS (
        SELECT UserID, ROW_NUMBER() OVER (ORDER BY TotalPoints DESC) AS NewRank
        FROM Leaderboard
    )
    UPDATE l
    SET Rank = r.NewRank
    FROM Leaderboard l
    INNER JOIN Ranked r ON l.UserID = r.UserID;
    
    -- Trigger notification
    DECLARE @Msg NVARCHAR(500) = 'Congratulations! Your pickup is complete. You earned ' + CAST(@TotalPoints AS NVARCHAR(10)) + ' reward points.';
    
    INSERT INTO Notifications (UserID, Message, Type, IsRead, CreatedAt)
    VALUES (@UserID, @Msg, 'PickupCompleted', 0, GETDATE());
    
    -- Audit Log
    INSERT INTO AuditLogs (UserID, Action, TableName, RecordID, Details, Timestamp)
    VALUES (@UserID, 'PickupCompleted', 'PickupRequests', @PickupID, 'Earned ' + CAST(@TotalPoints AS NVARCHAR(10)) + ' points', GETDATE());
END
GO

-- Get items associated with a pickup
CREATE PROCEDURE sp_GetPickupItems
    @PickupID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pi.*, wc.Name AS CategoryName, wc.PointsPerKg
    FROM PickupItems pi
    INNER JOIN WasteCategories wc ON pi.CategoryID = wc.CategoryID
    WHERE pi.PickupID = @PickupID;
END
GO

-- Update item weight
CREATE PROCEDURE sp_UpdatePickupItemActualWeight
    @ItemID INT,
    @ActualWeight DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PickupItems
    SET ActualWeight = @ActualWeight
    WHERE ItemID = @ItemID;
END
GO

-- Fetch Leaderboard
CREATE PROCEDURE sp_GetLeaderboard
    @Limit INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Limit) l.Rank, l.TotalPoints, u.FullName, u.AvatarUrl, u.Username
    FROM Leaderboard l
    INNER JOIN Users u ON l.UserID = u.UserID
    ORDER BY l.TotalPoints DESC;
END
GO

-- Fetch Rewards
CREATE PROCEDURE sp_GetRewards
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Rewards WHERE IsActive = 1 ORDER BY PointsCost ASC;
END
GO

-- Redeem reward and generate verification code
CREATE PROCEDURE sp_RedeemReward
    @UserID INT,
    @RewardID INT,
    @VerificationCode NVARCHAR(100),
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM Users WHERE UserID = @UserID AND IsBlocked = 1)
    BEGIN
        SET @Result = -1; -- Blocked
        RETURN;
    END
    
    DECLARE @Cost INT;
    DECLARE @Stock INT;
    SELECT @Cost = PointsCost, @Stock = StockCount FROM Rewards WHERE RewardID = @RewardID AND IsActive = 1;
    
    IF @Stock <= 0
    BEGIN
        SET @Result = -3; -- Out of stock
        RETURN;
    END
    
    DECLARE @UserPoints INT;
    SELECT @UserPoints = RewardPoints FROM Users WHERE UserID = @UserID;
    
    IF @UserPoints < @Cost
    BEGIN
        SET @Result = -2; -- Insufficient points
        RETURN;
    END
    
    UPDATE Users SET RewardPoints = RewardPoints - @Cost WHERE UserID = @UserID;
    UPDATE Rewards SET StockCount = StockCount - 1 WHERE RewardID = @RewardID;
    
    INSERT INTO RewardTransactions (UserID, RewardID, RedeemDate, PointsSpent, Status, VerificationCode)
    VALUES (@UserID, @RewardID, GETDATE(), @Cost, 'Pending', @VerificationCode);
    
    DECLARE @RewardName NVARCHAR(100);
    SELECT @RewardName = Name FROM Rewards WHERE RewardID = @RewardID;
    
    DECLARE @Msg NVARCHAR(500) = 'Redeemed ' + @RewardName + '. Code: ' + @VerificationCode + '. Present this QR code to an Admin.';
    
    INSERT INTO Notifications (UserID, Message, Type, IsRead, CreatedAt)
    VALUES (@UserID, @Msg, 'RewardEarned', 0, GETDATE());
    
    SET @Result = 1; -- Success
END
GO

-- Admin QR Verification
CREATE PROCEDURE sp_VerifyQRCode
    @VerificationCode NVARCHAR(100),
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TxID INT;
    DECLARE @Status NVARCHAR(50);
    DECLARE @UserID INT;
    
    SELECT @TxID = TransactionID, @Status = Status, @UserID = UserID 
    FROM RewardTransactions 
    WHERE VerificationCode = @VerificationCode;
    
    IF @TxID IS NULL
    BEGIN
        SET @Result = -1; -- Not found
        RETURN;
    END
    
    IF @Status = 'Redeemed'
    BEGIN
        SET @Result = -2; -- Already verified
        RETURN;
    END
    
    UPDATE RewardTransactions SET Status = 'Redeemed', RedeemDate = GETDATE() WHERE TransactionID = @TxID;
    
    INSERT INTO Notifications (UserID, Message, Type, IsRead, CreatedAt)
    VALUES (@UserID, 'Your reward redemption code ' + @VerificationCode + ' has been verified by the administrator.', 'System', 0, GETDATE());
    
    SET @Result = 1; -- Verified successfully
END
GO

-- Get active campaigns
CREATE PROCEDURE sp_GetCampaigns
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.*,
           (SELECT COUNT(*) FROM CampaignParticipants cp WHERE cp.CampaignID = c.CampaignID) AS MemberCount
    FROM Campaigns c
    ORDER BY c.IsActive DESC, c.EndDate ASC;
END
GO

-- User joins a campaign
CREATE PROCEDURE sp_JoinCampaign
    @CampaignID INT,
    @UserID INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Campaigns WHERE CampaignID = @CampaignID AND IsActive = 1 AND EndDate >= GETDATE())
    BEGIN
        SET @Result = -2; -- Inactive
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM CampaignParticipants WHERE CampaignID = @CampaignID AND UserID = @UserID)
    BEGIN
        SET @Result = -1; -- Already joined
        RETURN;
    END
    
    INSERT INTO CampaignParticipants (CampaignID, UserID, JoinedAt)
    VALUES (@CampaignID, @UserID, GETDATE());
    
    SET @Result = 1; -- Success
END
GO

-- Retrieve user notifications
CREATE PROCEDURE sp_GetNotifications
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Notifications
    WHERE UserID = @UserID
    ORDER BY CreatedAt DESC;
END
GO

-- Mark notifications as read
CREATE PROCEDURE sp_MarkNotificationsRead
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Notifications
    SET IsRead = 1
    WHERE UserID = @UserID AND IsRead = 0;
END
GO

-- Admin Dashboard aggregates
CREATE PROCEDURE sp_GetAdminDashboardStats
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        (SELECT COUNT(*) FROM Users u INNER JOIN Roles r ON u.RoleID = r.RoleID WHERE r.RoleName = 'User') AS TotalUsers,
        (SELECT COUNT(*) FROM Stores) AS TotalStores,
        (SELECT COUNT(*) FROM Stores WHERE IsApproved = 0) AS PendingStores,
        (SELECT COUNT(*) FROM PickupRequests) AS TotalPickups,
        (SELECT COUNT(*) FROM PickupRequests WHERE Status = 'Completed') AS CompletedPickups,
        (SELECT COUNT(*) FROM PickupRequests WHERE Status = 'Pending') AS PendingPickups;
        
    SELECT wc.Name AS CategoryName, COALESCE(SUM(pi.ActualWeight), 0) AS TotalWeight
    FROM WasteCategories wc
    LEFT JOIN PickupItems pi ON wc.CategoryID = pi.CategoryID
    LEFT JOIN PickupRequests pr ON pi.PickupID = pr.PickupID AND pr.Status = 'Completed'
    GROUP BY wc.Name;
    
    SELECT 
        FORMAT(pr.CreatedAt, 'yyyy-MM') AS MonthLabel,
        COUNT(pr.PickupID) AS TotalPickups,
        COALESCE(SUM(pr.TotalWeight), 0) AS TotalWeight
    FROM PickupRequests pr
    WHERE pr.Status = 'Completed' AND pr.CreatedAt >= DATEADD(month, -6, GETDATE())
    GROUP BY FORMAT(pr.CreatedAt, 'yyyy-MM')
    ORDER BY MonthLabel ASC;
END
GO

-- Store Dashboard stats
CREATE PROCEDURE sp_GetStoreDashboardStats
    @StoreID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        (SELECT COUNT(*) FROM PickupRequests WHERE StoreID = @StoreID) AS TotalAssigned,
        (SELECT COUNT(*) FROM PickupRequests WHERE StoreID = @StoreID AND Status = 'Completed') AS TotalCompleted,
        (SELECT COUNT(*) FROM PickupRequests WHERE StoreID = @StoreID AND Status = 'Scheduled') AS TotalScheduled,
        (SELECT COALESCE(SUM(TotalWeight), 0) FROM PickupRequests WHERE StoreID = @StoreID AND Status = 'Completed') AS TotalWeightCollected;
        
    SELECT wc.Name AS CategoryName, COALESCE(SUM(pi.ActualWeight), 0) AS TotalWeight
    FROM WasteCategories wc
    LEFT JOIN PickupItems pi ON wc.CategoryID = pi.CategoryID
    LEFT JOIN PickupRequests pr ON pi.PickupID = pr.PickupID AND pr.StoreID = @StoreID AND pr.Status = 'Completed'
    GROUP BY wc.Name;
END
GO

-- Create Audit Log
CREATE PROCEDURE sp_AddAuditLog
    @UserID INT = NULL,
    @Action NVARCHAR(100),
    @TableName NVARCHAR(50) = NULL,
    @RecordID INT = NULL,
    @Details NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO AuditLogs (UserID, Action, TableName, RecordID, Details, Timestamp)
    VALUES (@UserID, @Action, @TableName, @RecordID, @Details, GETDATE());
END
GO

-- Get settings
CREATE PROCEDURE sp_GetSystemSettings
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Settings;
END
GO

-- Update a system setting
CREATE PROCEDURE sp_UpdateSystemSetting
    @SettingKey NVARCHAR(50),
    @SettingValue NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM Settings WHERE SettingKey = @SettingKey)
        UPDATE Settings SET SettingValue = @SettingValue WHERE SettingKey = @SettingKey;
    ELSE
        INSERT INTO Settings (SettingKey, SettingValue) VALUES (@SettingKey, @SettingValue);
END
GO
