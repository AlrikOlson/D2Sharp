# Stage 1: Build Go wrapper
FROM golang:1.22-alpine AS go-builder

RUN apk add --no-cache gcc musl-dev

WORKDIR /build
COPY src/D2Sharp/d2wrapper/ ./

RUN CGO_ENABLED=1 go build -trimpath -buildmode=c-shared -ldflags "-s" -o d2wrapper.so .

# Stage 2: Build .NET application
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS dotnet-builder

WORKDIR /src

# Copy solution and project files
COPY D2Sharp.sln ./
COPY src/D2Sharp/D2Sharp.csproj ./src/D2Sharp/
COPY examples/D2Sharp.Web/D2Sharp.Web.csproj ./examples/D2Sharp.Web/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/D2Sharp/ ./src/D2Sharp/
COPY examples/D2Sharp.Web/ ./examples/D2Sharp.Web/

# Copy the built Go library
COPY --from=go-builder /build/d2wrapper.so ./src/D2Sharp/bin/Release/net8.0/d2wrapper.so

# Build the application
RUN dotnet publish examples/D2Sharp.Web/D2Sharp.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Install runtime dependencies for the Go shared library
RUN apk add --no-cache libgcc libstdc++

WORKDIR /app

# Copy published application
COPY --from=dotnet-builder /app/publish .

# Copy the Go library to the app directory
COPY --from=go-builder /build/d2wrapper.so .

# Create non-root user
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser && \
    chown -R appuser:appuser /app

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "D2Sharp.Web.dll"]
