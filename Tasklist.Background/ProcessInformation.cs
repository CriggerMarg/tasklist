namespace Tasklist.Background
{
    /// <summary>
    /// Holds information about process. Provides process name with it's cpu load
    /// </summary>
    public class ProcessInformation
    {
        public ProcessInformation(string name, float cpuLoad)
        {
            Name = name;
            CPULoad = cpuLoad;
        }

        public string Name { get; }
        public float CPULoad { get; }
    }
}