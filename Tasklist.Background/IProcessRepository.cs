using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Tasklist.Background
{
    public interface IProcessRepository
    {
        IReadOnlyCollection<ProcessInformation> ProcessInformation { get; }
        Task SetProcessInfo(IReadOnlyCollection<ProcessInformation> info);
        bool IsCpuHigh { get; set; }
        bool IsMemoryLow { get; set; }
        void AddSocket(WebSocket socket, TaskCompletionSource<object> tcs);
    }
}
