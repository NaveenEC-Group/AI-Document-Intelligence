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
COPY entrypoint.sh /app/entrypoint.sh

RUN mkdir -p /app/data \
    && chmod +x /app/entrypoint.sh

ENV ASPNETCORE_ENVIRONMENT=Production
ENV Database__Provider=Sqlite
ENV ConnectionStrings__DefaultConnection=Data Source=/app/data/aidocs.db
ENV PORT=8080

EXPOSE 8080

ENTRYPOINT ["/app/entrypoint.sh"]
