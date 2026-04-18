namespace WebAPI.Services.Interfaces
{
    public interface IUserService
    {
        Task<object> GetAllUsersAsync(string? keyword, int page, int pageSize);
        Task<object?> GetUserDetailAsync(int id);
        Task<object> ResetPasswordAsync(int id);
    }
}