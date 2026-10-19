
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY StudentFeedback.csproj ./
RUN dotnet restore StudentFeedback.csproj

COPY . ./
RUN dotnet publish StudentFeedback.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "StudentFeedback.dll"]
