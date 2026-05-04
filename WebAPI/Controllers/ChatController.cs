using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Chat;

namespace WebAPI.Controllers;

public class ChatRequestDto
{
    public List<ChatMessage>? History { get; set; }
    public string Message { get; set; } = "";
}


[ApiController]
[Route("api/chat")]
public class ChatController(IChatService chatService) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { message = "Message is required." });

        try
        {
            var reply = await chatService.SendMessageAsync(dto.History ?? [], dto.Message);
            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "AI error.", detail = ex.Message });
        }
    }
}