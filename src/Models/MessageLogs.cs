using System;

namespace mummybot
{
    public partial class MessageLogs
    {
        public long Id { get; set; }
        public ulong Guildid { get; set; }
        public ulong Messageid { get; set; }
        public ulong Authorid { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public string Channelname { get; set; }
        public ulong Channelid { get; set; }
        public string Content { get; set; }
        public string Attachments { get; set; }
        public string[] Mentionedusers { get; set; }
        public DateTime Createdat { get; set; }
        public bool Deleted { get; set; }
        public DateTime? Deletedat { get; set; }
        public string UpdatedContent { get; set; }
    }
}
