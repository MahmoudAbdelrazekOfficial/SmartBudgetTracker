using Application.DTOs.Authentcation;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface IAuthService
{
    Task<Result<string>> RegisterAsync(RegisterDto request);
    Task<Result<AuthResponse>> LoginAsync(string email, string password);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<Result<AuthResponse>> LoginWithGoogleAsync(GoogleSignInVM model, string ipAddress);
    Task<Result<string>> LogoutAsync(LogoutDto request);
}
