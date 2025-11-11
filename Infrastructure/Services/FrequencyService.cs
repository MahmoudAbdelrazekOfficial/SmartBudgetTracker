using Application.DTOs.Frequency;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services;
public class FrequencyService(
    IUnitOfWork _unitOfWork,
    IMapper _mapper,
    IChangeLogService _changeLogService
    ) : IFrequencyService
{
    public async Task<Result<List<FrequencyDto>>> GetAllFrequenciesAsync()
    {
        var frequencies = await _unitOfWork.Repository<Frequency>()
            .GetQueryable()
            .OrderBy(f => f.Name)
            .ProjectTo<FrequencyDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Result<List<FrequencyDto>>.Success(frequencies);
    }

    public async Task<Result<FrequencyDto>> GetFrequencyByIdAsync(int id)
    {
        var frequency = await _unitOfWork.Repository<Frequency>().GetByIdAsync(id);

        if (frequency is null)
        {
            return Result<FrequencyDto>.Failure("Frequency not found.", HttpStatusCode.NotFound);
        }

        var frequencyDto = _mapper.Map<FrequencyDto>(frequency);
        return Result<FrequencyDto>.Success(frequencyDto);
    }

    public async Task<Result<int>> CreateFrequencyAsync(UpsertFrequencyRequest request)
    {
        var existingFrequency = (await _unitOfWork.Repository<Frequency>()
            .GetAsync(f => f.Name.ToLower() == request.Name.ToLower())).FirstOrDefault();

        if (existingFrequency is not null)
        {
            return Result<int>.Failure("A frequency with this name already exists.", HttpStatusCode.BadRequest);
        }

        var frequency = _mapper.Map<Frequency>(request);
        _changeLogService.SetCreateChangeLogInfo(frequency);

        var createdFrequency = await _unitOfWork.Repository<Frequency>().AddAsync(frequency);

        return Result<int>.Success(createdFrequency.Id, "Frequency created successfully.", HttpStatusCode.Created);
    }

    public async Task<Result<bool>> UpdateFrequencyAsync(int id, UpsertFrequencyRequest request)
    {
        var frequencyToUpdate = await _unitOfWork.Repository<Frequency>().GetByIdAsync(id);
        if (frequencyToUpdate is null)
        {
            return Result<bool>.Failure("Frequency not found.", HttpStatusCode.NotFound);
        }

        var existingFrequency = (await _unitOfWork.Repository<Frequency>()
            .GetAsync(f => f.Name.ToLower() == request.Name.ToLower() && f.Id != id)).FirstOrDefault();

        if (existingFrequency is not null)
        {
            return Result<bool>.Failure("A frequency with this name already exists.", HttpStatusCode.BadRequest);
        }

        _mapper.Map(request, frequencyToUpdate);
        _changeLogService.SetUpdateChangeLogInfo(frequencyToUpdate);
        await _unitOfWork.Repository<Frequency>().UpdateAsync(frequencyToUpdate);

        return Result<bool>.Success(true, "Frequency updated successfully.", HttpStatusCode.OK);
    }

    public async Task<Result<bool>> DeleteFrequencyAsync(int id)
    {
        var frequencyToDelete = await _unitOfWork.Repository<Frequency>().GetByIdAsync(id);
        if (frequencyToDelete is null)
        {
            return Result<bool>.Failure("Frequency not found.", HttpStatusCode.NotFound);
        }

        //TODO
        //var isInUse = (await _unitOfWork.Repository<RecurringTransaction>()
        //    .GetAsync(rt => rt.FrequencyId == id)).Any();

        //if (isInUse)
        //{
        //    return Result<bool>.Failure("Cannot delete this frequency as it is currently in use by recurring transactions.", HttpStatusCode.BadRequest);
        //}

        frequencyToDelete.IsDeleted = true;
        _changeLogService.SetDeleteChangeLogInfo(frequencyToDelete);
        await _unitOfWork.Repository<Frequency>().UpdateAsync(frequencyToDelete);

        return Result<bool>.Success(true, "Frequency deleted successfully.", HttpStatusCode.OK);
    }

}
