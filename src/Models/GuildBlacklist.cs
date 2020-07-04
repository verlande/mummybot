using System;

namespace mummybot.Models
{
    public class GuildBlacklist
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}