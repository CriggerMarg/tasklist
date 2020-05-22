using System.Collections.Generic;
using Tasklist.Models;

namespace Tasklist.Background
{
    public interface IProcessRepository
    {
        IReadOnlyCollection<ProcessInformation> ProcessInformation { get; set; }
    }
}
