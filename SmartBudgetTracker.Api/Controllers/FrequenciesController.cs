using Application.Constants;
using Application.DTOs.Frequency;
using Application.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
public class FrequenciesController(IFrequencyService _frequencyService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = AppRoles.User)]
    [ProducesResponseType(typeof(Result<List<FrequencyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFrequencies()
    {
        var result = await _frequencyService.GetAllFrequenciesAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = AppRoles.User)]
    [ProducesResponseType(typeof(Result<FrequencyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<FrequencyDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFrequencyById(int id)
    {
        var result = await _frequencyService.GetFrequencyByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFrequency([FromBody] UpsertFrequencyRequest request)
    {
        var result = await _frequencyService.CreateFrequencyAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFrequency(int id, [FromBody] UpsertFrequencyRequest request)
    {
        var result = await _frequencyService.UpdateFrequencyAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFrequency(int id)
    {
        var result = await _frequencyService.DeleteFrequencyAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    
}

