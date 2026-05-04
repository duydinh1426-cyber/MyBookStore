using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Data.Models;

namespace WebAPI.Services.Chat;

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Text { get; set; } = "";
}

public interface IChatService
{
    Task<string> SendMessageAsync(List<ChatMessage> history, string userMessage);
}

public class ChatService(IConfiguration config, IHttpClientFactory httpFactory, DBContext db) : IChatService
{
    public async Task<string> SendMessageAsync(List<ChatMessage> history, string userMessage)
    {
        var apiKey = config["Gemini:ApiKey"];
        var model = config["Gemini:Model"] ?? "gemini-1.5-flash-8b";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        // ── Lấy dữ liệu sách từ DB ────────────────────────────────────
        var books = await db.Books
            .Include(b => b.Category)
            .Where(b => b.NumberStock > 0)
            .OrderByDescending(b => b.NumberSold)
            .Take(100)
            .Select(b => new
            {
                b.BookId,
                b.Title,
                b.Author,
                Category = b.Category != null ? b.Category.CategoryName : "Khác",
                b.Price,
                b.AvgRating,
                b.NumberSold,
                b.Description
            })
            .ToListAsync();

        // ── Build danh sách sách cho system prompt ────────────────────
        var bookList = string.Join("\n", books.Select(b =>
            $"- [{b.BookId}] \"{b.Title}\" | Tác giả: {b.Author ?? "N/A"} | Thể loại: {b.Category} | Giá: {b.Price:N0}đ | Đánh giá: {b.AvgRating:F1}/5 | Đã bán: {b.NumberSold}"
        ));

        var systemPrompt = $"""
            Bạn là trợ lý AI của BookStore - một cửa hàng sách online.
            Nhiệm vụ của bạn là tư vấn và gợi ý sách phù hợp cho khách hàng dựa trên danh sách sách thực tế trong kho.
            
            DANH SÁCH SÁCH HIỆN CÓ TRONG KHO ({books.Count} cuốn):
            {bookList}
            
            HƯỚNG DẪN:
            - Chỉ gợi ý sách có trong danh sách trên (dựa vào BookId)
            - Khi gợi ý sách, luôn nêu tên sách, tác giả, thể loại và giá
            - Nếu khách hỏi về thể loại cụ thể, lọc theo cột Thể loại
            - Gợi ý tối đa 3-5 cuốn phù hợp nhất
            - Trả lời ngắn gọn, thân thiện bằng tiếng Việt
            - Nếu không có sách phù hợp, thông báo lịch sự
            """;

        // ── Gọi Gemini API ────────────────────────────────────────────
        var contents = history
            .Select(m => (object)new { role = m.Role, parts = new[] { new { text = m.Text } } })
            .ToList();
        contents.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

        var body = new { system_instruction = new { parts = new[] { new { text = systemPrompt } } }, contents };
        var client = httpFactory.CreateClient();
        var payload = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(url, payload);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode) throw new Exception($"Gemini {resp.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "Xin lỗi, tôi không thể trả lời lúc này.";
    }
}