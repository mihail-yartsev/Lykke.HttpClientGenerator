using System.Threading.Tasks;
using Refit;

namespace MihailYartsev.HttpClientGenerator.Tests
{
    public interface ITestInterface
    {
        [Get("/fake/url/")]
        Task<string> TestMethod();
    }
}