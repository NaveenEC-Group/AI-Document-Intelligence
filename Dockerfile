# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /src

COPY ["AI Document Intelligence.csproj", "./"]
RUN dotnet restore "AI Document Intelligence.csproj"

COPY . .
RUN dotnet publish "AI Document Intelligence.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=builder /app/publish .

RUN mkdir -p /app/data

ENV ASPNETCORE_ENVIRONMENT=Production
ENV Database__Provider=Sqlite
ENV PORT=8080

EXPOSE 8080

# Bind to Render's $PORT. Keep SQLite path without broken Docker ENV quoting.
CMD ["sh", "-c", "mkdir -p /app/data && export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} && export Database__Provider=Sqlite && export ConnectionStrings__DefaultConnection='Data Source=/app/data/aidocs.db' && echo Starting on $ASPNETCORE_URLS && exec dotnet 'AI Document Intelligence.dll'"]
