using DocTask.Core.DTOs.ApiResponses;
using DocTask.Core.Dtos.Authentications;
using DocTask.Core.Exceptions;
using DocTask.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocTask.Api.Controllers;

[ApiController]
[Route("/api/v1/auth")]
[SwaggerTag("Xác thực")]

public class AuthenticationController : ControllerBase
{
    private IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    [SwaggerOperation(Summary = "Đăng nhập", Description = "Trả về access token và refresh token")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authenticationService.Login(request);
        //     /* string hashChuan = BCrypt.Net.BCrypt.HashPassword("123456");
        // return Ok(new 
        // {
        //     Message = "HÃY COPY DÒNG DƯỚI UPDATE VÀO DATABASE",
        //     HashPassword = hashChuan
        // });
        // */
        return Ok(new ApiResponse<LoginResponseDto>
        {
            Data = result,
            Message = "Login success"
        });
    }

    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Đăng xuất", Description = "Trả ra message khi đăng xuất thành công")]
    public async Task<IActionResult> Logout([FromHeader] string accessToken, [FromHeader] string refreshToken)
    {
        await _authenticationService.Logout(accessToken, refreshToken);

        return Ok(new ApiResponse<object>
        {
            Message = "Logout success"
        });
    }

    [HttpPost("refresh")]
    [SwaggerOperation(Summary = "Làm mới token", Description = "Trả ra về access token và refresh token mới")]
    public async Task<IActionResult> Refresh([FromHeader] string refreshToken)
    {
        var result = await _authenticationService.RefreshToken(refreshToken);
        return Ok(new ApiResponse<RefreshResponseDto>
        {
            Data = result,
            Message = "Refresh token success"
        });
    }
}