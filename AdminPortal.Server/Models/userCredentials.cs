namespace AdminPortal.Server.Models
{
    public class UserCredentials
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Powerunit { get; set; }
        public List<string>? Companies { get; set; } = new List<string>();
        public List<string>? Modules { get; set; } = new List<string>();
    }
}
