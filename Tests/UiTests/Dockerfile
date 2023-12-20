#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Tests/UiTests/UiTests.csproj", "Tests/UiTests/"]
RUN dotnet restore "./Tests/UiTests/./UiTests.csproj"
COPY . .
WORKDIR "/src/Tests/UiTests"
RUN dotnet build "./UiTests.csproj" -c $BUILD_CONFIGURATION -o /app/build

#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./UiTests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
# Installing global tools
run dotnet tool install --global Microsoft.Playwright.CLI
#run /root/.dotnet/tools/playwright install

WORKDIR "/app/build"
#Install playwright dependencies
RUN pwsh ./playwright.ps1 install
RUN pwsh ./playwright.ps1 install chromium
RUN pwsh ./playwright.ps1 install-deps chromium

#FROM base AS final
#WORKDIR /app
#COPY --from=build /app/build .

WORKDIR "/src/Tests/UiTests"
ENTRYPOINT ["dotnet", "test"]