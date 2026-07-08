using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoRecycle.DAL;
using EcoRecycle.Models;
using EcoRecycle.Models.ViewModels;

namespace EcoRecycle.Controllers
{
    [Authorize(Roles = "RecyclingStore")]
    public class StoreController : Controller
    {
        private readonly UserDAL _userDal;
        private readonly PickupDAL _pickupDal;
        private readonly ContentDAL _contentDal;
        private readonly NotificationDAL _notificationDal;

        public StoreController(
            UserDAL userDal,
            PickupDAL pickupDal,
            ContentDAL contentDal,
            NotificationDAL notificationDal)
        {
            _userDal = userDal;
            _pickupDal = pickupDal;
            _contentDal = contentDal;
            _notificationDal = notificationDal;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private Store GetCurrentStore()
        {
            var user = _userDal.GetUserById(CurrentUserId);
            if (user != null && user.RoleName == "RecyclingStore")
            {
                return new Store
                {
                    StoreID = user.StoreID,
                    StoreName = user.StoreName,
                    IsApproved = user.IsApproved
                };
            }
            return null;
        }

        public IActionResult Index()
        {
            var store = GetCurrentStore();
            if (store == null) return RedirectToAction("Login", "Account");

            var vm = _contentDal.GetStoreDashboardStats(store.StoreID, store.StoreName, store.IsApproved);
            
            // Get active assigned pickups (exclude completed or rejected ones from dashboard list)
            var pickups = _pickupDal.GetPickupRequestsByStore(store.StoreID)
                .FindAll(p => p.Status == "Pending" || p.Status == "Scheduled" || p.Status == "Accepted");
            ViewBag.RecentPickups = pickups.Count > 5 ? pickups.GetRange(0, 5) : pickups;

            return View(vm);
        }

        [HttpGet]
        public IActionResult Materials()
        {
            var list = _pickupDal.GetWasteCategories();
            return View(list);
        }

        [HttpGet]
        public IActionResult Queue()
        {
            var store = GetCurrentStore();
            if (store == null || !store.IsApproved)
            {
                TempData["ErrorMessage"] = "Your store is pending approval and cannot accept pickups.";
                return RedirectToAction(nameof(Index));
            }

            // Retrieve pending requests assigned to this specific store
            var queue = _pickupDal.GetPickupRequestsByStore(store.StoreID).FindAll(p => p.Status == "Pending");
            return View(queue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptRequest(int id, DateTime? scheduledDate)
        {
            var store = GetCurrentStore();
            if (store == null || !store.IsApproved) return Json(new { success = false, message = "Store unauthorized or pending approval." });

            try
            {
                var finalScheduleDate = scheduledDate ?? DateTime.Now.AddDays(1);
                _pickupDal.UpdatePickupStatus(id, store.StoreID, "Scheduled", finalScheduleDate);
                _contentDal.AddAuditLog(CurrentUserId, "AcceptPickup", "PickupRequests", id, $"Accepted and scheduled for {finalScheduleDate} by store {store.StoreName}");
                
                // Fetch request user ID to notify
                var request = _pickupDal.GetPickupRequestById(id);
                if (request != null)
                {
                    _notificationDal.CreateNotification(request.UserID, $"Your pickup request has been accepted and scheduled for {finalScheduleDate:f} by {store.StoreName}!", "PickupScheduled");
                }

                TempData["SuccessMessage"] = "Pickup request approved and scheduled successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error accepting pickup: " + ex.Message;
                return RedirectToAction(nameof(Queue));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectRequest(int id)
        {
            var store = GetCurrentStore();
            if (store == null || !store.IsApproved) return Json(new { success = false, message = "Store unauthorized or pending approval." });

            try
            {
                _pickupDal.UpdatePickupStatus(id, store.StoreID, "Rejected");
                _contentDal.AddAuditLog(CurrentUserId, "RejectPickup", "PickupRequests", id, $"Rejected by store {store.StoreName}");

                // Notify User
                var request = _pickupDal.GetPickupRequestById(id);
                if (request != null)
                {
                    _notificationDal.CreateNotification(request.UserID, $"Your pickup request was rejected by {store.StoreName}.", "PickupRejected");
                }

                TempData["SuccessMessage"] = "Pickup request rejected successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error rejecting request: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var store = GetCurrentStore();
            if (store == null) return RedirectToAction("Login", "Account");

            var request = _pickupDal.GetPickupRequestById(id);
            if (request == null || request.StoreID != store.StoreID) return NotFound();

            var vm = new StorePickupDetailsViewModel
            {
                Pickup = request,
                Items = _pickupDal.GetPickupItems(id)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SchedulePickup(int pickupId, DateTime scheduledDate)
        {
            var store = GetCurrentStore();
            if (store == null) return Json(new { success = false, message = "Unauthorized" });

            if (scheduledDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Scheduled date and time must be in the future.";
                return RedirectToAction(nameof(Details), new { id = pickupId });
            }

            try
            {
                _pickupDal.UpdatePickupStatus(pickupId, store.StoreID, "Scheduled", scheduledDate);
                _contentDal.AddAuditLog(CurrentUserId, "SchedulePickup", "PickupRequests", pickupId, $"Scheduled for {scheduledDate}");

                // Notify User
                var request = _pickupDal.GetPickupRequestById(pickupId);
                if (request != null)
                {
                    _notificationDal.CreateNotification(request.UserID, $"Your pickup request has been scheduled for {scheduledDate:f} by {store.StoreName}!", "PickupScheduled");
                }

                TempData["SuccessMessage"] = "Pickup scheduled successfully!";
                return RedirectToAction(nameof(Details), new { id = pickupId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error scheduling pickup: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id = pickupId });
            }
        }

        [HttpGet]
        public IActionResult Complete(int id)
        {
            var store = GetCurrentStore();
            if (store == null) return RedirectToAction("Login", "Account");

            var request = _pickupDal.GetPickupRequestById(id);
            if (request == null || request.StoreID != store.StoreID) return NotFound();

            var vm = new CompletePickupViewModel
            {
                PickupID = request.PickupID,
                UserFullName = request.UserFullName,
                Address = request.Address
            };

            var items = _pickupDal.GetPickupItems(id);
            foreach (var item in items)
            {
                vm.Items.Add(new PickupItemActualInput
                {
                    ItemID = item.ItemID,
                    CategoryID = item.CategoryID,
                    CategoryName = item.CategoryName,
                    PointsPerKg = item.PointsPerKg,
                    ActualWeight = item.EstimatedWeight ?? 0 // Default actual weight to estimated weight
                });
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Complete(CompletePickupViewModel model)
        {
            var store = GetCurrentStore();
            if (store == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    _pickupDal.CompletePickup(model.PickupID, model.Items);
                    _contentDal.AddAuditLog(CurrentUserId, "CompletePickup", "PickupRequests", model.PickupID, "Completed pickup logistics and calculated rewards");

                    // Notify User
                    var request = _pickupDal.GetPickupRequestById(model.PickupID);
                    if (request != null)
                    {
                        _notificationDal.CreateNotification(request.UserID, $"Your pickup has been processed! Check your profile for earned reward points.", "PickupCompleted");
                    }

                    TempData["SuccessMessage"] = "Pickup completed and points credited to the user!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error completing pickup: " + ex.Message);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult History()
        {
            var store = GetCurrentStore();
            if (store == null) return RedirectToAction("Login", "Account");

            var list = _pickupDal.GetPickupRequestsByStore(store.StoreID);
            var completed = new List<PickupRequest>();
            decimal totalWeight = 0;
            int totalEarned = 0;

            foreach (var p in list)
            {
                if (p.Status == "Completed")
                {
                    completed.Add(p);
                    totalWeight += p.TotalWeight ?? 0;
                    totalEarned += p.TotalPoints ?? 0;
                }
            }

            ViewBag.TotalWeight = totalWeight;
            ViewBag.TotalEarned = totalEarned;

            return View(completed);
        }
    }
}
