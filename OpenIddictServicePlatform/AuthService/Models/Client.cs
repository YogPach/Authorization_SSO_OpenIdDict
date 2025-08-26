namespace AuthService.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string ClientId { get; set; } 
        public string ClientSecret { get; set; }
        public string DisplayName { get; set; }
        public string RedirectUris { get; set; }

    }
}
