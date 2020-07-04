FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build

COPY . /mummybot
WORKDIR /mummybot
RUN set -ex; \
    dotnet add src/mummybot.csproj package NRuneScape --version 0.1.0 --source https://www.myget.org/F/nrunescape/api/v3/index.json; \	
    dotnet restore; \ 
    dotnet build -c Release; \
    dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime
WORKDIR /app

COPY --from=build /app /app
COPY src/_config.json /app
COPY src/NLog.config /app

ENTRYPOINT [ "dotnet", "mummybot.dll" ]
