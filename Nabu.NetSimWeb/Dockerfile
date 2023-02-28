#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 5816

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Nabu.NetSimWeb/Nabu.NetSimWeb.csproj", "Nabu.NetSimWeb/"]
RUN dotnet restore "Nabu.NetSimWeb/Nabu.NetSimWeb.csproj"
COPY . .
WORKDIR "/src/Nabu.NetSimWeb"
RUN dotnet build "Nabu.NetSimWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nabu.NetSimWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/Assets/PAKs/cycle1/raw PAKs/cycle1/
COPY --from=build /src/Assets/PAKs/cycle2/raw PAKs/cycle2/
COPY --from=build /src/Assets/PAKs/cycle2ex/raw PAKs/cycle2ex/
COPY --from=build /src/Assets/Config/appsettings.docker.json appsettings.json
# These are user file folders...
RUN mkdir /app/NABUs
RUN touch /app/NABUs/.placeholder
RUN mkdir /app/Files
RUN touch /app/Files/.placeholder

# These are folders user by NetSim
RUN mkdir /app/cache
RUN touch /app/cache/.placeholder
RUN mkdir /app/logs
RUN touch /app/logs/.placeholder
ENTRYPOINT ["dotnet", "nns-wui.dll"]