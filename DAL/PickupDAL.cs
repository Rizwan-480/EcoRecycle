using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EcoRecycle.Models;
using EcoRecycle.Models.ViewModels;

namespace EcoRecycle.DAL
{
    public class PickupDAL
    {
        private readonly DatabaseHelper _db;

        public PickupDAL(DatabaseHelper db)
        {
            _db = db;
        }

        public int CreatePickupRequest(int userId, string address, decimal? lat, decimal? lng, string notes, int? storeId, List<PickupItemInput> items)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId),
                new SqlParameter("@Address", address),
                new SqlParameter("@Latitude", (object)lat ?? DBNull.Value),
                new SqlParameter("@Longitude", (object)lng ?? DBNull.Value),
                new SqlParameter("@Notes", (object)notes ?? DBNull.Value),
                new SqlParameter("@StoreID", (object)storeId ?? DBNull.Value),
                new SqlParameter("@NewPickupID", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };

            _db.ExecuteNonQuery("sp_CreatePickupRequest", parameters);
            int newPickupId = (int)parameters[6].Value;

            // Save items
            foreach (var item in items)
            {
                if (item.EstimatedWeight > 0)
                {
                    SqlParameter[] itemParams = new SqlParameter[]
                    {
                        new SqlParameter("@PickupID", newPickupId),
                        new SqlParameter("@CategoryID", item.CategoryID),
                        new SqlParameter("@EstimatedWeight", item.EstimatedWeight)
                    };
                    _db.ExecuteNonQuery("sp_AddPickupItem", itemParams);
                }
            }

