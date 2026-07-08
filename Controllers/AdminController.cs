using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoRecycle.DAL;
using EcoRecycle.Models;
using EcoRecycle.Models.ViewModels;

namespace EcoRecycle.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserDAL _userDal;
        private readonly PickupDAL _pickupDal;
        private readonly RewardDAL _rewardDal;
        private readonly CampaignDAL _campaignDal;
        private readonly ContentDAL _contentDal;
        private readonly NotificationDAL _notificationDal;

        public AdminController(
            UserDAL userDal,
            PickupDAL pickupDal,
            RewardDAL rewardDal,
            CampaignDAL campaignDal,
            ContentDAL contentDal,
            NotificationDAL notificationDal)
        {
            _userDal = userDal;
            _pickupDal = pickupDal;
            _rewardDal = rewardDal;
            _campaignDal = campaignDal;
            _contentDal = contentDal;
            _notificationDal = notificationDal;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        public IActionResult Index()
        {
            var vm = _contentDal.GetAdminDashboardStats();
            return View(vm);
        }

        [HttpGet]
        public IActionResult Users()
        {
            var list = _userDal.GetAllUsers();
            return View(list);
        }

        [HttpPost]
        public IActionResult ToggleBlockUser(int id, bool isBlocked)
        {
            try
            {
                _userDal.BlockUser(id, isBlocked);
                string act = isBlocked ? "BlockUser" : "UnblockUser";
                _contentDal.AddAuditLog(CurrentUserId, act, "Users", id, $"Modified blocked status of User ID {id} to {isBlocked}");
                return Json(new { success = true, message = $"User status updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Stores()
        {
            var pending = _userDal.GetPendingStores();
            var approved = _userDal.GetApprovedStores();

            ViewBag.Pending = pending;
            ViewBag.Approved = approved;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveStore(int storeId, bool approve)
        {
            try
            {
                _userDal.ApproveStore(storeId, approve);
                string act = approve ? "ApproveStore" : "RejectStore";
                _contentDal.AddAuditLog(CurrentUserId, act, "Stores", storeId, $"Store Approval outcome: {approve}");

                TempData["SuccessMessage"] = approve ? "Store approved and enabled." : "Store registration rejected.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating store status: " + ex.Message;
            }
            return RedirectToAction(nameof(Stores));
        }

        [HttpGet]
        public IActionResult Categories()
        {
            var list = _pickupDal.GetWasteCategories();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveCategory(int categoryId, string name, string description, int pointsPerKg, string iconUrl, bool isRecyclable)
        {
            if (string.IsNullOrEmpty(name))
            {
                TempData["ErrorMessage"] = "Category name is required.";
                return RedirectToAction(nameof(Categories));
            }

            try
            {
                _pickupDal.SaveWasteCategory(categoryId, name, description, pointsPerKg, iconUrl, isRecyclable);
                _contentDal.AddAuditLog(CurrentUserId, "SaveCategory", "WasteCategories", categoryId, $"Saved category: {name}");

                TempData["SuccessMessage"] = "Waste category saved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving category: " + ex.Message;
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public IActionResult Campaigns()
        {
            var list = _campaignDal.GetCampaigns();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveCampaign(int campaignId, string name, string description, decimal targetGoal, DateTime startDate, DateTime endDate, bool isActive)
        {
            if (string.IsNullOrEmpty(name) || targetGoal <= 0 || startDate >= endDate)
            {
                TempData["ErrorMessage"] = "Invalid inputs. Check name, targets, and dates.";
                return RedirectToAction(nameof(Campaigns));
            }

            try
            {
                _campaignDal.SaveCampaign(campaignId, name, description, targetGoal, startDate, endDate, isActive);
                _contentDal.AddAuditLog(CurrentUserId, "SaveCampaign", "Campaigns", campaignId, $"Saved campaign: {name}");

                // Notify all users if it's a new active campaign
                if (campaignId == 0 && isActive)
                {
                    var users = _userDal.GetAllUsers();
                    foreach (var u in users)
                    {
                        if (u.RoleName == "User")
                        {
                            _notificationDal.CreateNotification(u.UserID, $"New Eco Campaign launched: {name}! Join now to contribute.", "CampaignStarted");
                        }
                    }
                }

                TempData["SuccessMessage"] = "Environmental campaign saved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving campaign: " + ex.Message;
            }
            return RedirectToAction(nameof(Campaigns));
        }

        [HttpGet]
        public IActionResult Rewards()
        {
            var list = _rewardDal.GetRewards();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveReward(int rewardId, string name, string description, int pointsCost, int stockCount, bool isActive, string imageUrl)
        {
            if (string.IsNullOrEmpty(name) || pointsCost <= 0 || stockCount < 0)
            {
                TempData["ErrorMessage"] = "Invalid reward inputs. Cost must be > 0.";
                return RedirectToAction(nameof(Rewards));
            }

            try
            {
                _rewardDal.SaveReward(rewardId, name, description, pointsCost, stockCount, isActive, imageUrl);
                _contentDal.AddAuditLog(CurrentUserId, "SaveReward", "Rewards", rewardId, $"Saved reward: {name}");

                TempData["SuccessMessage"] = "Reward catalog item saved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving reward: " + ex.Message;
            }
            return RedirectToAction(nameof(Rewards));
        }

        [HttpGet]
        public IActionResult Verification()
        {
            var model = new VerifyRedemptionViewModel();
            model.PendingTransactions = _rewardDal.GetPendingTransactions();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SearchVerification(VerifyRedemptionViewModel model)
        {
            model.PendingTransactions = _rewardDal.GetPendingTransactions();

            if (string.IsNullOrEmpty(model.VerificationCode))
            {
                ModelState.AddModelError("VerificationCode", "Code is required.");
                return View("Verification", model);
            }

            var tx = _rewardDal.GetTransactionByCode(model.VerificationCode.Trim());
            if (tx == null)
            {
                model.VerificationResult = -1; // Not found
            }
            else
            {
                model.Transaction = tx;
                if (tx.Status == "Redeemed")
                {
                    model.VerificationResult = -2; // Already redeemed
                }
            }
            return View("Verification", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmVerification(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "Verification code is empty.";
                return RedirectToAction(nameof(Verification));
            }

            try
            {
                int res = _rewardDal.VerifyQRCode(code);
                if (res == 1)
                {
                    _contentDal.AddAuditLog(CurrentUserId, "VerifyQR", "RewardTransactions", 0, $"Verified code {code}");
                    TempData["SuccessMessage"] = "QR Code / Verification Code successfully confirmed! Reward item may be handed over to the user.";
                }
                else if (res == -2)
                {
                    TempData["ErrorMessage"] = "This code has already been verified and claimed.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid code.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Verification error: " + ex.Message;
            }

            return RedirectToAction(nameof(Verification));
        }

        [HttpGet]
        public IActionResult Settings()
        {
            var settings = _contentDal.GetSettings();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(string googleMapsKey, string huggingFaceKey, bool localMapsFallback)
        {
            try
            {
                _contentDal.UpdateSetting("GoogleMapsApiKey", googleMapsKey ?? "");
                _contentDal.UpdateSetting("HuggingFaceApiKey", huggingFaceKey ?? "");
                _contentDal.UpdateSetting("LocalMapsFallback", localMapsFallback ? "True" : "False");

                _contentDal.AddAuditLog(CurrentUserId, "UpdateSettings", "Settings", 0, "Updated system API configuration keys");

                TempData["SuccessMessage"] = "System settings updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating settings: " + ex.Message;
            }
            return RedirectToAction(nameof(Settings));
        }

        [HttpGet]
        public IActionResult AuditLogs()
        {
            var logs = _contentDal.GetAuditLogs();
            return View(logs);
        }

        [HttpGet]
        public IActionResult Reports()
        {
            var vm = _contentDal.GetAdminDashboardStats();
            return View(vm);
        }
    }
}
