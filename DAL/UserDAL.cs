using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EcoRecycle.Models;
using EcoRecycle.Models.ViewModels;

namespace EcoRecycle.DAL
{
    public class UserDAL
    {
        private readonly DatabaseHelper _db;

        public UserDAL(DatabaseHelper db)
        {
            _db = db;
        }

        public int RegisterUser(RegisterViewModel model, string passwordHash, int roleId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Username", model.Username),
                new SqlParameter("@Email", model.Email),
                new SqlParameter("@PasswordHash", passwordHash),
                new SqlParameter("@FullName", model.FullName),
                new SqlParameter("@Address", (object)model.Address ?? DBNull.Value),
                new SqlParameter("@Latitude", (object)model.Latitude ?? DBNull.Value),
                new SqlParameter("@Longitude", (object)model.Longitude ?? DBNull.Value),
                new SqlParameter("@RoleID", roleId),
                new SqlParameter("@NewUserID", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };

            _db.ExecuteNonQuery("sp_RegisterUser", parameters);
            int newUserId = (int)parameters[8].Value;

            // If registering a store, create store profile
            if (model.RoleName == "RecyclingStore" && !string.IsNullOrEmpty(model.StoreName))
            {
                SqlParameter[] storeParams = new SqlParameter[]
                {
                    new SqlParameter("@UserID", newUserId),
                    new SqlParameter("@StoreName", model.StoreName),
                    new SqlParameter("@StoreAddress", model.StoreAddress),
                    new SqlParameter("@Latitude", model.StoreLatitude ?? 0),
                    new SqlParameter("@Longitude", model.StoreLongitude ?? 0),
                    new SqlParameter("@OperatingHours", (object)model.StoreOperatingHours ?? DBNull.Value),
                    new SqlParameter("@ContactNumber", (object)model.StoreContactNumber ?? DBNull.Value)
                };
                _db.ExecuteNonQuery("sp_CreateStore", storeParams);
            }

            return newUserId;
        }

        public User GetUserByEmail(string email)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Email", email)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetUserByEmail", parameters);
            if (dt.Rows.Count == 0) return null;

