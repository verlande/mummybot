using System;

namespace mummybot.Models
{
    public class Blacklist
    {
        public int Id { get; }
        public ulong UserId { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; } 
    }
}