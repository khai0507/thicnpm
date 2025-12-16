using System.IdentityModel.Tokens.Jwt;
using DocTask.Core.Dtos.Authentications;
using DocTask.Core.Exceptions;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Interfaces.Services;
using DocTask.Data.Repositories;
using TaskModel = DocTask.Core.Models.Task;

namespace DocTask.Service.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public AuthenticationService(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async System.Threading.Tasks.Task<LoginResponseDto> Login(LoginRequestDto request)
    {
        var foundUser = await _userRepository.GetByUserNameAsync(request.Username);
        if (foundUser == null || !BCrypt.Net.BCrypt.Verify(request.Password, foundUser.Password))
            throw new BadRequestException("Username or password is incorrect");

        foundUser!.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var refresh = _jwtService.GenerateRefreshToken(foundUser!);
        var updatedUser = await _userRepository.UpdateRefreshToken(foundUser, refresh);
        
        return new LoginResponseDto
        {
            AccessToken = _jwtService.GenerateAccessToken(foundUser!),
            RefreshToken =  refresh
        };
    }

    public async System.Threading.Tasks.Task Logout(string accessToken, string refreshToken)
    {
        var jwtToken = (JwtSecurityToken)_jwtService.ValidateAccessToken(accessToken);
        _jwtService.ValidateRefreshToken(refreshToken);
        var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
        if (string.IsNullOrEmpty(username))
            throw new UnauthorizedException("Invalid token");
        var user = await _userRepository.GetByUserNameAsync(username);
        if (user?.Refreshtoken == null || !user.Refreshtoken.Equals(refreshToken))
            throw new UnauthorizedException("Invalid token");
        await _userRepository.UpdateRefreshToken(user, null);
    }

    public async System.Threading.Tasks.Task<RefreshResponseDto> RefreshToken(string refreshToken)
    {
        var jwtToken = (JwtSecurityToken)_jwtService.ValidateRefreshToken(refreshToken);
        var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
        if (string.IsNullOrEmpty(username))
            throw new UnauthorizedException("Invalid token");
        var user = await _userRepository.GetByUserNameAsync(username);
        if (user?.Refreshtoken == null || !user.Refreshtoken.Equals(refreshToken))
            throw new UnauthorizedException("Invalid token");
        
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken(user);
        
        var updatedUser = await _userRepository.UpdateRefreshToken(user, newRefreshToken);
        
        return new RefreshResponseDto(newAccessToken, newRefreshToken);
    }

    public async System.Threading.Tasks.Task ChangePassword(int userId, ChangePasswordRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("User not found");

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password))
            throw new BadRequestException("Old password is incorrect");

        var newHashed = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordAsync(user, newHashed);
    }
}