using System.Threading.Tasks;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public interface ICodeGenerationService
    {
        Task<GeneratedCode> GenerateCodeAsync(TechnicalSpecification specification);
        Task<string> GenerateApiClientAsync(TechnicalSpecification specification);
        Task<string> GenerateModelsAsync(TechnicalSpecification specification);
        Task<string> GenerateServiceInterfaceAsync(TechnicalSpecification specification);
        Task<string> GenerateServiceImplementationAsync(TechnicalSpecification specification);
    }
}