using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text.Json;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        public static Dictionary<int, User> users = SaveFile.Load();       

        [HttpPost("accinput")]
        public IActionResult NameCreation([FromBody] User accInput)
        {
            if (accInput == null || String.IsNullOrEmpty(accInput.user) || String.IsNullOrEmpty(accInput.pass))
            {
                return BadRequest(new { error = "Error" });
            }
            int id = users.Count + 1;
            users[id] = accInput;
            SaveFile.Save(users);
            return Ok(new { user = accInput });
        }
        [HttpGet("result")]
        public IActionResult Show()
        {
            return Ok(users);
        }
    }
    public class User
    {
        public String user { get; set; }
        public String pass { get; set; }
    }
    public static class SaveFile
    {
        private static readonly String path = "save.json";
        public static void Save(Dictionary<int, User> users)
        {
            var option = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            var json = JsonSerializer.Serialize(users);
            File.WriteAllText(path, json);
        }
        public static Dictionary<int, User> Load()
        {
            if (!File.Exists(path)) return new Dictionary<int, User>();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<int, User>>(json) ?? new Dictionary<int, User>();
        }
    }
}
