using BackToSchool.Application.Dtos.Security;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;

namespace BackToSchool.Application.Interfaces.Security;

public interface IAuthService
{
    Task<ResponseGetObject> Login(LoginDto loginDto, string? ipAddress);
    Task<ResponseGetObject> RefreshToken(RefreshRequestDto refreshRequestDto, string? ipAddress);
    Task<ResponsePost> Logout(LogoutRequestDto logoutRequestDto);
}