FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["GoodHamburgerAPI.Domain/GoodHamburgerAPI.Domain.csproj", "GoodHamburgerAPI.Domain/"]
COPY ["GoodHamburgerAPI.Infrastructure/GoodHamburgerAPI.Infrastructure.csproj", "GoodHamburgerAPI.Infrastructure/"]
COPY ["GoodHamburgerAPI.Application/GoodHamburgerAPI.Application.csproj", "GoodHamburgerAPI.Application/"]
COPY ["GoodHamburgerAPI.WebAPI/GoodHamburgerAPI.WebAPI.csproj", "GoodHamburgerAPI.WebAPI/"]

RUN dotnet restore "GoodHamburgerAPI.WebAPI/GoodHamburgerAPI.WebAPI.csproj"

COPY . .

WORKDIR "/src/GoodHamburgerAPI.WebAPI"
RUN dotnet build "GoodHamburgerAPI.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GoodHamburgerAPI.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "GoodHamburgerAPI.WebAPI.dll"]
