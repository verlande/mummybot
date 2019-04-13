using System;

namespace mummybot.Models
{
    public class UsersAudit
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public ulong GuildId { get; set; }
        public DateTime? ChangedOn { get; set; }
    }
}