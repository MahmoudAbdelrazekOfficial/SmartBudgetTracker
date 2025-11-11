using Application.Constants;
using Application.DTOs.Profile;
using Application.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
public class ProfileController(IProfileService _profileService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = AppRoles.User)]
    [ProducesResponseType(typeof(Result<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _profileService.GetProfileAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut]
    [Authorize(Roles = AppRoles.User)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var result = await _profileService.UpdateProfileAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.User)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _profileService.ChangePasswordAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }
}