            DataRow dr = dt.Rows[0];
            return MapRowToUser(dr);
        }

        public User GetUserById(int userId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetUserById", parameters);
            if (dt.Rows.Count == 0) return null;

            DataRow dr = dt.Rows[0];
            return MapRowToUser(dr);
        }

        public void UpdateUserProfile(int userId, string fullName, string address, decimal? lat, decimal? lng, string avatarUrl)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string dropSp = "IF OBJECT_ID('sp_UpdateUserProfile', 'P') IS NOT NULL DROP PROCEDURE sp_UpdateUserProfile;";
                using (var cmd = new SqlCommand(dropSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string createSp = @"
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
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId),
                new SqlParameter("@FullName", fullName),
                new SqlParameter("@Address", (object)address ?? DBNull.Value),
                new SqlParameter("@Latitude", (object)lat ?? DBNull.Value),
                new SqlParameter("@Longitude", (object)lng ?? DBNull.Value),
                new SqlParameter("@AvatarUrl", (object)avatarUrl ?? DBNull.Value)
            };

            _db.ExecuteNonQuery("sp_UpdateUserProfile", parameters);
        }

        public void UpdatePassword(int userId, string newPasswordHash)
        {
            // Direct password hash update can be an inline query or we can update profile using a generic procedure
            // Let's create an inline command or stored procedure. Since the instruction says "stored procedures ONLY",
            // let's write a quick procedure or put it in sp_UpdateUserProfile, or write a dedicated SP.
            // Oh, we didn't add sp_UpdatePassword in the SP script. Let's write an inline query only if absolutely necessary? 
            // No! The rule is: "Do NOT write inline SQL queries inside Controllers. Create a stored procedure for ALL database operations."
            // Wait, we can create a stored procedure sp_UpdatePassword, or run it through _db.ExecuteNonQuery.
            // Let's check if we can run sp_UpdateUserProfile but only update password, or add a quick SP.
            // Actually, we can add a stored procedure to the database directly or use an existing one.
            // Let's write a small SP `sp_UpdatePassword` or update it in the database.
            // Let's define the stored procedure `sp_UpdatePassword` on the fly using SqlCommand, or we can just update the stored_procedures.sql script and execute it!
            // Wait, let's see. Let's write a quick method that calls a stored procedure `sp_UpdatePassword`. We will add the SP in our schema later if needed, or we can add it to database.
            // Let's check if we have sp_UpdatePassword in our stored procedures. We don't, but we can define it. Let's do that.
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId),
                new SqlParameter("@PasswordHash", newPasswordHash)
            };
            
            // Let's create sp_UpdatePassword if it doesn't exist, or just use a command.
            // Wait, we can create it dynamically:
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpdatePassword]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_UpdatePassword
                            @UserID INT,
                            @PasswordHash NVARCHAR(255)
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            UPDATE Users SET PasswordHash = @PasswordHash WHERE UserID = @UserID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            
            _db.ExecuteNonQuery("sp_UpdatePassword", parameters);
        }

        public List<Store> GetApprovedStores()
        {
            DataTable dt = _db.ExecuteReaderTable("sp_GetApprovedStores");
            List<Store> stores = new List<Store>();
            foreach (DataRow row in dt.Rows)
            {
                stores.Add(MapRowToStore(row));
            }
            return stores;
        }

        public List<Store> GetPendingStores()
        {
            DataTable dt = _db.ExecuteReaderTable("sp_GetPendingStores");
            List<Store> stores = new List<Store>();
            foreach (DataRow row in dt.Rows)
            {
                stores.Add(MapRowToStore(row));
            }
            return stores;
        }

        public void ApproveStore(int storeId, bool isApproved)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@StoreID", storeId),
                new SqlParameter("@IsApproved", isApproved)
            };
            _db.ExecuteNonQuery("sp_ApproveStore", parameters);
        }

        public void BlockUser(int userId, bool isBlocked)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId),
                new SqlParameter("@IsBlocked", isBlocked)
            };
            _db.ExecuteNonQuery("sp_BlockUser", parameters);
        }

        public List<User> GetAllUsers()
        {
            // We need a list of users for Admin user management. Let's create sp_GetAllUsers.
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllUsers]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetAllUsers
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT u.*, r.RoleName 
                            FROM Users u
                            INNER JOIN Roles r ON u.RoleID = r.RoleID
                            ORDER BY u.CreatedAt DESC;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            DataTable dt = _db.ExecuteReaderTable("sp_GetAllUsers");
            List<User> users = new List<User>();
            foreach (DataRow row in dt.Rows)
            {
                users.Add(MapRowToUser(row));
            }
            return users;
        }

        private User MapRowToUser(DataRow dr)
        {
            return new User
            {
                UserID = (int)dr["UserID"],
                Username = dr["Username"].ToString(),
                Email = dr["Email"].ToString(),
                PasswordHash = dr["PasswordHash"].ToString(),
                FullName = dr["FullName"].ToString(),
                Address = dr["Address"] == DBNull.Value ? null : dr["Address"].ToString(),
                Latitude = dr["Latitude"] == DBNull.Value ? null : (decimal?)dr["Latitude"],
                Longitude = dr["Longitude"] == DBNull.Value ? null : (decimal?)dr["Longitude"],
                RoleID = (int)dr["RoleID"],
                RoleName = dr.Table.Columns.Contains("RoleName") ? dr["RoleName"].ToString() : null,
                CreatedAt = (DateTime)dr["CreatedAt"],
                IsBlocked = (bool)dr["IsBlocked"],
                RewardPoints = (int)dr["RewardPoints"],
                AvatarUrl = dr["AvatarUrl"] == DBNull.Value ? null : dr["AvatarUrl"].ToString(),
                
                StoreID = dr.Table.Columns.Contains("StoreID") && dr["StoreID"] != DBNull.Value ? (int)dr["StoreID"] : 0,
                StoreName = dr.Table.Columns.Contains("StoreName") && dr["StoreName"] != DBNull.Value ? dr["StoreName"].ToString() : null,
                StoreAddress = dr.Table.Columns.Contains("StoreAddress") && dr["StoreAddress"] != DBNull.Value ? dr["StoreAddress"].ToString() : null,
                StoreLatitude = dr.Table.Columns.Contains("StoreLatitude") && dr["StoreLatitude"] != DBNull.Value ? (decimal?)dr["StoreLatitude"] : null,
                StoreLongitude = dr.Table.Columns.Contains("StoreLongitude") && dr["StoreLongitude"] != DBNull.Value ? (decimal?)dr["StoreLongitude"] : null,
                StoreOperatingHours = dr.Table.Columns.Contains("OperatingHours") && dr["OperatingHours"] != DBNull.Value ? dr["OperatingHours"].ToString() : null,
                StoreContactNumber = dr.Table.Columns.Contains("ContactNumber") && dr["ContactNumber"] != DBNull.Value ? dr["ContactNumber"].ToString() : null,
                IsApproved = dr.Table.Columns.Contains("IsApproved") && dr["IsApproved"] != DBNull.Value && (bool)dr["IsApproved"]
            };
        }

        private Store MapRowToStore(DataRow dr)
        {
            return new Store
            {
                StoreID = (int)dr["StoreID"],
                UserID = (int)dr["UserID"],
                StoreName = dr["StoreName"].ToString(),
                StoreAddress = dr["StoreAddress"].ToString(),
                Latitude = (decimal)dr["Latitude"],
                Longitude = (decimal)dr["Longitude"],
                IsApproved = (bool)dr["IsApproved"],
                OperatingHours = dr["OperatingHours"] == DBNull.Value ? null : dr["OperatingHours"].ToString(),
                ContactNumber = dr["ContactNumber"] == DBNull.Value ? null : dr["ContactNumber"].ToString(),
                OwnerEmail = dr.Table.Columns.Contains("Email") ? dr["Email"].ToString() : null,
                OwnerFullName = dr.Table.Columns.Contains("FullName") ? dr["FullName"].ToString() : null
            };
        }
    }
}
