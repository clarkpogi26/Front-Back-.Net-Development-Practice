using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HighLowController : ControllerBase
    {
        public static HttpClient client = new();
        public static Random rnd = new();
        public static int secret = rnd.Next(1, 101);
        public static Dictionary<int, User> users = SaveFile.Load();
        [HttpPost("account")]
        public IActionResult Account([FromBody] User userIn)
        {
            if (String.IsNullOrEmpty(userIn.name) || String.IsNullOrEmpty(userIn.pass)
                || users.Any(k => k.Value.name == userIn.name))
                return BadRequest(new { error = "error" });
            int userKey = users.Count + 1;
            users[userKey] = userIn;
            SaveFile.Save(users);
            return Ok(new { user = userIn });
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] User userIn)
        {
            var match = users.Values.FirstOrDefault(k => k.name == userIn.name);
            if (match == null)
                return BadRequest(new { error = "Can't find any user with that name" });
            if (userIn.pass != match.pass)
                return BadRequest(new { error = "Invalid password" });
            return Ok(new { user = match });
        }
        [HttpPost("{hilo}")]
        public IActionResult HighLow(String hilo)
        {
            if (!int.TryParse(hilo, out int highlow) || highlow > 100)
                return BadRequest(new { error = "Numbers (1-100) Only" });
            if (highlow == secret)
                return Ok(new { result = "Correct" });
            else if (highlow > secret)
                return Ok(new { result = "Lower" });
            else
                return Ok(new { result = "Higher" });           
        }
        [HttpGet("showacc")]
        public IActionResult ShowAcc()
        {
            return Ok(users);
        }
        [HttpPatch("score")]
        public IActionResult Score([FromBody] User newScore)
        {
            if (newScore == null)
                return BadRequest(new { error = "Invalid" });
            if (newScore.score > users[GK(newScore.name)].score)
            users[GK(newScore.name)].score = newScore.score;
            SaveFile.Save(users);
            return Ok(new { success = "Successful" });
        }
        [HttpDelete("byname/{name}")]
        public IActionResult DeleteUser(String name)
        {
            if (String.IsNullOrEmpty(name) || name == null)
                return NotFound();
            users.Remove(GK(name));
            Arrange();
            SaveFile.Save(users);
            return Ok(new { success = "User successfully deleted" });
        }
        public static void Arrange()
        {
            Dictionary<int, User> newUser = new();
            int newKey = 1;
            foreach (var x in users) newUser[newKey++] = x.Value;
            users = newUser;
        }
        public static int GK(String x) { return users.FirstOrDefault(k => k.Value.name == x).Key; }
        public class User
        {
            public String? name { get; set; }
            public String? pass { get; set; }
            public int? score { get; set; }
        }
        public static class SaveFile
        {
            private static readonly String path = "hilo.json";
            public static void Save(Dictionary<int, User> user)
            {
                var option = new JsonSerializerOptions { IncludeFields = true };
                var json = JsonSerializer.Serialize(user);
                System.IO.File.WriteAllText(path, json);
            }
            public static Dictionary<int, User> Load()
            {
                if (!System.IO.File.Exists(path)) return new Dictionary<int, User>();
                var json = System.IO.File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<int, User>>(json);
            }
        }
    }
}
