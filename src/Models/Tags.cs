using System;

namespace mummybot.Models
{
    public class Tags
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public ulong Author { get; set; }
        public ulong Guild { get; set; }
        public DateTime? Createdat { get; set; }
        public bool IsCommand { get; set; } = false;
        public int Uses { get; set; } = 0;
        public ulong? LastUsedBy { get; set; }
        public DateTime? LastUsed { get; set; }
    }
}
