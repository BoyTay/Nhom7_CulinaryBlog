FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

COPY CulinaryBlog.sln Directory.Build.props Directory.Packages.props global.json ./
COPY src/CulinaryBlog.Domain/CulinaryBlog.Domain.csproj src/CulinaryBlog.Domain/
COPY src/CulinaryBlog.Application/CulinaryBlog.Application.csproj src/CulinaryBlog.Application/
COPY src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj src/CulinaryBlog.Infrastructure/
COPY src/CulinaryBlog.API/CulinaryBlog.API.csproj src/CulinaryBlog.API/
RUN dotnet restore src/CulinaryBlog.API/CulinaryBlog.API.csproj

COPY src/ src/
RUN dotnet publish src/CulinaryBlog.API/CulinaryBlog.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache krb5-libs wget \
    && addgroup --system culinary \
    && adduser --system --ingroup culinary culinary
COPY --from=build --chown=culinary:culinary /app/publish .
USER culinary
EXPOSE 8080
ENTRYPOINT ["dotnet", "CulinaryBlog.API.dll"]
