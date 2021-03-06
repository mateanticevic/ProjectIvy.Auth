FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src

COPY . .
WORKDIR "/src/src/ProjectIvy.Auth"
RUN dotnet build "ProjectIvy.Auth.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "ProjectIvy.Auth.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ProjectIvy.Auth.dll"]

