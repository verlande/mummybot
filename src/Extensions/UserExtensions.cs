using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mummybot.Extensions
{
    public static class UserExtensions
    {
        public static Uri RealAvatarUrl(this IUser usr, int size = 0)
        {
            var append = size <= 0
            ? "" : $"?size={size}";

            return usr.AvatarId == null
                ? null
                : new Uri(usr.AvatarId.StartsWith("a_", StringComparison.InvariantCulture)
                    ? $"{DiscordConfig.CDNUrl}avatars/{usr.Id}/{usr.AvatarId}.gif" + append
                    : usr.GetAvatarUrl(ImageFormat.Auto) + append);
        }

        public static async Task<IEnumerable<IGuildUser>> GetMembersAsync(this IRole role) =>
            (await role.Guild.GetUsersAsync(CacheMode.AllowDownload).ConfigureAwait(false)).Where(u => u.RoleIds.Contains(role.Id)) ?? Enumerable.Empty<IGuildUser>();

        public static IRole HightestRole(this IGuildUser user) 
            => user.RoleIds.Select(x => user.Guild.GetRole(x)).OrderByDescending(x => x.Position).FirstOrDefault();
    }
}
