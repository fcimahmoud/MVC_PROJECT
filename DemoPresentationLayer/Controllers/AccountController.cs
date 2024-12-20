﻿
using NuGet.Common;

namespace DemoPresentationLayer.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
		public AccountController(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
		}

		public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if(!ModelState.IsValid) return View(model);
            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.UserName,
                Email = model.Email
            };

            var result = _userManager.CreateAsync(user, model.Password).Result;
            if (result.Succeeded)
                return RedirectToAction(nameof(Login));
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
		public IActionResult Login(LoginViewModel model)
		{
            // 1. Server Side Validation
			if(!ModelState.IsValid) return View(model);

            // 2. Check If User Exist
            var user = _userManager.FindByEmailAsync(model.Email).Result;
            if(user is not null)
            {
                // 3. Check Password Correct
                if (_userManager.CheckPasswordAsync(user, model.Password).Result)
                {
                    // 4. Login If Password is Correct
                    var result = _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false).Result;
                    if (result.Succeeded)
                        return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
                }
            }

			ModelState.AddModelError(string.Empty, "InCorrect Email Or Password");
			return View(model);
		}
        public new IActionResult SignOut()
        {
            _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
		public IActionResult ForgetPassword()
		{
			return View();
		}
        [HttpPost]
        public IActionResult ForgetPassword(ForgetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = _userManager.FindByEmailAsync(model.Email).Result;
            if(user is not null)
            {
                // Create Reset Password Token
                var token = _userManager.GeneratePasswordResetTokenAsync(user).Result;
                // Create Url to Reset Password
                var url = Url.Action(nameof(ResetPassword),
                    nameof(AccountController).Replace("Controller", string.Empty), 
                    new {email = model.Email, Token = token}, Request.Scheme);
                // Create Email Object 
                var email = new Email
                {
                    Subject = "Reset Password",
                    Body = url!,
                    Recipient = model.Email
                };
                // ToDo
                // Send Email
                MailSettings.SendEmail(email);
                // Redirect to check Ur Inbox
                return RedirectToAction(nameof(CheckYourInBox)); 
            }

            ModelState.AddModelError(string.Empty, "User Not Found!");
            return View(model);
        }
        public IActionResult CheckYourInBox()
        {
            return View();
        }

		public IActionResult ResetPassword(string email , string token)
        {
			if (email == null || token == null) return BadRequest();
            TempData["Email"] = email;
            TempData["Token"] = token;
            return View();
        }
        [HttpPost]
		public IActionResult ResetPassword(ResetPasswordViewModel model)
		{
            model.Email = TempData["Email"]?.ToString() ?? string.Empty;
			model.Token = TempData["Token"]?.ToString() ?? string.Empty;
			if (!ModelState.IsValid) return View(model);

            var user = _userManager.FindByEmailAsync(model.Email).Result;
            if (user != null)
            {
                var result = _userManager.ResetPasswordAsync(user, model.Token, model.Password).Result;
                if (result.Succeeded)
                    return RedirectToAction(nameof(Login));
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            ModelState.AddModelError(string.Empty, "User Not Found");
			TempData["Email"] = model.Email;
			TempData["Token"] = model.Token;
			return View(model);
		}
	}
}
