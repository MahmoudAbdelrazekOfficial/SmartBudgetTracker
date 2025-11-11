using Application.DTOs.Authentcation;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services;
public class AuthService(UserManager<ApplicationUser> _userManager, IJwtService _jwtService, AppDbContext _context, IGoogleAuthService _googleAuthService) :IAuthService
{

    public async Task<Result<string>> RegisterAsync(RegisterDto request)
    {
        var userExists = await _userManager.FindByEmailAsync(request.Email);
        if (userExists != null)
        {
            return Result<string>.Failure("A user with this email already exists.", HttpStatusCode.Conflict);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<string>.Failure(errorMessage, HttpStatusCode.BadRequest);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            var errorMessage = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            return Result<string>.Failure(errorMessage, HttpStatusCode.BadRequest);
        }

        return Result<string>.Success("User registered successfully", HttpStatusCode.Created);
    }

    public async Task<Result<AuthResponse>> LoginAsync(string email, string password)
    {
        var user = await _userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Email == email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            return Result<AuthResponse>.Failure("Invalid credentials", HttpStatusCode.Unauthorized);

        var accessToken = await _jwtService.GenerateToken(user);

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = "user-ip-or-placeholder",
            User = user
        };

        user.RefreshTokens.Add(refreshToken);
        await _userManager.UpdateAsync(user);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(30) 
        };

        return Result<AuthResponse>.Success(response, "Login successful", HttpStatusCode.OK);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var user = _userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == refreshToken));

        if (user == null)
            return Result<AuthResponse>.Failure("Invalid refresh token", HttpStatusCode.Unauthorized);

        var token = user.RefreshTokens.Single(x => x.Token == refreshToken);

        if (token.IsExpired || token.Revoked != null)
            return Result<AuthResponse>.Failure("Refresh token is invalid or expired", HttpStatusCode.Unauthorized);

        var accessToken = await _jwtService.GenerateToken(user);

        var newRefreshToken = GenerateRefreshToken(ipAddress);
        user.RefreshTokens.Add(newRefreshToken);

        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;

        await _userManager.UpdateAsync(user);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token
        };

        return Result<AuthResponse>.Success(response, "Token refreshed successfully", HttpStatusCode.OK);
    }

    public async Task<Result<string>> LogoutAsync(LogoutDto request)
    {
        var token = await _context.Users
            .SelectMany(u => u.RefreshTokens)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (token == null)
            return Result<string>.Failure("Invalid refresh token.", HttpStatusCode.BadRequest);

        _context.RefreshTokens.Remove(token);
        await _context.SaveChangesAsync();

        return Result<string>.Success("User logged out successfully.", HttpStatusCode.OK);
    }

    public async Task<Result<AuthResponse>> LoginWithGoogleAsync(GoogleSignInVM model, string ipAddress)
    {
        var userResult = await _googleAuthService.GoogleSignInAsync(model);

        if (!userResult.IsSuccess)
            return Result<AuthResponse>.Failure(userResult.Message);

        var user = userResult.Data!;

        var accessToken = await _jwtService.GenerateToken(user);

        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            Expires = DateTime.UtcNow.AddDays(7),
            User = user
        };

        user.RefreshTokens.Add(refreshToken);
        await _userManager.UpdateAsync(user);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        return Result<AuthResponse>.Success(response, "Login with Google successful");
    }

    private RefreshToken GenerateRefreshToken(string ipAddress)
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }

}
