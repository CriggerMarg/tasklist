using System.ComponentModel.DataAnnotations;

namespace Tasklist.Background
{
    public class ProcessInformation
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public float CPULoad { get; set; }

        public override string ToString()
        {
            return $"{Name} {CPULoad}";
        }
    }
}