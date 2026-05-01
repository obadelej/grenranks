FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["backend/TrackRank.Api/TrackRank.Api.csproj", "backend/TrackRank.Api/"]
RUN dotnet restore "backend/TrackRank.Api/TrackRank.Api.csproj"

COPY . .
RUN dotnet publish "backend/TrackRank.Api/TrackRank.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "TrackRank.Api.dll"]
