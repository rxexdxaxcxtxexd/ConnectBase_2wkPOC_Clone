using System.Threading.Tasks;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public interface IApiDocumentAnalyzer
    {
        Task<TechnicalSpecification> AnalyzeDocumentAsync(ApiDocument document);
        Task<UseCaseGuide> GenerateUseCaseGuideAsync(ApiDocument document);
        Task<ApiEndpoint[]> ExtractEndpointsAsync(string documentContent);
        Task<DataModel[]> ExtractDataModelsAsync(string documentContent);
        Task<AuthenticationDetails> ExtractAuthenticationDetailsAsync(string documentContent);
    }
}