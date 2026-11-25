FROM node:24-alpine AS client
WORKDIR /src/client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY server/*.csproj server/
RUN dotnet restore server
COPY server/ server/
COPY --from=client /src/client/dist/ server/wwwroot/
RUN dotnet publish server --no-restore -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out/ .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DraftSpace.Api.dll"]
