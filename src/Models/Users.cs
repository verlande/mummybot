using System;

namespace mummybot.Models
{
    public class Users
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public ulong GuildId { get; set; }
        public bool TagBanned { get; set; }
        public DateTime Joined { get; set; }
    }
}