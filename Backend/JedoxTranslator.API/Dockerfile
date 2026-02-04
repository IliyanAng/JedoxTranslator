FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["JedoxTranslator.API/JedoxTranslator.API.csproj", "JedoxTranslator.API/"]
COPY ["JedoxTranslator.Core/JedoxTranslator.Core.csproj", "JedoxTranslator.Core/"]
RUN dotnet restore "JedoxTranslator.API/JedoxTranslator.API.csproj"

COPY . .
WORKDIR "/src/JedoxTranslator.API"
RUN dotnet build "JedoxTranslator.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JedoxTranslator.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JedoxTranslator.API.dll"]
