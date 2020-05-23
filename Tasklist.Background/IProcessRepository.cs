using System.Collections.Generic;

namespace Tasklist.Background
{
    public interface IProcessRepository
    {
        IReadOnlyCollection<ProcessInformation> ProcessInformation { get; set; }
    }
}
