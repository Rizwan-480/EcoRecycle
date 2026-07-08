using System;

namespace EcoRecycle.Models
{
    public class News
    {
        public int NewsID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EcoTip
    {
        public int TipID { get; set; }
        public string Title { get; set; }
        public string TipContent { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Badge
    {
        public int BadgeID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public int ThresholdPoints { get; set; }
    }

    public class LeaderboardRow
    {
        public int Rank { get; set; }
        public int TotalPoints { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public string Username { get; set; }
    }

    public class AuditLog
    {
        public int LogID { get; set; }
        public int? UserID { get; set; }
        public string UserFullName { get; set; }
        public string Action { get; set; }
        public string TableName { get; set; }
        public int? RecordID { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SystemSetting
    {
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string Description { get; set; }
    }
}
