using System;

namespace EcoRecycle.Models
{
    public class Reward
    {
        public int RewardID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PointsCost { get; set; }
        public int StockCount { get; set; }
        public bool IsActive { get; set; }
        public string ImageUrl { get; set; }
    }

    public class RewardTransaction
    {
        public int TransactionID { get; set; }
        public int UserID { get; set; }
        public int RewardID { get; set; }
        public DateTime RedeemDate { get; set; }
        public int PointsSpent { get; set; }
        public string Status { get; set; } // Pending, Redeemed
        public string VerificationCode { get; set; }

        // Extracted / Joined fields
        public string Username { get; set; }
        public string UserFullName { get; set; }
        public string RewardName { get; set; }
    }
}
