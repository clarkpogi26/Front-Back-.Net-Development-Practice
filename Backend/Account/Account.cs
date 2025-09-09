using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private static Dictionary<int, User> users = File.Load().Result;
        private static int userKey = 1;
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            try
            {
            if (user == null || String.IsNullOrEmpty(user.Name) || String.IsNullOrEmpty(user.Pass) ||
                users.Any(k => k.Value.Name == user.Name))
                return BadRequest(new { msg = "Error" });
                if (user.Name.Length < 4 || user.Name.Length > 12)
                    return BadRequest(new { msg = "Name must be 4-12 long" });
                if (user.Pass.Length < 6 || user.Pass.Length > 18)
                    return BadRequest(new { msg = "Password must be 6-18 long" });  
                if (!user.Pass.Any(k => !char.IsLetterOrDigit(k)))
                    return BadRequest(new { msg = "Password must contain a special character" });
                userKey = users.Any() ? users.Keys.Max() + 1 : 1;
                       users.Add(userKey++, new User { Name = user.Name, Pass = Argon2.Hash(user.Pass) });
                await File.Save(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { msg = "Save Failed", error = ex.Message });
            }
            return Ok(new { msg = "Successully registered" });
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] User user)
        {
            if (!users.ContainsKey(GUK(user.Name)) || !users.Any(k => k.Value.Name == user.Name))
                return BadRequest(new { msg = "Name cannot be found" });
            if (!Argon2.Verify(users[GUK(user.Name)].Pass, user.Pass))
                return BadRequest(new { msg = "Wrong password" });
            return Ok(new { uzer = users[GUK(user.Name)], msg = $"{user.Name} Successfully logged on"});
        }
        [HttpGet("show")]
        public IActionResult ShowAcc()
        {
            return Ok(users);
        }
        [HttpPut("byname/{name}")]
        public async Task<IActionResult> UpdateName(String name, [FromBody] User user)
        {
            if (user.Name.Length < 6 || user.Name.Length > 12)
                return BadRequest(new { msg = "Name must be 8-12 long" });
            if (!users.Any(k => k.Value.Name == name))
                return NotFound();
            if (users.Any(k => k.Value.Name == user.Name))
                return BadRequest(new { msg = "Name already taken" });
            User cur = users[GUK(name)];
            cur.Name = user.Name;
            await File.Save(users);
            return Ok(new { msg = "Name change successful" });
        }
        [HttpPut("bypass")]
        public async Task<IActionResult> UpdatePass([FromBody] User user)
        {
            if (!users.ContainsKey(GUK(user.Name)))
                return NotFound();
            if (user.Pass.Length < 6 || user.Pass.Length > 12)
                return BadRequest(new { msg = "Password must be 6-12 long" });
            if (!user.Pass.Any(k => !char.IsLetterOrDigit(k)))
                return BadRequest(new { msg = "Password must include a special character" });
            User cur = users[GUK(user.Name)];
            cur.Pass = Argon2.Hash(user.Pass);
            await File.Save(users);
            return Ok(new { msg = "Name change successful" });
        }
        [HttpDelete("delete/{name}")]
        public async Task<IActionResult> DeleteUser(String name)
        {
            if (!users.Any(k => k.Value.Name == name))
                return BadRequest(new { msg = "That account doesn't exist" });
            users.Remove(GUK(name));
            Arrange();
            await File.Save(users);
            return Ok(new { msg = "Account removal successful" });
        }
        public static void Arrange()
        {
            Dictionary<int, User> newUsers = new();
            int newKey = 1;
            foreach (var x in users) { newUsers[newKey++] = x.Value; } users = newUsers;
        }
        public class File()
        {
            private static readonly String path = "acc.json";
            public static async Task<Dictionary<int, User>> Load()
            {
                if (!System.IO.File.Exists(path)) return new Dictionary<int, User>();
                var json = await System.IO.File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<Dictionary<int, User>>(json);
            }
            public static async Task Save(Dictionary<int, User> user)
            {
                var option = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
                var json = JsonSerializer.Serialize(user, option);
                await System.IO.File.WriteAllTextAsync(path, json);
            }
        }
        private static int GUK(String x) { return users.FirstOrDefault(k => k.Value.Name == x).Key; }
        public class User
        {
            public String? Name { get; set; }
            public String? Pass { get; set; }
        }
    }
}
