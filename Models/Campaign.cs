using System;

namespace EcoRecycle.Models
{
    public class Campaign
    {
        public int CampaignID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal TargetGoal { get; set; }
        public decimal CurrentProgress { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Extracted / Joined fields
        public int MemberCount { get; set; }
        public bool UserJoined { get; set; } // Flag to check if current logged-in user joined
    }

    public class CampaignParticipant
    {
        public int ParticipantID { get; set; }
        public int CampaignID { get; set; }
        public int UserID { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
