

namespace ClientCore.Entity
{
    public record class User
    {
        public int id { get; set; }
        public string username { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string tag { get; set; } = string.Empty;
        public int side { get; set; } = 3;

    }
}
