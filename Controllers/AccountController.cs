using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EcoRecycle.DAL;
using EcoRecycle.Models;
using EcoRecycle.Models.ViewModels;
using EcoRecycle.Services;

namespace EcoRecycle.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserDAL _userDal;
        private readonly ContentDAL _contentDal;

        public AccountController(UserDAL userDal, ContentDAL contentDal)
        {
            _userDal = userDal;
            _contentDal = contentDal;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToDashboard(User.FindFirst(ClaimTypes.Role)?.Value);
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = _userDal.GetUserByEmail(model.Email);
                if (user != null)
                {
                    if (user.IsBlocked)
                    {
                        ModelState.AddModelError("", "Your account has been suspended. Please contact support.");
                        return View(model);
                    }

                    if (PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
                    {
                        // Check if recycling store is approved
                        if (user.RoleName == "RecyclingStore")
                        {
                            var storeUser = _userDal.GetUserById(user.UserID);
                            if (storeUser != null && !storeUser.IsApproved)
                            {
                                ModelState.AddModelError("", "Your store account registration is pending administrator approval.");
                                return View(model);
                            }
                        }

                        // Create Claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.Role, user.RoleName),
                            new Claim("FullName", user.FullName),
                            new Claim("AvatarUrl", user.AvatarUrl ?? "/images/default-avatar.png")
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                        
                        _contentDal.AddAuditLog(user.UserID, "Login", "Users", user.UserID, "Successfully logged in");

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToDashboard(user.RoleName);
                    }
                }
                ModelState.AddModelError("", "Invalid email or password.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToDashboard(User.FindFirst(ClaimTypes.Role)?.Value);
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (model.RoleName == "RecyclingStore")
            {
                if (string.IsNullOrWhiteSpace(model.StoreName))
                {
                    ModelState.AddModelError("StoreName", "Store Name is required for store owners.");
                }
                if (string.IsNullOrWhiteSpace(model.StoreAddress))
                {
                    ModelState.AddModelError("StoreAddress", "Store Address is required for store owners.");
                }
            }
            else if (model.RoleName == "User")
            {
                if (string.IsNullOrWhiteSpace(model.Address))
                {
                    ModelState.AddModelError("Address", "Home Address is required.");
                }
            }

            if (ModelState.IsValid)
            {
                // Check if user already exists
                var existingUser = _userDal.GetUserByEmail(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email address is already in use.");
                    return View(model);
                }

                // Get role ID
                int roleId = 3; // Default to User
                if (model.RoleName == "RecyclingStore") roleId = 2;
                else if (model.RoleName == "Admin") roleId = 1; // Safeguard if seeded admin needed

                string passHash = PasswordHasher.HashPassword(model.Password);
                
                try
                {
                    int userId = _userDal.RegisterUser(model, passHash, roleId);
                    
                    _contentDal.AddAuditLog(userId, "Register", "Users", userId, $"Registered as {model.RoleName}");

                    if (model.RoleName == "RecyclingStore")
                    {
                        TempData["SuccessMessage"] = "Store registration submitted successfully! Please wait for administrator approval before logging in.";
                        return RedirectToAction(nameof(Login));
                    }

                    TempData["SuccessMessage"] = "Registration successful! You can now log in.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred during registration: " + ex.Message);
                }
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            _contentDal.AddAuditLog(userId, "Logout", "Users", userId, "User logged out");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            var user = _userDal.GetUserById(userId);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                UserID = user.UserID,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Address = user.Address,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
                AvatarUrl = user.AvatarUrl ?? "/images/default-avatar.png",
                RewardPoints = user.RewardPoints,
                RoleName = user.RoleName
            };

            if (user.RoleName == "RecyclingStore")
            {
                model.StoreName = user.StoreName;
                model.StoreAddress = user.StoreAddress;
                model.StoreLatitude = user.StoreLatitude;
                model.StoreLongitude = user.StoreLongitude;
                model.StoreOperatingHours = user.StoreOperatingHours;
                model.StoreContactNumber = user.StoreContactNumber;
                model.IsApproved = user.IsApproved;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model, IFormFile avatarFile)
        {
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            var user = _userDal.GetUserById(userId);
            if (user == null) return NotFound();

            // Retain view values
            model.UserID = user.UserID;
            model.Username = user.Username;
            model.Email = user.Email;
            model.RoleName = user.RoleName;
            model.RewardPoints = user.RewardPoints;

            if (ModelState.IsValid)
            {
                try
                {
                    string avatarPath = user.AvatarUrl;

                    // Handle Avatar Removal or Upload
                    if (model.RemoveAvatar)
                    {
                        if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/uploads/avatars/"))
                        {
                            try
                            {
                                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarUrl.TrimStart('/'));
                                if (System.IO.File.Exists(fullPath))
                                {
                                    System.IO.File.Delete(fullPath);
                                }
                            }
                            catch { }
                        }
                        avatarPath = null;
                    }
                    else if (avatarFile != null && avatarFile.Length > 0)
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                        var extension = Path.GetExtension(avatarFile.FileName).ToLower();

                        if (!Array.Exists(allowedExtensions, ext => ext == extension))
                        {
                            ModelState.AddModelError("", "Only .jpg, .jpeg, and .png images are allowed.");
                            return View(model);
                        }

                        if (avatarFile.Length > 2 * 1024 * 1024) // 2MB
                        {
                            ModelState.AddModelError("", "Avatar file size cannot exceed 2MB.");
                            return View(model);
                        }

                        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                        if (!Directory.Exists(uploadFolder))
                        {
                            Directory.CreateDirectory(uploadFolder);
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + extension;
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await avatarFile.CopyToAsync(fileStream);
                        }

                        avatarPath = "/uploads/avatars/" + uniqueFileName;
                    }

                    // Update database
                    _userDal.UpdateUserProfile(userId, model.FullName, model.Address, model.Latitude, model.Longitude, avatarPath);

                    // Re-issue cookie authentication claims to update navbar/profile pictures dynamically
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.RoleName),
                        new Claim("FullName", model.FullName),
                        new Claim("AvatarUrl", avatarPath ?? "")
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    _contentDal.AddAuditLog(userId, "UpdateProfile", "Users", userId, "Updated profile information");

                    TempData["SuccessMessage"] = "Profile details updated successfully!";
                    return RedirectToAction(nameof(Profile));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating profile: " + ex.Message);
                }
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            var user = _userDal.GetUserById(userId);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var profileModel = new ProfileViewModel
                {
                    UserID = user.UserID,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Address = user.Address,
                    Latitude = user.Latitude,
                    Longitude = user.Longitude,
                    AvatarUrl = user.AvatarUrl,
                    RewardPoints = user.RewardPoints,
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmNewPassword = model.ConfirmNewPassword
                };
                return View("Profile", profileModel);
            }

            if (!PasswordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                var profileModel = new ProfileViewModel
                {
                    UserID = user.UserID,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Address = user.Address,
                    Latitude = user.Latitude,
                    Longitude = user.Longitude,
                    AvatarUrl = user.AvatarUrl,
                    RewardPoints = user.RewardPoints,
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmNewPassword = model.ConfirmNewPassword
                };
                return View("Profile", profileModel);
            }

            string newHash = PasswordHasher.HashPassword(model.NewPassword);
            _userDal.UpdatePassword(userId, newHash);
            _contentDal.AddAuditLog(userId, "UpdatePassword", "Users", userId, "Changed account password");

            TempData["SuccessMessage"] = "Password updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            var user = _userDal.GetUserByEmail(email);
            if (user != null)
            {
                // Simulate sending reset link
                string resetToken = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
                ViewBag.Success = $"A password reset request was initialized. Your simulated verification token is: {resetToken}. Typically, a link would be sent to {email}.";
                
                _contentDal.AddAuditLog(user.UserID, "ForgotPassword", "Users", user.UserID, "Initialized simulated password reset");
            }
            else
            {
                ViewBag.Error = "No account found associated with this email address.";
            }
            return View();
        }

        private IActionResult RedirectToDashboard(string role)
        {
            if (role == "Admin") return RedirectToAction("Index", "Admin");
            if (role == "RecyclingStore") return RedirectToAction("Index", "Store");
            return RedirectToAction("Index", "User");
        }
    }
}
