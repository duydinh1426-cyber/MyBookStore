namespace WebAPI.DTOs
{
    public class ContactDto
    {
        public string? Name { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Topic { get; set; }
        public string Message { get; set; } = null!;
    }
}
