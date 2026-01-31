using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SelfDestructMessageAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace SelfDestructMessageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecretController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        public SecretController(IMemoryCache memoryCache, IConfiguration config)
        {
            _cache = memoryCache;
            _config = config;
        }

        [HttpPost]
        public IActionResult CreateMessage([FromBody] SecretMessage message)
        {
            if (string.IsNullOrEmpty(message.Message))
            {
                return BadRequest("Message content cannot be empty.");
            }

            if (message.Duration <= 0) message.Duration = 10;
            if (message.Duration > 60) message.Duration = 60;

            string? mySecretKey = _config["SecretKey"];

            if (string.IsNullOrEmpty(mySecretKey))
            {
                return StatusCode(500, "Server Error: Encryption key (SecretKey) is missing configuration.");
            }

            string id = Guid.NewGuid().ToString();

            var settings = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            message.Message = EncryptionHelper.Encrypt(message.Message, mySecretKey);

            _cache.Set(id, message, settings);

            string url = $"{Request.Scheme}://{Request.Host}/api/secret/{id}";

            return Ok(new { url, id });
        }

        [HttpGet("{id}")]
        public IActionResult GetMessage(string id)
        {
            if (_cache.TryGetValue(id, out SecretMessage? foundMessage) && foundMessage != null)
            {
                if (foundMessage.FirstReadTime == null)
                {
                    foundMessage.FirstReadTime = DateTime.Now;

                    var newSettings = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(foundMessage.Duration));

                    _cache.Set(id, foundMessage, newSettings);
                }

                string? mySecretKey = _config["SecretKey"];

                if (string.IsNullOrEmpty(mySecretKey))
                {
                    return StatusCode(500, "Server Error: Encryption key is missing.");
                }

                string decryptedText;
                try
                {
                    decryptedText = EncryptionHelper.Decrypt(foundMessage.Message, mySecretKey);
                }
                catch
                {
                    return BadRequest("Decryption failed. Message is corrupted or key is invalid.");
                }

                return Ok(new
                {
                    message = decryptedText,
                    duration = foundMessage.Duration,
                    firstReadTime = foundMessage.FirstReadTime
                });
            }

            return NotFound("This message no longer exists or has expired.");
        }
    }
}