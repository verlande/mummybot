using System;
using Discord;
using Discord.WebSocket;

namespace mummybot
{
    public class Utils
    {
        public static Color GetRandomColor()
        {
            var r = new Random();
            return new Color((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255));
        }

        public static string FullUserName(SocketUser user) 
            => $"{user.Username}#{user.Discriminator}";
    }
}