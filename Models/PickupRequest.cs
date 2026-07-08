using System;
using System.Collections.Generic;

namespace EcoRecycle.Models
{
    public class PickupRequest
    {
        public int PickupID { get; set; }
        public int UserID { get; set; }
        public int? StoreID { get; set; }
        public string Address { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string Status { get; set; } // Pending, Accepted, Rejected, Scheduled, Completed
        public decimal? TotalWeight { get; set; }
        public int? TotalPoints { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Extracted / Joined fields
        public string StoreName { get; set; }
        public string StoreContact { get; set; }
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }

        public List<PickupItem> Items { get; set; } = new List<PickupItem>();
    }

    public class PickupItem
    {
        public int ItemID { get; set; }
        public int PickupID { get; set; }
        public int CategoryID { get; set; }
        public decimal? EstimatedWeight { get; set; }
        public decimal? ActualWeight { get; set; }

        // Joined fields
        public string CategoryName { get; set; }
        public int PointsPerKg { get; set; }
    }

    public class WasteCategory
    {
        public int CategoryID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PointsPerKg { get; set; }
        public string IconUrl { get; set; }
        public bool IsRecyclable { get; set; }
    }
}
