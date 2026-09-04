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
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection=Data Source=/app/data/aidocs.db

EXPOSE 8080

ENTRYPOINT ["dotnet", "AI Document Intelligence.dll"]
