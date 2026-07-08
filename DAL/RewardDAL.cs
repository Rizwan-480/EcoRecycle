using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EcoRecycle.Models;

namespace EcoRecycle.DAL
{
    public class RewardDAL
    {
        private readonly DatabaseHelper _db;

        public RewardDAL(DatabaseHelper db)
        {
            _db = db;
        }

        public List<Reward> GetRewards()
        {
            DataTable dt = _db.ExecuteReaderTable("sp_GetRewards");
            List<Reward> rewards = new List<Reward>();
            foreach (DataRow row in dt.Rows)
            {
                rewards.Add(MapRowToReward(row));
            }
            return rewards;
        }

        public Reward GetRewardById(int rewardId)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetRewardById]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetRewardById
                            @RewardID INT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT * FROM Rewards WHERE RewardID = @RewardID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@RewardID", rewardId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetRewardById", parameters);
            if (dt.Rows.Count == 0) return null;
            return MapRowToReward(dt.Rows[0]);
        }

        public int RedeemReward(int userId, int rewardId, string verificationCode)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId),
                new SqlParameter("@RewardID", rewardId),
                new SqlParameter("@VerificationCode", verificationCode),
                new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };

            _db.ExecuteNonQuery("sp_RedeemReward", parameters);
            return (int)parameters[3].Value;
        }

        public int VerifyQRCode(string verificationCode)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@VerificationCode", verificationCode),
                new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };

            _db.ExecuteNonQuery("sp_VerifyQRCode", parameters);
            return (int)parameters[1].Value;
        }

        public RewardTransaction GetTransactionByCode(string code)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetTransactionByCode]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetTransactionByCode
                            @VerificationCode NVARCHAR(100)
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT t.*, u.Username, u.FullName AS UserFullName, r.Name AS RewardName
                            FROM RewardTransactions t
                            INNER JOIN Users u ON t.UserID = u.UserID
                            INNER JOIN Rewards r ON t.RewardID = r.RewardID
                            WHERE t.VerificationCode = @VerificationCode;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@VerificationCode", code)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetTransactionByCode", parameters);
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new RewardTransaction
            {
                TransactionID = (int)row["TransactionID"],
                UserID = (int)row["UserID"],
                RewardID = (int)row["RewardID"],
                RedeemDate = (DateTime)row["RedeemDate"],
                PointsSpent = (int)row["PointsSpent"],
                Status = row["Status"].ToString(),
                VerificationCode = row["VerificationCode"].ToString(),
                Username = row["Username"].ToString(),
                UserFullName = row["UserFullName"].ToString(),
            };
        }

        public List<RewardTransaction> GetPendingTransactions()
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetPendingTransactions]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetPendingTransactions
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT t.*, u.Username, u.FullName AS UserFullName, r.Name AS RewardName
                            FROM RewardTransactions t
                            INNER JOIN Users u ON t.UserID = u.UserID
                            INNER JOIN Rewards r ON t.RewardID = r.RewardID
                            WHERE t.Status = ''Pending''
                            ORDER BY t.RedeemDate DESC;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            DataTable dt = _db.ExecuteReaderTable("sp_GetPendingTransactions");
            List<RewardTransaction> list = new List<RewardTransaction>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new RewardTransaction
                {
                    TransactionID = (int)row["TransactionID"],
                    UserID = (int)row["UserID"],
                    RewardID = (int)row["RewardID"],
                    RedeemDate = (DateTime)row["RedeemDate"],
                    PointsSpent = (int)row["PointsSpent"],
                    Status = row["Status"].ToString(),
                    VerificationCode = row["VerificationCode"].ToString(),
                    Username = row["Username"].ToString(),
                    UserFullName = row["UserFullName"].ToString(),
                    RewardName = row["RewardName"].ToString()
                });
            }
            return list;
        }

        public List<RewardTransaction> GetUserRedemptions(int userId)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetUserRedemptions]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetUserRedemptions
                            @UserID INT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT t.*, r.Name AS RewardName
                            FROM RewardTransactions t
                            INNER JOIN Rewards r ON t.RewardID = r.RewardID
                            WHERE t.UserID = @UserID
                            ORDER BY t.RedeemDate DESC;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetUserRedemptions", parameters);
            List<RewardTransaction> list = new List<RewardTransaction>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new RewardTransaction
                {
                    TransactionID = (int)row["TransactionID"],
                    UserID = (int)row["UserID"],
                    RewardID = (int)row["RewardID"],
                    RedeemDate = (DateTime)row["RedeemDate"],
                    PointsSpent = (int)row["PointsSpent"],
                    Status = row["Status"].ToString(),
                    VerificationCode = row["VerificationCode"].ToString(),
                    RewardName = row["RewardName"].ToString()
                });
            }
            return list;
        }

        public void SaveReward(int rewardId, string name, string description, int pointsCost, int stockCount, bool isActive, string imageUrl)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_SaveReward]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_SaveReward
                            @RewardID INT,
                            @Name NVARCHAR(100),
                            @Description NVARCHAR(500),
                            @PointsCost INT,
                            @StockCount INT,
                            @IsActive BIT,
                            @ImageUrl NVARCHAR(255)
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            IF @RewardID = 0
                                INSERT INTO Rewards (Name, Description, PointsCost, StockCount, IsActive, ImageUrl)
                                VALUES (@Name, @Description, @PointsCost, @StockCount, @IsActive, @ImageUrl);
                            ELSE
                                UPDATE Rewards
                                SET Name = @Name,
                                    Description = @Description,
                                    PointsCost = @PointsCost,
                                    StockCount = @StockCount,
                                    IsActive = @IsActive,
                                    ImageUrl = COALESCE(@ImageUrl, ImageUrl)
                                WHERE RewardID = @RewardID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@RewardID", rewardId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Description", (object)description ?? DBNull.Value),
                new SqlParameter("@PointsCost", pointsCost),
                new SqlParameter("@StockCount", stockCount),
                new SqlParameter("@IsActive", isActive),
                new SqlParameter("@ImageUrl", (object)imageUrl ?? DBNull.Value)
            };
            _db.ExecuteNonQuery("sp_SaveReward", parameters);
        }

        private Reward MapRowToReward(DataRow dr)
        {
            return new Reward
            {
                RewardID = (int)dr["RewardID"],
                Name = dr["Name"].ToString(),
                Description = dr["Description"] == DBNull.Value ? null : dr["Description"].ToString(),
                PointsCost = (int)dr["PointsCost"],
                StockCount = (int)dr["StockCount"],
                IsActive = (bool)dr["IsActive"],
                ImageUrl = dr["ImageUrl"] == DBNull.Value ? null : dr["ImageUrl"].ToString()
            };
        }
    }
}
