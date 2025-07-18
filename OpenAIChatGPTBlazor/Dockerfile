#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["OpenAIChatGPTBlazor/OpenAIChatGPTBlazor.csproj", "OpenAIChatGPTBlazor/"]
RUN dotnet restore "OpenAIChatGPTBlazor/OpenAIChatGPTBlazor.csproj"
COPY . .
WORKDIR "/src/OpenAIChatGPTBlazor"
RUN dotnet build "OpenAIChatGPTBlazor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OpenAIChatGPTBlazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenAIChatGPTBlazor.dll"]