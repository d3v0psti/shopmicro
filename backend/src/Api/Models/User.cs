namespace Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Cpf { get; set; }
        public string? Phone { get; set; }
        public string? Cep { get; set; }
        public string? Address { get; set; }
        public string UserType { get; set; } = "Client";
    }
}
