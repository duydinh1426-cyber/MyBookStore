namespace WebAPI.DTOs
{
    public record CategoryDto(int CategoryId, string CategoryName);
    public record CategoryUpsertDto(string CategoryName);
}
