namespace WebAPI.DTOs
{
    public record BookSummaryDto(
        int BookId,
        string Title,
        string? Author,
        decimal Price,
        string? Image,
        string? CategoryName,
        int NumberStock,
        int NumberSold
    );
    public record BookDetailDto(
        int BookId,
        string Title,
        string Author,
        decimal Price,
        string? Image,
        string? Description,
        int? PublisherYear,
        int NumberPage,
        int NumberStock,
        int NumberSold,
        int? CategoryId,
        string? CategoryName,
        double AvgRating,
        int ReviewCount
    );
    public record BookUpsertDto(
        string? Author,
        string Title,
        int? PublisherYear,
        string? Description,
        string? Image,
        decimal Price,
        int? NumberPage,
        int NumberStock,
        int? CategoryId
    );
    public record BookQueryDto(
        int Page = 1,
        int PageSize = 10,
        string? Keyword = null,
        int? CategoryId = null,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        string SortBy = "createdAt",
        string SortOrder = "desc"
    );
    public record BookPagedResultDto(
        int Total,
        int Page,
        int PageSize,
        int TotalPages,
        List<BookSummaryDto> Data
    );
}
