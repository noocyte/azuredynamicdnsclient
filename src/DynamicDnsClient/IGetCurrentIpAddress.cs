using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    public interface IGetCurrentIpAddress
    {
        Task<string> GetCurrentIdAsync(CancellationToken cancellationToken);
    }
}
