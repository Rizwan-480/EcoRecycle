using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EcoRecycle.Models;
using EcoRecycle.Models.ViewModels;

namespace EcoRecycle.DAL
{
    public class ContentDAL
    {
        private readonly DatabaseHelper _db;

        public ContentDAL(DatabaseHelper db)
        {
            _db = db;
        }

        #region Eco Tips
        public List<EcoTip> GetEcoTips()
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetEcoTips]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetEcoTips
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT * FROM EcoTips ORDER BY CreatedAt DESC;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            DataTable dt = _db.ExecuteReaderTable("sp_GetEcoTips");
            List<EcoTip> list = new List<EcoTip>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new EcoTip
                {
                    TipID = (int)row["TipID"],
                    Title = row["Title"].ToString(),
                    TipContent = row["TipContent"].ToString(),
                    Category = row["Category"] == DBNull.Value ? null : row["Category"].ToString(),
                    CreatedAt = (DateTime)row["CreatedAt"]
                });
            }
            return list;
        }

        public void SaveEcoTip(int tipId, string title, string content, string category)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_SaveEcoTip]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_SaveEcoTip
                            @TipID INT,
                            @Title NVARCHAR(200),
                            @TipContent NVARCHAR(1000),
                            @Category NVARCHAR(50)
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            IF @TipID = 0
                                INSERT INTO EcoTips (Title, TipContent, Category, CreatedAt)
                                VALUES (@Title, @TipContent, @Category, GETDATE());
                            ELSE
                                UPDATE EcoTips
                                SET Title = @Title,
                                    TipContent = @TipContent,
                                    Category = @Category
                                WHERE TipID = @TipID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@TipID", tipId),
                new SqlParameter("@Title", title),
                new SqlParameter("@TipContent", content),
                new SqlParameter("@Category", (object)category ?? DBNull.Value)
            };
            _db.ExecuteNonQuery("sp_SaveEcoTip", parameters);
        }
        #endregion

        #region News
        public List<News> GetNewsList()
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetNewsList]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetNewsList
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT * FROM News ORDER BY CreatedAt DESC;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            DataTable dt = _db.ExecuteReaderTable("sp_GetNewsList");
            List<News> list = new List<News>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new News
                {
                    NewsID = (int)row["NewsID"],
                    Title = row["Title"].ToString(),
                    Content = row["Content"].ToString(),
                    ImageUrl = row["ImageUrl"] == DBNull.Value ? null : row["ImageUrl"].ToString(),
                    CreatedAt = (DateTime)row["CreatedAt"]
                });
            }
            return list;
        }

        public News GetNewsById(int newsId)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetNewsById]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetNewsById
                            @NewsID INT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT * FROM News WHERE NewsID = @NewsID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@NewsID", newsId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetNewsById", parameters);
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new News
            {
                NewsID = (int)row["NewsID"],
                Title = row["Title"].ToString(),
                Content = row["Content"].ToString(),
                ImageUrl = row["ImageUrl"] == DBNull.Value ? null : row["ImageUrl"].ToString(),
                CreatedAt = (DateTime)row["CreatedAt"]
            };
        }

        public void SaveNews(int newsId, string title, string content, string imageUrl)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_SaveNews]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_SaveNews
                            @NewsID INT,
                            @Title NVARCHAR(200),
                            @Content NVARCHAR(MAX),
                            @ImageUrl NVARCHAR(255)
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            IF @NewsID = 0
                                INSERT INTO News (Title, Content, ImageUrl, CreatedAt)
                                VALUES (@Title, @Content, @ImageUrl, GETDATE());
                            ELSE
                                UPDATE News
                                SET Title = @Title,
                                    Content = @Content,
                                    ImageUrl = COALESCE(@ImageUrl, ImageUrl)
                                WHERE NewsID = @NewsID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@NewsID", newsId),
                new SqlParameter("@Title", title),
                new SqlParameter("@Content", content),
                new SqlParameter("@ImageUrl", (object)imageUrl ?? DBNull.Value)
            };
            _db.ExecuteNonQuery("sp_SaveNews", parameters);
        }
        #endregion

        #region Leaderboard
        public List<LeaderboardRow> GetLeaderboard(int limit = 10)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Limit", limit)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetLeaderboard", parameters);
            List<LeaderboardRow> list = new List<LeaderboardRow>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new LeaderboardRow
                {
                    Rank = row["Rank"] == DBNull.Value ? 0 : (int)row["Rank"],
                    TotalPoints = (int)row["TotalPoints"],
                    FullName = row["FullName"].ToString(),
                    AvatarUrl = row["AvatarUrl"] == DBNull.Value ? null : row["AvatarUrl"].ToString(),
                    Username = row["Username"].ToString()
                });
            }
            return list;
        }
        #endregion

        #region Badges
        public List<Badge> GetBadges()
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM Badges ORDER BY ThresholdPoints ASC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        List<Badge> list = new List<Badge>();
                        foreach (DataRow row in dt.Rows)
                        {
                            list.Add(new Badge
                            {
                                BadgeID = (int)row["BadgeID"],
                                Name = row["Name"].ToString(),
                                Description = row["Description"] == DBNull.Value ? null : row["Description"].ToString(),
                                IconUrl = row["IconUrl"] == DBNull.Value ? null : row["IconUrl"].ToString(),
                                ThresholdPoints = (int)row["ThresholdPoints"]
                            });
                        }
                        return list;
                    }
                }
            }
        }

        public List<Badge> GetEarnedBadges(int points)
        {
            var all = GetBadges();
            var earned = new List<Badge>();
            foreach (var b in all)
            {
                if (points >= b.ThresholdPoints)
                {
                    earned.Add(b);
                }
            }
            return earned;
        }
        #endregion

        #region Settings
        public Dictionary<string, string> GetSettings()
        {
            DataTable dt = _db.ExecuteReaderTable("sp_GetSystemSettings");
            var dict = new Dictionary<string, string>();
            foreach (DataRow row in dt.Rows)
            {
                dict[row["SettingKey"].ToString()] = row["SettingValue"].ToString();
            }
            return dict;
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            var settings = GetSettings();
            if (settings.ContainsKey(key)) return settings[key];
            return defaultValue;
        }

        public void UpdateSetting(string key, string value)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@SettingKey", key),
                new SqlParameter("@SettingValue", value)
            };
            _db.ExecuteNonQuery("sp_UpdateSystemSetting", parameters);
        }
        #endregion

        #region Audit Logs & Admin Stats
        public void AddAuditLog(int? userId, string action, string tableName, int? recordId, string details)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", (object)userId ?? DBNull.Value),
                new SqlParameter("@Action", action),
                new SqlParameter("@TableName", (object)tableName ?? DBNull.Value),
                new SqlParameter("@RecordID", (object)recordId ?? DBNull.Value),
                new SqlParameter("@Details", (object)details ?? DBNull.Value)
            };
            _db.ExecuteNonQuery("sp_AddAuditLog", parameters);
        }

        public List<AuditLog> GetAuditLogs()
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT a.*, u.FullName AS UserFullName
                    FROM AuditLogs a
                    LEFT JOIN Users u ON a.UserID = u.UserID
                    ORDER BY a.Timestamp DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        List<AuditLog> list = new List<AuditLog>();
                        foreach (DataRow row in dt.Rows)
                        {
                            list.Add(new AuditLog
                            {
                                LogID = (int)row["LogID"],
                                UserID = row["UserID"] == DBNull.Value ? null : (int?)row["UserID"],
                                UserFullName = row["UserFullName"] == DBNull.Value ? "System" : row["UserFullName"].ToString(),
                                Action = row["Action"].ToString(),
                                TableName = row["TableName"] == DBNull.Value ? null : row["TableName"].ToString(),
                                RecordID = row["RecordID"] == DBNull.Value ? null : (int?)row["RecordID"],
                                Details = row["Details"] == DBNull.Value ? null : row["Details"].ToString(),
                                Timestamp = (DateTime)row["Timestamp"]
                            });
                        }
                        return list;
                    }
                }
            }
        }

        public AdminDashboardViewModel GetAdminDashboardStats()
        {
            DataSet ds = _db.ExecuteReaderSet("sp_GetAdminDashboardStats");
            var vm = new AdminDashboardViewModel();

            // First Table: Simple Counts
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                vm.TotalUsers = (int)dr["TotalUsers"];
                vm.TotalStores = (int)dr["TotalStores"];
                vm.PendingStores = (int)dr["PendingStores"];
                vm.TotalPickups = (int)dr["TotalPickups"];
                vm.CompletedPickups = (int)dr["CompletedPickups"];
                vm.PendingPickups = (int)dr["PendingPickups"];
            }

            // Second Table: Category stats
            if (ds.Tables.Count > 1)
            {
                foreach (DataRow row in ds.Tables[1].Rows)
                {
                    vm.CategoryStats.Add(new CategoryWeightStat
                    {
                        CategoryName = row["CategoryName"].ToString(),
                        TotalWeight = (decimal)row["TotalWeight"]
                    });
                }
            }

            // Third Table: Monthly stats
            if (ds.Tables.Count > 2)
            {
                foreach (DataRow row in ds.Tables[2].Rows)
                {
                    vm.MonthlyStats.Add(new MonthlyPickupStat
                    {
                        MonthLabel = row["MonthLabel"].ToString(),
                        TotalPickups = (int)row["TotalPickups"],
                        TotalWeight = (decimal)row["TotalWeight"]
                    });
                }
            }

            return vm;
        }

        public StoreDashboardViewModel GetStoreDashboardStats(int storeId, string storeName, bool isApproved)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@StoreID", storeId)
            };

            DataSet ds = _db.ExecuteReaderSet("sp_GetStoreDashboardStats", parameters);
            var vm = new StoreDashboardViewModel
            {
                StoreID = storeId,
                StoreName = storeName,
                IsApproved = isApproved
            };

            // First table: Stats
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                vm.TotalAssigned = (int)dr["TotalAssigned"];
                vm.TotalCompleted = (int)dr["TotalCompleted"];
                vm.TotalScheduled = (int)dr["TotalScheduled"];
                vm.TotalWeightCollected = (decimal)dr["TotalWeightCollected"];
            }

            // Second table: Category weights
            if (ds.Tables.Count > 1)
            {
                foreach (DataRow row in ds.Tables[1].Rows)
                {
                    vm.CategoryStats.Add(new CategoryWeightStat
                    {
                        CategoryName = row["CategoryName"].ToString(),
                        TotalWeight = (decimal)row["TotalWeight"]
                    });
                }
            }

            return vm;
        }
        #endregion
    }
}
