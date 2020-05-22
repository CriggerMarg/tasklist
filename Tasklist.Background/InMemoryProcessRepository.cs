using System.Collections.Generic;

using Tasklist.Models;

namespace Tasklist.Background
{
    public class InMemoryProcessRepository : IProcessRepository
    {
        private IReadOnlyCollection<ProcessInformation> _processes = new List<ProcessInformation>();

        private static readonly object _locker = new object();


        public IReadOnlyCollection<ProcessInformation> ProcessInformation
        {
            get
            {
                lock (_locker)
                { return _processes; }
            }
            set { lock (_locker) { _processes = value; } }
        }
    }
}
