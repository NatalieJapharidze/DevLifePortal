FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["DevLife.API.csproj", "."]
RUN dotnet restore "DevLife.API.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "DevLife.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DevLife.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DevLife.API.dll"]