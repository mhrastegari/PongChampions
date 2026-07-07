using PongChampions.Api.Common.Dtos.User;

namespace PongChampions.Api.Services;

public interface IAuthService
{
    Task RegisterAsync(UserRegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(UserLoginDto dto);
}
