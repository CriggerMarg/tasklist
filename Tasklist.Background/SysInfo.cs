using System;

namespace Tasklist.Background
{
    /// <summary>
    /// Structure to pass system info
    /// </summary>
    public struct SysInfo : IEquatable<SysInfo>
    {
        
        public bool HighCpu { get; set; }
        public bool LowMemory { get; set; }

        public bool Equals(SysInfo other)
        {
            return HighCpu == other.HighCpu && LowMemory == other.LowMemory;
        }

        public override bool Equals(object obj)
        {
            return obj is SysInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(HighCpu, LowMemory);
        }
    }
}