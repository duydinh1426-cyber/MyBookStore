namespace WebAPI.Services.User
{
    public interface IUserService
    {
        Task<ServiceResult<object>> GetAllUsersAsync(string? keyword, int page, int pageSize);
        Task<ServiceResult<object>> GetUserDetailAsync(int id);
    }
}