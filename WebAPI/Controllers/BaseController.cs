using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    public class BaseController : ControllerBase
    {
        [NonAction]
        protected IActionResult HandleResult(ServiceResult result)
        {
            if (result == null)
                return StatusCode(500, new { message = "Lỗi hệ thống." });
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        [NonAction]
        protected IActionResult HandleResult<T>(ServiceResult<T> result)
        {
            if (result == null) return StatusCode(500, new { message = "Lỗi hệ thống." });

            if (result.IsSuccess)
            {
                object responseData = (object?)result.Data ?? new { message = result.Message };
                return StatusCode(result.StatusCode, responseData);
            }
               

            return StatusCode(result.StatusCode, new { message = result.Message });
        }
    }
}
