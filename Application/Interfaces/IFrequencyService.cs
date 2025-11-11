using Application.DTOs.Frequency;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface IFrequencyService
{
    Task<Result<List<FrequencyDto>>> GetAllFrequenciesAsync();
    Task<Result<FrequencyDto>> GetFrequencyByIdAsync(int id);
    Task<Result<int>> CreateFrequencyAsync(UpsertFrequencyRequest request); 
    Task<Result<bool>> UpdateFrequencyAsync(int id, UpsertFrequencyRequest request); 
    Task<Result<bool>> DeleteFrequencyAsync(int id);
}
