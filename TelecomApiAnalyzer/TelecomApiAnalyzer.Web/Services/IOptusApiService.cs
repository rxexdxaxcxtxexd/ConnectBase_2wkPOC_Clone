using System;
using System.Threading;
using System.Threading.Tasks;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public interface IOptusApiService
    {
        Task<OptusB2BSQResponse> ServiceQualificationAsync(OptusB2BSQParams parameters, CancellationToken cancellationToken = default);
        Task<OptusB2BQuoteResponse> CreateQuoteAsync(OptusB2BQuoteParams parameters, CancellationToken cancellationToken = default);
        Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
        Task<string> GetHealthStatusAsync(CancellationToken cancellationToken = default);
    }
}