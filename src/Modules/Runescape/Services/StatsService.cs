﻿using mummybot.Services;
using System;
using System.Threading.Tasks;
using NRuneScape.RuneScape3;

namespace mummybot.Modules.Runescape.Services
{
    public class StatsService : INService
    {
        private readonly RS3RestClient _rs3 = new RS3RestClient();

        private IServiceProvider _services;

        public void Initialize(IServiceProvider service)
            => _services = service;

        public async Task<RS3HiscoreCharacter> GetPlayerStats(string name) 
            => await _rs3.GetCharacterAsync(name).ConfigureAwait(false);
    }
}
