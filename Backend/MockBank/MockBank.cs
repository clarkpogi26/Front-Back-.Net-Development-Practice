using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BankController : ControllerBase
    {
        private static Dictionary<int, User> users = Files.Load().Result;
        [HttpPost("login")]
        public IActionResult Login([FromBody] User user)
        {
            if (String.IsNullOrEmpty(user.Name) || String.IsNullOrEmpty(user.Pass))
                return BadRequest(new { msg = "Empty input" });
            var match = users.Values.FirstOrDefault(k => k.Name == user.Name);
            if (match == null)
                return NotFound(new { msg = "Username doesn't exist" });
            if (!Argon2.Verify(users[GUK(user.Name)].Pass, user.Pass))
                return BadRequest(new { msg = "Wrong password" });
            return Ok(new { user = match, msg = "Login successful" });
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            try
            {
                if (String.IsNullOrEmpty(user.Name) || String.IsNullOrEmpty(user.Pass))
                    return BadRequest(new { msg = "Empty input" });
                if (users.Values.Any(k => k.Name == user.Name))
                    return BadRequest(new { msg = "Name already taken" });
                if (user.Name.Length < 5 || user.Name.Length > 12)
                    return BadRequest(new { msg = "Name limit (5-15) characters" });
                if (user.Pass.Length < 5 || user.Pass.Length > 12)
                    return BadRequest(new { msg = "Pass limit (5-12) characters" });
                if (!user.Pass.Any(k => !Char.IsLetterOrDigit(k)))
                    return BadRequest(new { msg = "Pass must include a special character" });
                int newKey = users.Any() ? users.Keys.Max() + 1 : 1;
                users.Add(newKey, new User
                {
                    Name = user.Name,
                    Pass = Argon2.Hash(user.Pass),
                    Balance = 0,
                    Trans = new List<Transaction>()
                });
                await Files.Save(users);    
                return Ok(new { msg = "Registration Successful" });
            } catch (Exception ex)
            {
                return StatusCode(500, new { msg = $"Internal Server Error: {ex.Message}" });
            }
        }
        [HttpPost("deposit/{name}")]
        public async Task<IActionResult> Deposit(String name, [FromBody] TransRequest req)
        {
            try
            {
                var userKey = GUK(name);
                var user = users[userKey];
                if (!users.Any(k => k.Key == userKey))
                    return NotFound(new { msg = "That key doesn't exist" });
                if (user == null)
                    return NotFound(new { msg = "That account doesn't exist" });
                user.Trans.Add(new Transaction
                {
                    Type = req.Type,
                    Amount = +req.Amount,
                    Date = DateTime.Now,
                    Reference = Guid.NewGuid().ToString()
                });
                user.Balance = Sum(user.Name);
                await Files.Save(users);
                return Ok(new { msg = "Transaction successful", balance = user.Balance });
            } catch (Exception ex)
            {
                return StatusCode(500, new { msg = $"Internal Server Error: {ex.Message}" });
            }
        }
        [HttpPost("withdraw/{name}")]
        public async Task<IActionResult> Withdraw(String name, [FromBody] TransRequest req)
        {
            try
            {
                var userKey = GUK(name);
                var user = users[userKey];
                if (!users.Any(k => k.Key == userKey))
                    return NotFound(new { msg = "That key doesn't exist" });
                if (user == null)
                    return NotFound(new { msg = "That account doesn't exist" });
                if (req.Amount > user.Balance)
                    return BadRequest(new { msg = "Insufficient funds" });
                user.Trans.Add(new Transaction
                {
                    Type = req.Type,
                    Amount = -req.Amount,
                    Date = DateTime.Now,
                    Reference = Guid.NewGuid().ToString()
                });
                user.Balance = Sum(user.Name);
                await Files.Save(users);
                return Ok(new { msg = "Transaction successful", balance = user.Balance });
            } catch (Exception ex)
            {
                return StatusCode(500, new { msg = $"Internal Server Error: {ex.Message}" });
            }
        }
        [HttpPost("transfer/{sender}/{recipient}")]
        public async Task<IActionResult> Transfer(String sender, String recipient, [FromBody] TransRequest req)
        {
            try
            {
                var sKey = GUK(sender);
                var rKey = GUK(recipient);
                var s = users[sKey];
                var r = users[rKey];
                if (!users.Keys.Any(k => k == sKey) || !users.Keys.Any(k => k == rKey))
                    return NotFound(new { msg = "That key doesn't exist" });
                if (s == null || r == null)
                    return NotFound(new { msg = "That account doesn't exist" });
                if (sender == recipient || s == r)
                    return BadRequest(new { msg = "You can't send it to yourself" });
                if (req.Amount > s.Balance)
                    return BadRequest(new { msg = "Insufficient funds" });
                s.Trans.Add(new Transaction
                {
                    To = recipient,
                    Type = req.Type,
                    Amount = -req.Amount,
                    Date = DateTime.Now,
                    Reference = Guid.NewGuid().ToString()
                });
                r.Trans.Add(new Transaction
                {
                    From = sender,
                    Type = req.Type,
                    Amount = +req.Amount,
                    Date = DateTime.Now,
                    Reference = Guid.NewGuid().ToString()
                });
                r.Balance = Sum(r.Name);
                s.Balance = Sum(s.Name);
                await Files.Save(users);
                return Ok(new { msg = "Transfer successful", balance = r.Balance });
            } catch (Exception ex)
            {
                return StatusCode(500, new { msg = $"Internal Server Error: {ex.Message}" });
            }
        }
        [HttpGet("transaction/{name}")]
        public IActionResult Transaction(String name)
        {
            try
            {
                if (!users.Any(k => k.Value.Name == name))
                    return NotFound(new { msg = "Name cannot be found" });
                var userKey = GUK(name);
                if (userKey == null || userKey < 0 || userKey > users.Keys.Count)
                    return NotFound(new { msg = "Key cannot be found" });
                var curUser = users[userKey];
                if (!curUser.Trans.Any())
                    return NotFound(new { msg = "You have no transactions" });
                return Ok(new
                {
                    transaction = curUser.Trans
                    .Where(k => k.deleted == false)
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new { msg = $"Internal Server Error: {ex.Message}" });
            }
        }
        [HttpGet("show")]
        public IActionResult Show() { return Ok(users); }
        [HttpDelete("clear")]
        public async Task<IActionResult> Clear() { users.Clear(); await Files.Save(users); return Ok(new { msg = "Accounts cleared" }); }
        [HttpDelete("transremove/{x}/{refe}")]
        public async Task<IActionResult> TransRemove(String x, String refe)
        {
            try
            {
                if (String.IsNullOrEmpty(x) || !users.Any(k => k.Value.Name == x))
                    return NotFound(new { msg = "That username doesn't exist"});
                var tran = users[GUK(x)].Trans.FirstOrDefault(k => k.Reference == refe);
                if (tran == null)
                    return NotFound(new { msg = "That transaction doesn't exist" });
                tran.deleted = true;
                await Files.Save(users);
                return Ok(new { msg = "Transactions successfully cleared" });
            } catch (Exception ex)
            {
                return StatusCode(500, new { msg = $"Internal Server Error: {ex.Message}" });
            }
        }
        public static decimal? Sum(String x) { return users[GUK(x)].Trans.Sum(k => k.Amount); }     
        public static int GUK(String x) { return users.FirstOrDefault(k => k.Value.Name == x).Key; }
    }
    public class Files
    {
        private static readonly String path = "bank.json";
        public static async Task Save(Dictionary<int, User> user)
        {
            var option = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
            var json = JsonSerializer.Serialize(user, option);
            await File.WriteAllTextAsync(path, json);
        }
        public static async Task<Dictionary<int, User>> Load()
        {
            if (!File.Exists(path)) return new Dictionary<int, User>();
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<Dictionary<int, User>>(json) ?? new Dictionary<int, User>();
        }
    }
    public class User
    {
        public String? Name { get; set; }
        public String? Pass { get; set; }
        public decimal? Balance { get; set; }
        public List<Transaction>? Trans { get; set; }      
    }
    public class Transaction
    {
        public String? From { get; set; }
        public String? To { get; set; }
        public String? Type { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Date { get; set; }
        public string? Reference { get; set; }
        public bool? deleted { get; set; } = false;
    }
    public class TransRequest
    {
        public String? Type { get; set; }
        public decimal? Amount { get; set; }
    }
}
