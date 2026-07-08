namespace EcoRecycle.Models
{
    public class Store
    {
        public int StoreID { get; set; }
        public int UserID { get; set; }
        public string StoreName { get; set; }
        public string StoreAddress { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsApproved { get; set; }
        public string OperatingHours { get; set; }
        public string ContactNumber { get; set; }

        // Owner details populated by join queries
        public string OwnerEmail { get; set; }
        public string OwnerFullName { get; set; }
    }
}
