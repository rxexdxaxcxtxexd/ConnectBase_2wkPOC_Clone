using System.Threading.Tasks;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public interface IPostmanCollectionGenerator
    {
        Task<PostmanCollection> GenerateCollectionAsync(TechnicalSpecification specification);
    }
}