using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcoRecycle.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,}$", 
            ErrorMessage = "Password must be at least 6 characters and contain at least one uppercase letter, one lowercase letter, one digit, and one special character (e.g. @, #, $, %).")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        public string? FullName { get; set; }

        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [Required(ErrorMessage = "Please select a Role")]
        public string? RoleName { get; set; } // User or RecyclingStore

        // Recycling Store specific fields (conditional in views)
        public string? StoreName { get; set; }
        public string? StoreAddress { get; set; }
        public decimal? StoreLatitude { get; set; }
        public decimal? StoreLongitude { get; set; }
        public string? StoreOperatingHours { get; set; }
        public string? StoreContactNumber { get; set; }
    }

    public class ProfileViewModel
    {
        public int UserID { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? RoleName { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        public string FullName { get; set; }

        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? AvatarUrl { get; set; }
        public bool RemoveAvatar { get; set; }
        public int RewardPoints { get; set; }

        // Store details if store role
        public string? StoreName { get; set; }
        public string? StoreAddress { get; set; }
        public decimal? StoreLatitude { get; set; }
        public decimal? StoreLongitude { get; set; }
        public string? StoreOperatingHours { get; set; }
        public string? StoreContactNumber { get; set; }
        public bool IsApproved { get; set; }

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New passwords do not match")]
        public string? ConfirmNewPassword { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New passwords do not match")]
        public string ConfirmNewPassword { get; set; }
    }

    public class PickupItemInput
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public decimal EstimatedWeight { get; set; }
    }

    public class CreatePickupViewModel
    {
        [Required(ErrorMessage = "Pickup address is required")]
        public string Address { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string Notes { get; set; }

        // Estimated weights for categories
        public List<PickupItemInput> CategoryInputs { get; set; } = new List<PickupItemInput>();
    }

    public class StorePickupDetailsViewModel
    {
        public PickupRequest Pickup { get; set; }
        public List<PickupItem> Items { get; set; }
    }

    public class CompletePickupViewModel
    {
        public int PickupID { get; set; }
        public string UserFullName { get; set; }
        public string Address { get; set; }
        public List<PickupItemActualInput> Items { get; set; } = new List<PickupItemActualInput>();
    }

    public class PickupItemActualInput
    {
        public int ItemID { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int PointsPerKg { get; set; }
        public decimal ActualWeight { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStores { get; set; }
        public int PendingStores { get; set; }
        public int TotalPickups { get; set; }
        public int CompletedPickups { get; set; }
        public int PendingPickups { get; set; }

        public List<CategoryWeightStat> CategoryStats { get; set; } = new List<CategoryWeightStat>();
        public List<MonthlyPickupStat> MonthlyStats { get; set; } = new List<MonthlyPickupStat>();
    }

    public class CategoryWeightStat
    {
        public string CategoryName { get; set; }
        public decimal TotalWeight { get; set; }
    }

    public class MonthlyPickupStat
    {
        public string MonthLabel { get; set; }
        public int TotalPickups { get; set; }
        public decimal TotalWeight { get; set; }
    }

    public class StoreDashboardViewModel
    {
        public int StoreID { get; set; }
        public string StoreName { get; set; }
        public bool IsApproved { get; set; }
        public int TotalAssigned { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalScheduled { get; set; }
        public decimal TotalWeightCollected { get; set; }

        public List<CategoryWeightStat> CategoryStats { get; set; } = new List<CategoryWeightStat>();
    }

    public class VerifyRedemptionViewModel
    {
        public string VerificationCode { get; set; }

        // Redemption transaction loaded after query
        public RewardTransaction Transaction { get; set; }
        public int? VerificationResult { get; set; } // null, 1 = Success, -1 = Not found, -2 = Already redeemed
        public List<RewardTransaction> PendingTransactions { get; set; } = new List<RewardTransaction>();
    }

    public class ThrowWasteViewModel
    {
        [Required(ErrorMessage = "Please select a waste category")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Please enter estimated weight")]
        [Range(0.01, 1000.00, ErrorMessage = "Weight must be between 0.01 Kg and 1000 Kg")]
        public decimal EstimatedWeight { get; set; }

        [Required(ErrorMessage = "Please select a nearby recycling store")]
        public int StoreID { get; set; }

        [Required(ErrorMessage = "Pickup collection address is required")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        public decimal Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        public decimal Longitude { get; set; }

        public string? Notes { get; set; }
    }
}
