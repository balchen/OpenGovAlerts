#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
# BEGIN MODIFICATION - Node is needed for development (but not production)
RUN curl -sL https://deb.nodesource.com/setup_18.x | bash -
RUN apt install --assume-yes nodejs
#RUN apt-get install --assume-yes npm
# END MODIFICATION
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["OpenGovAlerts/OpenGovAlerts.csproj", "OpenGovAlerts/"]
COPY ["OpenGov/OpenGov.csproj", "OpenGov/"]
RUN dotnet restore "OpenGovAlerts/OpenGovAlerts.csproj"
COPY . .
WORKDIR "/src/OpenGovAlerts"
RUN dotnet build "OpenGovAlerts.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OpenGovAlerts.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenGovAlerts.dll"]