using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CarbonZero.Models;

namespace CarbonZero.Controllers;

using Microsoft.AspNetCore.Http;
using CarbonZero.Data;
using CarbonZero.Models;
using Microsoft.EntityFrameworkCore;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public IActionResult SuperAdminDashboard()
    {
        var username = HttpContext.Session.GetString("Username");
        if (username != "superadmin")
        {
            return RedirectToAction("Index");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ClearData()
    {
        var username = HttpContext.Session.GetString("Username");
        if (username != "superadmin")
        {
            return RedirectToAction("Index");
        }

        _context.SessionHistories.RemoveRange(_context.SessionHistories);
        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();

        TempData["ClearDataMessage"] = "All user and history data cleared.";
        return RedirectToAction("SuperAdminDashboard");
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Dashboard()
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("Index");
        }

        var loginTimeString = HttpContext.Session.GetString("LoginTime");
        var logoutTimeString = HttpContext.Session.GetString("LogoutTime");

        DateTime? loginTime = null;
        DateTime? logoutTime = null;

        if (!string.IsNullOrEmpty(loginTimeString))
        {
            loginTime = DateTime.Parse(loginTimeString);
        }
        if (!string.IsNullOrEmpty(logoutTimeString))
        {
            logoutTime = DateTime.Parse(logoutTimeString);
        }

        double hoursActive = 0;
        double energyGenerated = 0;
        double co2Saved = 0;

        var user = _context.Users.FirstOrDefault(u => u.Username == username);
        if (user != null && loginTime.HasValue)
        {
            if (logoutTime.HasValue)
            {
                // User logged out, use last session duration
                hoursActive = (logoutTime.Value - loginTime.Value).TotalHours;
            }
            else
            {
                // User currently logged in, calculate duration till now
                hoursActive = (DateTime.Now - loginTime.Value).TotalHours;
            }
            energyGenerated = user.PowerCapacityKW * hoursActive;
            co2Saved = energyGenerated * 0.417; // in kg
        }

        ViewData["Username"] = username;
        ViewData["HoursActive"] = hoursActive;
        ViewData["EnergyGenerated"] = energyGenerated;
        ViewData["CO2Saved"] = co2Saved;

        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ViewData["ErrorMessage"] = "Username and password are required.";
            return View();
        }

        // Check for superadmin login in database
        var superadminUser = await _context.Users
            .Where(u => u.Username == "superadmin" && u.Password == "SuperSecretPassword123!")
            .SingleOrDefaultAsync();

        if (superadminUser != null && username?.Trim().ToLower() == "superadmin" && password?.Trim() == "SuperSecretPassword123!")
        {
            HttpContext.Session.SetString("Username", "superadmin");
            HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());
            HttpContext.Session.Remove("LogoutTime");
            return RedirectToAction("SuperAdminDashboard");
        }

        var user = await _context.Users
            .Where(u => u.Username == username || u.IdNo == username)
            .SingleOrDefaultAsync();

        if (user == null || user.Password != password)
        {
            ViewData["ErrorMessage"] = "Invalid username or password.";
            return View();
        }

        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());
        HttpContext.Session.Remove("LogoutTime"); // Clear previous logout time if any

        // Create new session history record
        var sessionHistory = new SessionHistory
        {
            Username = user.Username,
            LoginTime = DateTime.Now
        };
        _context.SessionHistories.Add(sessionHistory);
        await _context.SaveChangesAsync();

        return RedirectToAction("Dashboard");
    }

    [HttpGet]
    public IActionResult Signup()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Signup(string username, string idno, string password, double powercapacitykw)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(idno) || string.IsNullOrEmpty(password))
        {
            ViewData["Error"] = "Please fill all fields";
            return View();
        }

        var existingUsername = await _context.Users.AnyAsync(u => u.Username == username);
        if (existingUsername)
        {
            ViewData["Error"] = "Username already exists";
            return View();
        }

        var existingIdNo = await _context.Users.AnyAsync(u => u.IdNo == idno);
        if (existingIdNo)
        {
            ViewData["Error"] = "ID No already exists";
            return View();
        }

        var user = new User
        {
            Username = username,
            IdNo = idno,
            Password = password,
            PowerCapacityKW = powercapacitykw
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        var username = HttpContext.Session.GetString("Username");
        var loginTimeString = HttpContext.Session.GetString("LoginTime");
        DateTime? loginTime = null;
        if (!string.IsNullOrEmpty(loginTimeString))
        {
            loginTime = DateTime.Parse(loginTimeString);
        }

        if (!string.IsNullOrEmpty(username) && loginTime.HasValue)
        {
            var sessionHistory = await _context.SessionHistories
                .Where(sh => sh.Username == username && sh.LoginTime <= loginTime)
                .OrderByDescending(sh => sh.LoginTime)
                .FirstOrDefaultAsync();

            if (sessionHistory != null)
            {
                sessionHistory.LogoutTime = DateTime.Now;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    var hoursActive = (sessionHistory.LogoutTime.Value - sessionHistory.LoginTime).TotalHours;
                    sessionHistory.EnergyGenerated = user.PowerCapacityKW * hoursActive;
                    sessionHistory.CO2Saved = sessionHistory.EnergyGenerated * 0.417;
                }

                await _context.SaveChangesAsync();
            }
        }

        HttpContext.Session.Clear();
        TempData["LogoutMessage"] = "You have successfully logged out.";
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public IActionResult GetLiveStats()
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            return Json(new { energyGenerated = 0, co2Saved = 0, hoursActive = 0 });
        }

        var loginTimeString = HttpContext.Session.GetString("LoginTime");
        DateTime? loginTime = null;
        if (!string.IsNullOrEmpty(loginTimeString))
        {
            loginTime = DateTime.Parse(loginTimeString);
        }

        double energyGenerated = 0;
        double co2Saved = 0;
        double hoursActive = 0;

        var user = _context.Users.FirstOrDefault(u => u.Username == username);
        if (user != null && loginTime.HasValue)
        {
            hoursActive = (DateTime.Now - loginTime.Value).TotalHours;
            energyGenerated = user.PowerCapacityKW * hoursActive;
            co2Saved = energyGenerated * 0.417; // in kg
        }

        return Json(new { energyGenerated, co2Saved, hoursActive });
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("Index");
        }

        var history = await _context.SessionHistories
            .Where(sh => sh.Username == username)
            .OrderByDescending(sh => sh.LoginTime)
            .ToListAsync();

        return View(history);
    }
}
