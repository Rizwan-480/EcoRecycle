using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EcoRecycle.Models;

namespace EcoRecycle.DAL
{
    public class NotificationDAL
    {
        private readonly DatabaseHelper _db;

        public NotificationDAL(DatabaseHelper db)
        {
            _db = db;
        }

        public List<Notification> GetNotifications(int userId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetNotifications", parameters);
            List<Notification> notifications = new List<Notification>();
            foreach (DataRow row in dt.Rows)
            {
                notifications.Add(new Notification
                {
                    NotificationID = (int)row["NotificationID"],
                    UserID = (int)row["UserID"],
                    Message = row["Message"].ToString(),
                    Type = row["Type"].ToString(),
                    IsRead = (bool)row["IsRead"],
                    CreatedAt = (DateTime)row["CreatedAt"]
                });
            }
            return notifications;
        }

        public void MarkNotificationsRead(int userId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId)
            };
            _db.ExecuteNonQuery("sp_MarkNotificationsRead", parameters);
        }

        public void CreateNotification(int userId, string message, string type)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CreateNotification]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_CreateNotification
                            @UserID INT,
                            @Message NVARCHAR(500),
                            @Type NVARCHAR(50)
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            INSERT INTO Notifications (UserID, Message, Type, IsRead, CreatedAt)
                            VALUES (@UserID, @Message, @Type, 0, GETDATE());
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId),
                new SqlParameter("@Message", message),
                new SqlParameter("@Type", type)
            };
            _db.ExecuteNonQuery("sp_CreateNotification", parameters);
        }

        public int GetUnreadCount(int userId)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string query = "SELECT COUNT(1) FROM Notifications WHERE UserID = @UserID AND IsRead = 0";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }
    }
}
