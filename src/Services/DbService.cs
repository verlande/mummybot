using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NadekoBot.Core.Services.Database;
using System;
using System.IO;
using System.Linq;

namespace mummybot.Services
{
    public class DbService
    {
        private readonly DbContextOptions<mummybotDbContext> options;
        private readonly DbContextOptions<mummybotDbContext> migrateOptions;

        private static readonly ILoggerFactory _loggerFactory = new LoggerFactory(new[] {
            new ConsoleLoggerProvider((category, level)
                => category == DbLoggerCategory.Database.Command.Name
                   && level >= LogLevel.Information, true)
            });

        private mummybotDbContext GetDbContextInternal()
        {
            var context = new mummybotDbContext(options);
            context.Database.SetCommandTimeout(60);
            var conn = context.Database.GetDbConnection();
            conn.Open();
            return context;
        }

        public IUnitOfWork GetDbContext() => new UnitOfWork(GetDbContextInternal());
    }
}
