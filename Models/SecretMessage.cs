namespace SelfDestructMessageAPI.Models
{
    public class SecretMessage
    {
        public string Message { get; set; } = string.Empty;
        public int Duration { get; set; } = 15;
        public DateTime? FirstReadTime { get; set; }
    }
}
