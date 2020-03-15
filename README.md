# mummybot

[![pipeline status](https://gitlab.com/mummy603/mummybot/badges/dev/pipeline.svg)](https://gitlab.com/mummy603/mummybot/commits/dev)

Just another Discord Bot

##### Built With
   [Discord.NET](https://github.com/RogueException/Discord.Net)
*  [PostgreSQL](https://www.postgresql.org/)
*  [EntityFramework Core](https://docs.microsoft.com/en-us/ef/core/)
*  [Docker](https://docker.com)

## Self Hosting With Docker
```bash
# Clone repository
git clone https://gitlab.com/mummy603/mummybot.git

# Enter directory
cd mummybot

# Edit src/_config.json with your bot token and prefix
{
  "token": "{YOUR_BOT_TOKEN}",
  "prefix": "£",
}

# Edit docker-compose.yml environment variables
DB_CONNECTION_STRING: "host=postgres_image;port=5432;database=mummybot;username=postgres;password={YOUR_PASSWORD};"
POSTGRES_PASSWORD: {YOUR_PASSWORD}

# Docker compose and daemon
docker-compose up -d
```