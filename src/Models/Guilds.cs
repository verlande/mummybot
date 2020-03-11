namespace mummybot.Models
{
    public class Guilds
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public string GuildName { get; set; }
        public ulong OwnerId { get; set; }
        public bool Active { get; set; }
        public string Region { get; set; }
        public string Greeting { get; set; }
        public string Goodbye { get; set; }
        public ulong? GreetChl { get; set; }
    	public bool FilterInvites { get; set; }
        public string Regex { get; set; }
    }
}
