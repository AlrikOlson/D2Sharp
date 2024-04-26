FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install GCC
RUN apt-get update && apt-get install -y gcc

# Install Go 1.22.2
RUN apt-get install -y wget && \
    wget https://dl.google.com/go/go1.22.2.linux-amd64.tar.gz && \
    tar -C /usr/local -xzf go1.22.2.linux-amd64.tar.gz && \
    rm go1.22.2.linux-amd64.tar.gz

ENV PATH=$PATH:/usr/local/go/bin

WORKDIR /src
COPY ["src/d2.Net.ConsoleApp/d2.Net.ConsoleApp.csproj", "src/d2.Net.ConsoleApp/"]
RUN dotnet restore "src/d2.Net.ConsoleApp/d2.Net.ConsoleApp.csproj"
COPY . .
WORKDIR "/src/src/d2.Net.ConsoleApp"
RUN dotnet build "d2.Net.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "d2.Net.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "d2.Net.ConsoleApp.dll"]
