using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EcoRecycle.Models;

namespace EcoRecycle.DAL
{
    public class CampaignDAL
    {
        private readonly DatabaseHelper _db;

        public CampaignDAL(DatabaseHelper db)
        {
            _db = db;
        }

        public List<Campaign> GetCampaigns(int? userId = null)
        {
            DataTable dt = _db.ExecuteReaderTable("sp_GetCampaigns");
            List<Campaign> campaigns = new List<Campaign>();
            foreach (DataRow row in dt.Rows)
            {
                var camp = MapRowToCampaign(row);
                if (userId.HasValue)
                {
                    camp.UserJoined = IsUserJoined(camp.CampaignID, userId.Value);
                }
                campaigns.Add(camp);
            }
            return campaigns;
        }

        public Campaign GetCampaignById(int campaignId, int? userId = null)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetCampaignById]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetCampaignById
                            @CampaignID INT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT c.*,
                                   (SELECT COUNT(*) FROM CampaignParticipants cp WHERE cp.CampaignID = c.CampaignID) AS MemberCount
                            FROM Campaigns c
                            WHERE c.CampaignID = @CampaignID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CampaignID", campaignId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetCampaignById", parameters);
            if (dt.Rows.Count == 0) return null;

            var camp = MapRowToCampaign(dt.Rows[0]);
            if (userId.HasValue)
            {
                camp.UserJoined = IsUserJoined(campaignId, userId.Value);
            }
            return camp;
        }

        public int JoinCampaign(int campaignId, int userId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CampaignID", campaignId),
                new SqlParameter("@UserID", userId),
                new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.ExecuteNonQuery("sp_JoinCampaign", parameters);
            return (int)parameters[2].Value;
        }

        public bool IsUserJoined(int campaignId, int userId)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string query = "SELECT COUNT(1) FROM CampaignParticipants WHERE CampaignID = @CampaignID AND UserID = @UserID";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CampaignID", campaignId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public void SaveCampaign(int campaignId, string name, string description, decimal targetGoal, DateTime startDate, DateTime endDate, bool isActive)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_SaveCampaign]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_SaveCampaign
                            @CampaignID INT,
                            @Name NVARCHAR(100),
                            @Description NVARCHAR(1000),
                            @TargetGoal DECIMAL(10,2),
                            @StartDate DATETIME,
                            @EndDate DATETIME,
                            @IsActive BIT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            IF @CampaignID = 0
                                INSERT INTO Campaigns (Name, Description, TargetGoal, CurrentProgress, StartDate, EndDate, IsActive, CreatedAt)
                                VALUES (@Name, @Description, @TargetGoal, 0.00, @StartDate, @EndDate, @IsActive, GETDATE());
                            ELSE
                                UPDATE Campaigns
                                SET Name = @Name,
                                    Description = @Description,
                                    TargetGoal = @TargetGoal,
                                    StartDate = @StartDate,
                                    EndDate = @EndDate,
                                    IsActive = @IsActive
                                WHERE CampaignID = @CampaignID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CampaignID", campaignId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Description", (object)description ?? DBNull.Value),
                new SqlParameter("@TargetGoal", targetGoal),
                new SqlParameter("@StartDate", startDate),
                new SqlParameter("@EndDate", endDate),
                new SqlParameter("@IsActive", isActive)
            };
            _db.ExecuteNonQuery("sp_SaveCampaign", parameters);
        }

        private Campaign MapRowToCampaign(DataRow dr)
        {
            return new Campaign
            {
                CampaignID = (int)dr["CampaignID"],
                Name = dr["Name"].ToString(),
                Description = dr["Description"] == DBNull.Value ? null : dr["Description"].ToString(),
                TargetGoal = (decimal)dr["TargetGoal"],
                CurrentProgress = (decimal)dr["CurrentProgress"],
                StartDate = (DateTime)dr["StartDate"],
                EndDate = (DateTime)dr["EndDate"],
                IsActive = (bool)dr["IsActive"],
                CreatedAt = (DateTime)dr["CreatedAt"],
                MemberCount = dr.Table.Columns.Contains("MemberCount") ? (int)dr["MemberCount"] : 0
            } ;
        }
    }
}
