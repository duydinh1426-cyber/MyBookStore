using MimeKit.Tnef;

namespace WebAPI.DTOs
{
    public record CreateReviewDto(int bookId, int rating, string? comment);
    public record ReviewResponseDto(
        int reviewId, 
        string customerName,
        int rating, 
        string? comment,
        DateTime createAt);
}
