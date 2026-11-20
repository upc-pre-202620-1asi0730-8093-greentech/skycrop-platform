FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY GreenTech.SkyCrop.Platform/*.csproj GreenTech.SkyCrop.Platform/
RUN dotnet restore ./GreenTech.SkyCrop.Platform
COPY . .
RUN dotnet publish ./GreenTech.SkyCrop.Platform -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /app/out .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GreenTech.SkyCrop.Platform.dll"]