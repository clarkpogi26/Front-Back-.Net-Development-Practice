using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HangManController : ControllerBase
    {
        private static Dictionary<int, User> users = Files.Load().Result;
        private static Dictionary<String, Game> games = new();
        private static readonly String[] animal = {
    "Antelope", "Armadillo", "Baboon", "Badger", "Bat", "Bear", "Beaver", "Bison", "Buffalo", "Butterfly",
    "Camel", "Caribou", "Cat", "Cheetah", "Chimpanzee", "Chipmunk", "Cobra", "Cougar", "Coyote", "Crab",
    "Crocodile", "Crow", "Deer", "Dingo", "Dog", "Dolphin", "Donkey", "Dove", "Dragonfly", "Duck",
    "Eagle", "Echidna", "Eel", "Elephant", "Elk", "Emu", "Falcon", "Ferret", "Finch", "Fish",
    "Flamingo", "Fox", "Frog", "Gazelle", "Gecko", "Giraffe", "Goat", "Goose", "Gorilla", "Grasshopper",
    "Hamster", "Hare", "Hawk", "Hedgehog", "Heron", "Hippo", "Horse", "Hummingbird", "Hyena", "Iguana",
    "Jaguar", "Jay", "Jellyfish", "Kangaroo", "Kingfisher", "Kiwi", "Koala", "Komodo", "Leopard", "Lion",
    "Lizard", "Llama", "Lobster", "Macaw", "Magpie", "Manatee", "Meerkat", "Mole", "Monkey", "Moose",
    "Mouse", "Octopus", "Opossum", "Orangutan", "Ostrich", "Otter", "Owl", "Panda", "Panther", "Parrot",
    "Peacock", "Pelican", "Penguin", "Pigeon", "Porcupine", "Rabbit", "Raccoon", "Rat", "Reindeer", "Rhino"
};
        private static readonly String[] food = {
    "Apple", "Apricot", "Avocado", "Bagel", "Bacon", "Banana", "Barbecue", "Beans", "Beef", "Berry",
    "Bread", "Broccoli", "Brownie", "Burger", "Butter", "Cabbage", "Cake", "Candy", "Carrot", "Cauliflower",
    "Celery", "Cheese", "Cherry", "Chicken", "Chili", "Chocolate", "Clam", "Coconut", "Coffee", "Cookie",
    "Corn", "Couscous", "Crab", "Cucumber", "Cupcake", "Curry", "Date", "Doughnut", "Duck", "Dumpling",
    "Egg", "Eggplant", "Falafel", "Fig", "Fish", "Fries", "Garlic", "Ginger", "Grape", "Granola",
    "Grapefruit", "Ham", "Hazelnut", "Honey", "Hotdog", "Icecream", "Jam", "Jelly", "Juice", "Kale",
    "Kebab", "Kiwi", "Lamb", "Lasagna", "Lemon", "Lettuce", "Lime", "Lobster", "Macaroni", "Mango",
    "Melon", "Milk", "Muffin", "Mushroom", "Noodle", "Nut", "Oatmeal", "Olive", "Onion", "Orange",
    "Oyster", "Papaya", "Pasta", "Pastry", "Peach", "Peanut", "Pear", "Peas", "Pepper", "Pie",
    "Pineapple", "Pizza", "Plum", "Popcorn", "Potato", "Pretzel", "Pumpkin", "Quinoa", "Raspberry", "Rice"
};
        private static readonly String[] country = {
    "Afghanistan", "Albania", "Algeria", "Andorra", "Angola", "Argentina", "Armenia", "Australia", "Austria", "Azerbaijan",
    "Bahamas", "Bahrain", "Bangladesh", "Barbados", "Belarus", "Belgium", "Belize", "Benin", "Bhutan", "Bolivia",
    "Bosnia", "Botswana", "Brazil", "Brunei", "Bulgaria", "Burkina", "Burundi", "Cambodia", "Cameroon", "Canada",
    "Chad", "Chile", "China", "Colombia", "Comoros", "Congo", "Croatia", "Cuba", "Cyprus", "Czechia",
    "Denmark", "Djibouti", "Dominica", "Ecuador", "Egypt", "ElSalvador", "Eritrea", "Estonia", "Eswatini", "Ethiopia",
    "Fiji", "Finland", "France", "Gabon", "Gambia", "Georgia", "Germany", "Ghana", "Greece", "Grenada",
    "Guatemala", "Guinea", "Guyana", "Haiti", "Honduras", "Hungary", "Iceland", "India", "Indonesia", "Iran",
    "Iraq", "Ireland", "Israel", "Italy", "Jamaica", "Japan", "Jordan", "Kazakhstan", "Kenya", "Kiribati",
    "Kuwait", "Kyrgyzstan", "Laos", "Latvia", "Lebanon", "Lesotho", "Liberia", "Libya", "Liechtenstein", "Lithuania",
    "Luxembourg", "Madagascar", "Malawi", "Malaysia", "Maldives", "Mali", "Malta", "Mexico", "Moldova", "Monaco"
};
        Random rnd = new();
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (user.Name.Any(c => !Char.IsLetterOrDigit(c)))
                return BadRequest(new { msg = "Special characters aren't allowed" });
            if (users.Values.Any(k => k.Name == user.Name))
                return BadRequest(new { msg = "Name already exists" });
            if (user.Name.Length < 5 || user.Name.Length > 12)
                return BadRequest(new { msg = "Name must be (5-12) characters" });
            if (user.Pass.Length < 8 || user.Pass.Length > 16)
                return BadRequest(new { msg = "Pass must be (8-16) characters" });
            int uKey = users.Any() ? users.Keys.Max() + 1 : 1;
            users.Add(uKey, new User
            {
                Name = user.Name,
                Pass = Argon2.Hash(user.Pass),
                Streaks = new Streak()
            });
            await Files.Save(users);
            return Ok(new { msg = "Registered successfully" });
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] User user)
        {
            if (!users.Any(k => k.Value.Name == user.Name))
                return NotFound(new { msg = "Username doesn't exist" });
            var uKey = GUK(user.Name);
            if (uKey == null || uKey == 0 || !users.ContainsKey(uKey))
                return BadRequest(new { msg = "Key doesn't exist" });
            var u = users[uKey];
            if (!Argon2.Verify(u.Pass, user.Pass))
                return BadRequest(new { msg = "Wrong password" });
            return Ok(u);
        }
        [HttpPost("game/{cat}")]
        public IActionResult Game([FromBody] User user, String cat)
        {
            int? newhint = 3;
            if (games.ContainsKey(user.Name))
            {
                var prevgame = games[user.Name];
                if (prevgame.Status == "Won")
                    newhint = prevgame.Hints;
            }
            var word = "";
            switch (cat)
            {
                case "animal": word = animal[rnd.Next(animal.Length)]; break;
                case "food": word = food[rnd.Next(food.Length)]; break;
                case "country": word = country[rnd.Next(country.Length)]; break;
            }
            var masked = String.Join(" ", word.Select(c => "_"));
            var game = new Game
            {
                Category = cat,
                Word = word,
                Masked = masked,
                Guessed = new List<char>(),
                Lives = 6,
                Hints = newhint,
                Status = "Ongoing"
            };
            games[user.Name] = game;
            return Ok(new
            {
                msg = "Game Started",
                category = game.Category,
                masked = game.Masked,
                lives = game.Lives,
                hints = game.Hints,
                guessed = game.Guessed,
                status = game.Status
            });
        }
        [HttpPost("guess")]
        public IActionResult Guess([FromBody] Guess guess)
        {
            if (!games.ContainsKey(guess.User))
                return NotFound(new { msg = "No game found" });
            var game = games[guess.User];
            if (game.Status != "Ongoing")
                return BadRequest(new { msg = "Game already ended" });
            var let = Char.ToLower(guess.Letter);
            if (game.Guessed.Contains(let))
                return BadRequest(new { msg = "You already guessed that" });
            game.Guessed.Add(let);
            bool isit = game.Word.Any(c => Char.ToLower(c) == let);
            var right = game.Word.Where(c => game.Guessed.Contains(Char.ToLower(c))).Distinct().ToList();
            if (game.Word.Any(c => Char.ToLower(c) == let))
            {
                game.Masked = String.Join(" ", game.Word.Select(c => game.Guessed.Contains(Char.ToLower(c)) ? c.ToString() : "_"));
                if (!game.Masked.Contains("_"))
                    game.Status = "Won";
            } else
            {
                game.Lives--;
                if (game.Lives <= 0)
                    game.Status = "Lost";
            }
            return Ok(new
            {
                category = game.Category,
                masked = game.Masked,
                lives = game.Lives,
                guessed = game.Guessed,
                correct = right,
                status = game.Status,
                result = isit
            });
        }
        [HttpPost("score")]
        public async Task<IActionResult> Score([FromBody] User user)
        {
            if (!users.Any(k => k.Value.Name == user.Name))
                return NotFound(new { msg = "That user doesn't exist" });
            var u = users[GUK(user.Name)];
            u.Streaks = user.Streaks;
            await Files.Save(users);
            return Ok(u);
        }
        [HttpGet("remaining/{user}")]
        public async Task<IActionResult> Remaining(String user)
        {
            var game = games[user];
            var remain = game.Word.Where(c => !game.Guessed.Contains(c)).Distinct().ToList();
            var Masked = String.Join(" ", game.Word.Select(c => c.ToString()));
            return Ok(new { wrong = remain, masked = Masked });
        }
        [HttpGet("hint/{x}")] public IActionResult Hint(String x)
        {
            var game = games[x];
            if (game.Hints <= 0)
                return BadRequest(new { msg = "You ran out of hints" });
            var clues = game.Word.Where(k => !game.Guessed.Contains(Char.ToLower(k))).Distinct().ToList();
            var clue = clues[rnd.Next(clues.Count)];
            if (clue == null || clues.Count == 0 || game.Status != "Ongoing")
                return BadRequest(new { msg = "Game already ended" });
            game.Hints--;
            return Ok(new { let = clue, hints = game.Hints });
        }
        [HttpGet("leaderboard/{search}")]
        public IActionResult Leaderboard(String search)
        {
            if (String.IsNullOrEmpty(search) || search == null)
                return NotFound();
            if (search == "!")
                return Ok(users.Where(p => p.Value.Sum != 0)
                    .OrderByDescending(k => k.Value.Sum).Take(10));
            if (search == "@")
                return Ok(users.Where(p => p.Value.Streaks.Animal != 0)
                    .OrderByDescending(k => k.Value.Streaks.Animal).Take(10));
            if (search == "%")
                return Ok(users.Where(p => p.Value.Streaks.Food != 0)
                    .OrderByDescending(k => k.Value.Streaks.Food).Take(10));
            if (search == "$")
                return Ok(users.Where(p => p.Value.Streaks.Country != 0)
                    .OrderByDescending(k => k.Value.Streaks.Country).Take(10));
            else
                return Ok(users.Where(k => k.Value.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                    .Take(10));
        }
        public static int GUK(String x) { return users.FirstOrDefault(k => k.Value.Name == x).Key; }
    }
    public static class Files
    {
        private static readonly String path = "hangman.json";
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
            return JsonSerializer.Deserialize<Dictionary<int, User>>(json);
        }
    }
    public class Game
    {
        public String? Category { get; set; }
        public String? Word { get; set; } = "";
        public String? Masked { get; set; } = "";
        public int? Lives { get; set; } = 6;
        public int? Hints { get; set; }
        public List<char>? Guessed { get; set; } = new();
        public String? Status { get; set; } = "Ongoing";
    }
    public class User
    {
        public String? Name { get; set; }
        public String? Pass { get; set; }
        public Streak? Streaks { get; set; }
        public int? Sum => (Streaks?.Animal ?? 0) + (Streaks?.Food ?? 0) + (Streaks?.Country ?? 0);
    }
    public class Streak
    {
        public int? Animal { get; set; } = 0;
        public int? Food { get; set; } = 0;
        public int? Country { get; set; } = 0;
    }
    public class Guess
    {
        public String User { get; set; }
        public Char Letter { get; set; }
    }
}