using mummybot.Services;
using System;
using System.Threading.Tasks;
using NRuneScape.RuneScape3;

namespace mummybot.Modules.Runescape.Services
{
    public class StatsService : INService
    {
        private readonly RS3RestClient RS3 = new RS3RestClient();

        private IServiceProvider _services;

        public void Initialize(IServiceProvider service)
            => _services = service;

        public async Task<RS3HiscoreCharacter> GetPlayerStats(string name)
        {
            return await RS3.GetCharacterAsync(name).ConfigureAwait(false);
        }
    }
}