            return newPickupId;
        }

        public List<PickupRequest> GetPickupRequestsByUser(int userId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserID", userId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetPickupRequestsByUser", parameters);
            List<PickupRequest> requests = new List<PickupRequest>();
            foreach (DataRow row in dt.Rows)
            {
                requests.Add(MapRowToPickupRequest(row));
            }
            return requests;
        }

        public List<PickupRequest> GetPickupRequestsByStore(int storeId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@StoreID", storeId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetPickupRequestsByStore", parameters);
            List<PickupRequest> requests = new List<PickupRequest>();
            foreach (DataRow row in dt.Rows)
            {
                requests.Add(MapRowToPickupRequest(row));
            }
            return requests;
        }

        public List<PickupRequest> GetPendingPickupRequests()
        {
            DataTable dt = _db.ExecuteReaderTable("sp_GetPendingPickupRequests");
            List<PickupRequest> requests = new List<PickupRequest>();
            foreach (DataRow row in dt.Rows)
            {
                requests.Add(MapRowToPickupRequest(row));
            }
            return requests;
        }

        public PickupRequest GetPickupRequestById(int pickupId)
        {
            // Create sp_GetPickupRequestById on the fly if it doesn't exist
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetPickupRequestById]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetPickupRequestById
                            @PickupID INT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT p.*, s.StoreName, s.ContactNumber AS StoreContact, u.FullName AS UserFullName, u.Email AS UserEmail
                            FROM PickupRequests p
                            LEFT JOIN Stores s ON p.StoreID = s.StoreID
                            INNER JOIN Users u ON p.UserID = u.UserID
                            WHERE p.PickupID = @PickupID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PickupID", pickupId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetPickupRequestById", parameters);
            if (dt.Rows.Count == 0) return null;

            var request = MapRowToPickupRequest(dt.Rows[0]);
            request.Items = GetPickupItems(pickupId);
            return request;
        }

        public void UpdatePickupStatus(int pickupId, int? storeId, string status, DateTime? scheduledDate = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PickupID", pickupId),
                new SqlParameter("@StoreID", (object)storeId ?? DBNull.Value),
                new SqlParameter("@Status", status),
                new SqlParameter("@ScheduledDate", (object)scheduledDate ?? DBNull.Value)
            };
            _db.ExecuteNonQuery("sp_UpdatePickupStatus", parameters);
        }

        public void CompletePickup(int pickupId, List<PickupItemActualInput> itemInputs)
        {
            decimal totalWeight = 0;
            int totalPoints = 0;

            // Update individual item actual weights
            foreach (var input in itemInputs)
            {
                SqlParameter[] itemParams = new SqlParameter[]
                {
                    new SqlParameter("@ItemID", input.ItemID),
                    new SqlParameter("@ActualWeight", input.ActualWeight)
                };
                _db.ExecuteNonQuery("sp_UpdatePickupItemActualWeight", itemParams);

                totalWeight += input.ActualWeight;
                totalPoints += (int)(input.ActualWeight * input.PointsPerKg);
            }

            // Complete the overall pickup request
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PickupID", pickupId),
                new SqlParameter("@ActualWeight", totalWeight),
                new SqlParameter("@TotalPoints", totalPoints)
            };
            _db.ExecuteNonQuery("sp_CompletePickup", parameters);
        }

        public List<PickupItem> GetPickupItems(int pickupId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PickupID", pickupId)
            };

            DataTable dt = _db.ExecuteReaderTable("sp_GetPickupItems", parameters);
            List<PickupItem> items = new List<PickupItem>();
            foreach (DataRow row in dt.Rows)
            {
                items.Add(new PickupItem
                {
                    ItemID = (int)row["ItemID"],
                    PickupID = (int)row["PickupID"],
                    CategoryID = (int)row["CategoryID"],
                    EstimatedWeight = row["EstimatedWeight"] == DBNull.Value ? null : (decimal?)row["EstimatedWeight"],
                    ActualWeight = row["ActualWeight"] == DBNull.Value ? null : (decimal?)row["ActualWeight"],
                    CategoryName = row["CategoryName"].ToString(),
                    PointsPerKg = (int)row["PointsPerKg"]
                });
            }
            return items;
        }

        public List<WasteCategory> GetWasteCategories()
        {
            // Simple helper to fetch all waste categories
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetWasteCategories]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_GetWasteCategories
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT * FROM WasteCategories;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            DataTable dt = _db.ExecuteReaderTable("sp_GetWasteCategories");
            List<WasteCategory> categories = new List<WasteCategory>();
            foreach (DataRow row in dt.Rows)
            {
                categories.Add(new WasteCategory
                {
                    CategoryID = (int)row["CategoryID"],
                    Name = row["Name"].ToString(),
                    Description = row["Description"] == DBNull.Value ? null : row["Description"].ToString(),
                    PointsPerKg = (int)row["PointsPerKg"],
                    IconUrl = row["IconUrl"] == DBNull.Value ? null : row["IconUrl"].ToString(),
                    IsRecyclable = (bool)row["IsRecyclable"]
                });
            }
            return categories;
        }

        public void SaveWasteCategory(int categoryId, string name, string description, int pointsPerKg, string iconUrl, bool isRecyclable)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string createSp = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_SaveWasteCategory]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC dbo.sp_executesql @statement = N'
                        CREATE PROCEDURE sp_SaveWasteCategory
                            @CategoryID INT,
                            @Name NVARCHAR(50),
                            @Description NVARCHAR(255),
                            @PointsPerKg INT,
                            @IconUrl NVARCHAR(255),
                            @IsRecyclable BIT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            IF @CategoryID = 0
                                INSERT INTO WasteCategories (Name, Description, PointsPerKg, IconUrl, IsRecyclable)
                                VALUES (@Name, @Description, @PointsPerKg, @IconUrl, @IsRecyclable);
                            ELSE
                                UPDATE WasteCategories
                                SET Name = @Name,
                                    Description = @Description,
                                    PointsPerKg = @PointsPerKg,
                                    IconUrl = @IconUrl,
                                    IsRecyclable = @IsRecyclable
                                WHERE CategoryID = @CategoryID;
                        END'
                    END";
                using (var cmd = new SqlCommand(createSp, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CategoryID", categoryId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Description", (object)description ?? DBNull.Value),
                new SqlParameter("@PointsPerKg", pointsPerKg),
                new SqlParameter("@IconUrl", (object)iconUrl ?? DBNull.Value),
                new SqlParameter("@IsRecyclable", isRecyclable)
            };
            _db.ExecuteNonQuery("sp_SaveWasteCategory", parameters);
        }

        private PickupRequest MapRowToPickupRequest(DataRow dr)
        {
            return new PickupRequest
            {
                PickupID = (int)dr["PickupID"],
                UserID = (int)dr["UserID"],
                StoreID = dr["StoreID"] == DBNull.Value ? null : (int?)dr["StoreID"],
                Address = dr["Address"].ToString(),
                ScheduledDate = dr["ScheduledDate"] == DBNull.Value ? null : (DateTime?)dr["ScheduledDate"],
                Status = dr["Status"].ToString(),
                TotalWeight = dr["TotalWeight"] == DBNull.Value ? null : (decimal?)dr["TotalWeight"],
                TotalPoints = dr["TotalPoints"] == DBNull.Value ? null : (int?)dr["TotalPoints"],
                Latitude = dr["Latitude"] == DBNull.Value ? null : (decimal?)dr["Latitude"],
                Longitude = dr["Longitude"] == DBNull.Value ? null : (decimal?)dr["Longitude"],
                Notes = dr["Notes"] == DBNull.Value ? null : dr["Notes"].ToString(),
                CreatedAt = (DateTime)dr["CreatedAt"],
                UpdatedAt = (DateTime)dr["UpdatedAt"],
                StoreName = dr.Table.Columns.Contains("StoreName") && dr["StoreName"] != DBNull.Value ? dr["StoreName"].ToString() : null,
                StoreContact = dr.Table.Columns.Contains("StoreContact") && dr["StoreContact"] != DBNull.Value ? dr["StoreContact"].ToString() : null,
                UserFullName = dr.Table.Columns.Contains("UserFullName") && dr["UserFullName"] != DBNull.Value ? dr["UserFullName"].ToString() : null,
                UserEmail = dr.Table.Columns.Contains("UserEmail") && dr["UserEmail"] != DBNull.Value ? dr["UserEmail"].ToString() : null
            };
        }
    }
}
