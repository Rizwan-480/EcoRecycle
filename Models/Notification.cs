using System;

namespace EcoRecycle.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // PickupAccepted, PickupRejected, PickupScheduled, PickupCompleted, RewardEarned, CampaignStarted, System
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
