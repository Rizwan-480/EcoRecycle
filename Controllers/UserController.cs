using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EcoRecycle.DAL;
using EcoRecycle.Models;
using EcoRecycle.Models.ViewModels;
using EcoRecycle.Services;

namespace EcoRecycle.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly UserDAL _userDal;
        private readonly PickupDAL _pickupDal;
        private readonly RewardDAL _rewardDal;
        private readonly CampaignDAL _campaignDal;
        private readonly NotificationDAL _notificationDal;
        private readonly ContentDAL _contentDal;
        private readonly WasteClassifierService _classifier;

        public UserController(
            UserDAL userDal,
            PickupDAL pickupDal,
            RewardDAL rewardDal,
            CampaignDAL campaignDal,
            NotificationDAL notificationDal,
            ContentDAL contentDal,
            WasteClassifierService classifier)
        {
            _userDal = userDal;
            _pickupDal = pickupDal;
            _rewardDal = rewardDal;
            _campaignDal = campaignDal;
            _notificationDal = notificationDal;
            _contentDal = contentDal;
            _classifier = classifier;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        public IActionResult Index()
        {
            var user = _userDal.GetUserById(CurrentUserId);
            if (user == null) return NotFound();

            var pickups = _pickupDal.GetPickupRequestsByUser(CurrentUserId);
            var earnedBadges = _contentDal.GetEarnedBadges(user.RewardPoints);
            var notifications = _notificationDal.GetNotifications(CurrentUserId);

            ViewBag.User = user;
            ViewBag.RecentPickups = pickups.Count > 5 ? pickups.GetRange(0, 5) : pickups;
            ViewBag.Badges = earnedBadges;
            ViewBag.Notifications = notifications.Count > 5 ? notifications.GetRange(0, 5) : notifications;
            
            // Statistics for user widgets
            int completedCount = 0;
            decimal totalWeight = 0;
            foreach (var p in pickups)
            {
                if (p.Status == "Completed")
                {
                    completedCount++;
                    totalWeight += p.TotalWeight ?? 0;
                }
            }
            ViewBag.CompletedCount = completedCount;
            ViewBag.TotalWeight = totalWeight;

            return View();
        }

        [HttpGet]
        public IActionResult ThrowWaste()
        {
            var categories = _pickupDal.GetWasteCategories();
            var stores = _userDal.GetApprovedStores();
            var user = _userDal.GetUserById(CurrentUserId);

            ViewBag.Categories = categories;
            ViewBag.Stores = stores;
            ViewBag.User = user;

            var model = new ThrowWasteViewModel
            {
                Address = user?.Address ?? "",
                Latitude = user?.Latitude ?? (decimal)17.385044, // Default Hyderabad coordinates
                Longitude = user?.Longitude ?? (decimal)78.486671
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThrowWaste(ThrowWasteViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var items = new List<PickupItemInput>
                    {
                        new PickupItemInput
                        {
                            CategoryID = model.CategoryID,
                            EstimatedWeight = model.EstimatedWeight
                        }
                    };

                    int pickupId = _pickupDal.CreatePickupRequest(CurrentUserId, model.Address, model.Latitude, model.Longitude, model.Notes, model.StoreID, items);
                    _contentDal.AddAuditLog(CurrentUserId, "CreatePickup", "PickupRequests", pickupId, $"Requested pickup from store ID {model.StoreID}");

                    // Notify Store
                    var store = _userDal.GetApprovedStores().Find(s => s.StoreID == model.StoreID);
                    if (store != null)
                    {
                        _notificationDal.CreateNotification(store.UserID, $"New pickup request created by client {User.Identity.Name}!", "NewPickupRequest");
                    }

                    TempData["SuccessMessage"] = "Waste request submitted successfully to the selected recycling store!";
                    return RedirectToAction(nameof(ActiveRequests));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating request: " + ex.Message);
                }
            }

            ViewBag.Categories = _pickupDal.GetWasteCategories();
            ViewBag.Stores = _userDal.GetApprovedStores();
            ViewBag.User = _userDal.GetUserById(CurrentUserId);
            return View(model);
        }

        [HttpGet]
        public IActionResult Map()
        {
            var stores = _userDal.GetApprovedStores();
            var user = _userDal.GetUserById(CurrentUserId);
            
            ViewBag.UserLat = user?.Latitude ?? (decimal)17.385044; // Default Hyderabad lat
            ViewBag.UserLng = user?.Longitude ?? (decimal)78.486671; // Default Hyderabad lng
            ViewBag.Stores = stores;

            return View();
        }

        [HttpGet]
        public IActionResult Rewards()
        {
            var user = _userDal.GetUserById(CurrentUserId);
            ViewBag.UserPoints = user?.RewardPoints ?? 0;

            var rewards = _rewardDal.GetRewards();
            var redemptions = _rewardDal.GetUserRedemptions(CurrentUserId);

            ViewBag.Rewards = rewards;
            ViewBag.Redemptions = redemptions;

            return View();
        }

        [HttpPost]
        public IActionResult RedeemReward(int rewardId)
        {
            var reward = _rewardDal.GetRewardById(rewardId);
            if (reward == null) return Json(new { success = false, message = "Reward not found." });

            string code = "ECO-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            int result = _rewardDal.RedeemReward(CurrentUserId, rewardId, code);

            if (result == 1)
            {
                return Json(new { success = true, code = code, message = $"Successfully redeemed {reward.Name}! Show the QR code or use verification code {code} at any admin center." });
            }
            if (result == -1) return Json(new { success = false, message = "Account blocked." });
            if (result == -2) return Json(new { success = false, message = "Insufficient reward points." });
            if (result == -3) return Json(new { success = false, message = "Item is currently out of stock." });

            return Json(new { success = false, message = "An unexpected error occurred during redemption." });
        }

        [HttpGet]
        public IActionResult ActiveRequests()
        {
            var all = _pickupDal.GetPickupRequestsByUser(CurrentUserId);
            var active = all.FindAll(p => p.Status == "Pending" || p.Status == "Scheduled");
            return View(active);
        }

        [HttpGet]
        public IActionResult History()
        {
            var all = _pickupDal.GetPickupRequestsByUser(CurrentUserId);
            var history = all.FindAll(p => p.Status == "Completed" || p.Status == "Cancelled");
            
            decimal totalWeight = 0;
            int totalPoints = 0;
            foreach (var p in history)
            {
                if (p.Status == "Completed")
                {
                    totalWeight += p.TotalWeight ?? 0;
                    totalPoints += p.TotalPoints ?? 0;
                }
            }

            ViewBag.TotalWeight = totalWeight;
            ViewBag.TotalPoints = totalPoints;

            return View(history);
        }

        [HttpGet]
        public IActionResult Campaigns()
        {
            var list = _campaignDal.GetCampaigns(CurrentUserId);
            return View(list);
        }

        [HttpPost]
        public IActionResult JoinCampaign(int campaignId)
        {
            int result = _campaignDal.JoinCampaign(campaignId, CurrentUserId);
            if (result == 1)
            {
                _contentDal.AddAuditLog(CurrentUserId, "JoinCampaign", "Campaigns", campaignId, "Joined environmental campaign");
                return Json(new { success = true, message = "Successfully joined campaign! Your recycling contributions will now be tracked." });
            }
            if (result == -1) return Json(new { success = false, message = "You have already joined this campaign." });
            if (result == -2) return Json(new { success = false, message = "This campaign has ended or is inactive." });

            return Json(new { success = false, message = "Error joining campaign." });
        }

        [HttpGet]
        public IActionResult Notifications()
        {
            var list = _notificationDal.GetNotifications(CurrentUserId);
            _notificationDal.MarkNotificationsRead(CurrentUserId); // Mark as read on load
            return View(list);
        }

        [HttpPost]
        public IActionResult MarkNotificationsRead()
        {
            _notificationDal.MarkNotificationsRead(CurrentUserId);
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetNotificationCount()
        {
            int count = _notificationDal.GetUnreadCount(CurrentUserId);
            return Json(new { count = count });
        }



        [HttpPost]
        public async Task<IActionResult> IdentifyWaste(IFormFile wasteImage)
        {
            if (wasteImage == null || wasteImage.Length == 0)
            {
                return Json(new { success = false, message = "No image file uploaded." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(wasteImage.FileName).ToLower();

            if (!Array.Exists(allowedExtensions, ext => ext == extension))
            {
                return Json(new { success = false, message = "Invalid file type. Only JPG, JPEG, and PNG images are supported." });
            }

            if (wasteImage.Length > 5 * 1024 * 1024) // 5MB
            {
                return Json(new { success = false, message = "Image size cannot exceed 5MB." });
            }

            try
            {
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "waste");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string uniqueName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(uploadFolder, uniqueName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await wasteImage.CopyToAsync(fileStream);
                }

                // Call Classifier
                var classification = await _classifier.ClassifyImageAsync(filePath);

                return Json(new {
                    success = true,
                    imageUrl = "/uploads/waste/" + uniqueName,
                    wasteName = classification.WasteName,
                    category = classification.Category,
                    confidence = classification.ConfidenceScore,
                    recyclable = classification.IsRecyclable
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error identifying image: " + ex.Message });
            }
        }
    }
}
