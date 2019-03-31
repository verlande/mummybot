FROM microsoft/dotnet:2.1-sdk-alpine AS build

COPY . /mummybot
WORKDIR /mummybot
RUN set -ex; \
    dotnet restore; \
    dotnet build -c Release; \
    dotnet publish -c Release -o /app

FROM microsoft/dotnet:2.1-runtime-alpine AS runtime
WORKDIR /app
COPY --from=build /app /app
ENTRYPOINT [ "dotnet", "mummybot.dll" ]