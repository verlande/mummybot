FROM microsoft/dotnet:3.0.100-preview2-sdk-alpine3.8 AS build

COPY . /mummybot
WORKDIR /mummybot
RUN set -ex; \
    dotnet restore; \
    dotnet build -c Release; \
    dotnet publish -c Release -o /app

FROM microsoft/dotnet:3.0-runtime-alpine AS runtime
WORKDIR /app
COPY --from=build /app /app
ENTRYPOINT [ "dotnet", "mummybot.dll" ]
