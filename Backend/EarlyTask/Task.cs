using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private static Dictionary<int, User> users = Files.Load().Result;
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (users.Any(k => k.Value.Name == user.Name))
                return BadRequest(new { msg = "Name already exists" });
            if (user.Name.Length < 5)
                return BadRequest(new { msg = "Name must be atleast 5 characters" });
            if (user.Pass.Length < 8)
                return BadRequest(new { msg = "Pass must be atleast 8 characters" });
            int userKey = users.Any() ? users.Keys.Max() + 1 : 1;
            users.Add(userKey, new User { Name = user.Name, Pass = Argon2.Hash(user.Pass) });
            await Files.Save(users);
            return Ok(new { msg = "Account successfully created" });
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] User user)
        {
            var u = users[GUK(user.Name)];
            if (!users.Any(k => k.Value.Name == user.Name) || u == null)
                return NotFound(new { msg = "Name cannot be found" });
            if (!Argon2.Verify(u.Pass, user.Pass))
                return BadRequest(new { msg = "Wrong password" });
            return Ok(u);
        }
        [HttpPost("task/{user}")]
        public async Task<IActionResult> CreateTask([FromBody] UserTask task, String user)
        {
            if (!users.Any(k => k.Value.Name == user))
                return NotFound(new { msg = "Name doesn't exist" });
            if (String.IsNullOrEmpty(task.Title) || String.IsNullOrEmpty(task.Description))
                return BadRequest(new { msg = "Must include something" });
            users[GUK(user)].tasks.Add(new UserTask
            {
                Title = task.Title,
                Description = task.Description,
                Status = "To Do",
                Date = DateOnly.FromDateTime(DateTime.Now)
            });
            await Files.Save(users);
            return Ok(new { msg = "Task successfully created" });
        }
        [HttpPost("edittasktitle/{user}/{tname}")]
        public async Task<IActionResult> EditTaskTitle([FromBody] UserTask task, String user, String tname)
        {
            try
            {
                var u = users[GUK(user)];
                if (!users.Any(k => k.Value.Name == user) || u == null)
                    return NotFound(new { msg = "Name cannot be found" });
                if (String.IsNullOrEmpty(task.Title))
                    return BadRequest(new { msg = "Must include something" });
                var t = u.tasks.FirstOrDefault(k => k.Title == tname);
                if (!u.tasks.Any(k => k.Title == tname) || t == null)
                    return NotFound(new { msg = "Task cannot be found" });
                t.Title = task.Title;
                await Files.Save(users);
                return Ok(new { msg = "Title changed successfully" });
            } catch (Exception err)
            {
                return StatusCode(500, new { msg = $"Internal Server Error: {err.Message}" });
            }
        }
        [HttpPost("edittaskdesc/{user}/{tname}")]
        public async Task<IActionResult> EditTaskDesc([FromBody] UserTask task, String user, String tname)
        {
            try
            {
                var u = users[GUK(user)];
                if (!users.Any(k => k.Value.Name == user) || u == null)
                    return NotFound(new { msg = "Name cannot be found" });
                if (String.IsNullOrEmpty(task.Description))
                    return BadRequest(new { msg = "Must include something" });
                var t = u.tasks.FirstOrDefault(k => k.Title == tname);
                if (!u.tasks.Any(k => k.Title == tname) || t == null)
                    return NotFound(new { msg = "Task cannot be found" });
                t.Description = task.Description;
                await Files.Save(users);
                return Ok(new { msg = "Description changed successfully" });
            } catch (Exception err)
            {
                return StatusCode(500, new { msg = $"Internal Server Error: {err.Message}" });
            }
        }
        [HttpPost("edittaskstat/{user}/{tname}")]
        public async Task<IActionResult> EditTaskStat([FromBody] UserTask task, String user, String tname)
        {
            try
            {
                var u = users[GUK(user)];
                if (!users.Any(k => k.Value.Name == user) || u == null)
                    return NotFound(new { msg = "Name cannot be found" });
                var t = u.tasks.FirstOrDefault(k => k.Title == tname);
                if (!u.tasks.Any(k => k.Title == tname) || t == null)
                    return NotFound(new { msg = "Task cannot be found" });
                t.Status = task.Status;
                await Files.Save(users);
                return Ok(new { msg = "Status changed successfully" });
            } catch (Exception err)
            {
                return StatusCode(500, new { msg = $"Internal Server Error: {err.Message}" });
            }
        }
        [HttpDelete("clearuser")] public async Task<IActionResult> ClearUser()
        {
            users.Clear();
            await Files.Save(users);
            return Ok(new { msg = "Users cleared" });
        }
        [HttpGet("showuser")] public IActionResult ShowUser() { return Ok(users); }
        [HttpGet("showtask/{x}")] public IActionResult ShowTask(String x) { return Ok(users[GUK(x)].tasks); }
        public static int GUK(String x) { return users.FirstOrDefault(k => k.Value.Name == x).Key; }
    }
    public class Files
    {
        private static readonly String path = "task.json";
        public static async Task Save(Dictionary<int, User> u)
        {
            var option = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
            var json = JsonSerializer.Serialize(u, option);
            await File.WriteAllTextAsync(path, json);
        }
        public static async Task<Dictionary<int, User>> Load()
        {
            if (!File.Exists(path)) return new Dictionary<int, User>();
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<Dictionary<int, User>>(json);
        }
    }
    public class User
    {
        public String? Name { get; set; }
        public String? Pass { get; set; }
        public List<UserTask>? tasks { get; set; } = new();
    }
    public class UserTask
    {
        public String? Title { get; set; }
        public String? Description { get; set; }
        public String? Status { get; set; }
        public DateOnly? Date { get; set; }
    }
}
