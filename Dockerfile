# API ASP.NET Core (.NET 10). Se construye desde la raiz del repo: docker build -t grammar-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero los .csproj, para que el restore se cachee entre builds.
COPY GrammarErrorDetection.Models/*.csproj GrammarErrorDetection.Models/
COPY GrammarErrorDetection.Infrastructure/*.csproj GrammarErrorDetection.Infrastructure/
COPY GrammarErrorDetection.Repositories/*.csproj GrammarErrorDetection.Repositories/
COPY GrammarErrorDetection.Services/*.csproj GrammarErrorDetection.Services/
COPY GrammarErrorDetection.API/*.csproj GrammarErrorDetection.API/
RUN dotnet restore GrammarErrorDetection.API/GrammarErrorDetection.API.csproj

COPY GrammarErrorDetection.Models/ GrammarErrorDetection.Models/
COPY GrammarErrorDetection.Infrastructure/ GrammarErrorDetection.Infrastructure/
COPY GrammarErrorDetection.Repositories/ GrammarErrorDetection.Repositories/
COPY GrammarErrorDetection.Services/ GrammarErrorDetection.Services/
COPY GrammarErrorDetection.API/ GrammarErrorDetection.API/
RUN dotnet publish GrammarErrorDetection.API/GrammarErrorDetection.API.csproj \
    -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# /logs: logs en archivo (LOG_DIR). /data/support-materials: copia local de los materiales subidos.
# Se crean aqui con el dueno correcto para que los volumenes nombrados hereden el permiso.
RUN mkdir -p /logs /data/support-materials && chown -R app:app /logs /data
COPY --from=build /out .
USER app
# La imagen ya escucha en el 8080 (ASPNETCORE_HTTP_PORTS). Solo HTTP: el TLS lo termina el proxy.
ENV ASPNETCORE_ENVIRONMENT=Production \
    LOG_DIR=/logs
EXPOSE 8080
ENTRYPOINT ["dotnet", "GrammarErrorDetection.API.dll"]
