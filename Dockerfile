FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build

COPY . /mummybot
WORKDIR /mummybot
RUN set -ex; \
    dotnet add src/mummybot.csproj package NRuneScape --version 0.1.0 --source https://www.myget.org/F/nrunescape/api/v3/index.json; \	
    dotnet restore; \ 
    dotnet build -c Release; \
    dotnet publish -c Release -o /app

FROM microsoft/dotnet:3.0-runtime AS runtime
WORKDIR /app
COPY --from=build /app /app
ENTRYPOINT [ "dotnet", "mummybot.dll" ]