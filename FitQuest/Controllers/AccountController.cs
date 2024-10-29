using FitQuest.Data;
using FitQuest.Models;
using FitQuest.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BCrypt.Net; // Add this using directive
using Microsoft.AspNetCore.Http; // Add this using directive

namespace FitQuest.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger, IEmailSender emailSender)
        {
            _context = context;
            _logger = logger;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View("~/Views/Home/Register.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match";
                return View("~/Views/Home/Register.cshtml");
            }

            // Check if the username or email already exists
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ViewBag.Error = "Username already exists";
                return View("~/Views/Home/Register.cshtml");
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ViewBag.Error = "Email already exists";
                return View("~/Views/Home/Register.cshtml");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User { Username = username, Email = email, Password = hashedPassword };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send confirmation email
            var token = "dummy-token"; // Generate a real token in a real application
            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, token }, Request.Scheme);
            await _emailSender.SendEmailAsync(email, "Confirm your email", $"Please confirm your email by clicking <a href=\"{confirmationLink}\">here</a>.");

            return RedirectToAction("RegistrationConfirmation");
        }

        [HttpGet]
        public IActionResult RegistrationConfirmation()
        {
            return View("~/Views/Home/RegistrationConfirmation.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.EmailConfirmed = true;
            await _context.SaveChangesAsync();

            return Ok("Email confirmed successfully!");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View("~/Views/Home/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                // Sign in the user
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                return RedirectToAction("Welcome", "Home");
            }

            ViewBag.Error = "Invalid login credentials";
            return View("~/Views/Home/Login.cshtml");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // Sign out the user
            HttpContext.Session.Remove("UserId");
            return RedirectToAction("Index", "Home");
        }
    }
}