using System;

namespace EcoRecycle.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsBlocked { get; set; }
        public int RewardPoints { get; set; }
        public string AvatarUrl { get; set; }

        // Store details if applicable (joined from Stores table)
        public int StoreID { get; set; }
        public string StoreName { get; set; }
        public string StoreAddress { get; set; }
        public decimal? StoreLatitude { get; set; }
        public decimal? StoreLongitude { get; set; }
        public string StoreOperatingHours { get; set; }
        public string StoreContactNumber { get; set; }
        public bool IsApproved { get; set; }
    }
}
