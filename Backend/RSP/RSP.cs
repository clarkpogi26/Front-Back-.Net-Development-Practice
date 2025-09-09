using Microsoft.AspNetCore.Mvc;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private static readonly String[] choice = { "rock", "paper", "scissor" };
        private readonly Random rnd = new();
        [HttpGet("{playerChoice}")]
        public IActionResult Play(string playerChoice)
        {
            if (!choice.Contains(playerChoice))
            {
                return BadRequest(new { error = "Only 'rock', 'paper', 'scissor'" });
            }
            String clankerChoice = choice[rnd.Next(choice.Length)];
            String res = Decider(playerChoice, clankerChoice);
            return Ok(new { player = playerChoice, clanker = clankerChoice, result = res });
        }
        public static String Decider(String a, String b)
        {
            if (a == b) return "Tie!";
            return (a, b) switch
            {
                ("rock", "scissor") => "Player Won!",
                ("paper", "rock") => "Player Won!",
                ("scissor", "paper") => "Player Won!",
                _ => "Clanker Won!"
            };
        }
    }
}
