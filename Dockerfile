# multi-stage build for cross-platform compatibility
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csproj(s) and restore
COPY api-blog-comments-dev/*.csproj ./api-blog-comments-dev/
RUN dotnet restore "api-blog-comments-dev/api-blog-comments-dev.csproj"

# copy everything else and publish
COPY . .
WORKDIR "/src/api-blog-comments-dev"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# bind to a non-privileged port for production-oriented containers
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "api-blog-comments-dev.dll"]