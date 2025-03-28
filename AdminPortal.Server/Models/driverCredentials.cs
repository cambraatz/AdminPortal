namespace AdminPortal.Server.Models
{
    public class driverCredentials
    {
        public string? USERNAME { get; set; }

        public string? PASSWORD { get; set; }

        public string? POWERUNIT { get; set; }

        public List<string>? COMPANIES { get; set; } = new List<string>();
        public List<string>? MODULES { get; set; } = new List<string>();
    }
}
