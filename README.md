# GrammarErrorDetection.API

Backend para un sistema de detección de errores gramaticales. Su propósito es ofrecer una API HTTP para recibir texto, coordinar su análisis y devolver los resultados mediante respuestas estructuradas.

El sistema web consiste en un sistema de gestión de notas de estudiantes y cursos en el centro de Enseñanza y Traducción de Idiomas (CETI) ubicado en La Paz, Bolivia. Los estudiantes pueden subir oraciones para detectar errores gramaticales mediante técnicas y herramientas de procesamiento de lenguaje natural NLP.

## Tecnologías

- **C# y .NET 10**: lenguaje y plataforma de los proyectos.
- **ASP.NET Core Web API**: aplicación HTTP configurada para usar controladores.
- **Microsoft.AspNetCore.OpenApi 10.0.9**: generación del documento OpenAPI.
- **Bibliotecas de clases .NET**: separación de modelos, servicios y repositorios.
- **Git**: control de versiones.

## Estructura

```text
GrammarErrorDetection.API/
├── GrammarErrorDetection.API.slnx
├── GrammarErrorDetection.API/           # Aplicación web y configuración
├── GrammarErrorDetection.Models/        # Modelos compartidos
├── GrammarErrorDetection.Repositories/  # Capa prevista para acceso a datos
├── GrammarErrorDetection.Services/      # Capa prevista para lógica de negocio
├── .env.example
├── .gitignore
└── README.md
```

Actualmente no hay referencias entre los proyectos. La solución `.slnx` todavía no contiene proyectos; los comandos siguientes utilizan directamente el proyecto de la API.

## Requisitos

- [Git](https://git-scm.com/downloads).
- [SDK de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0). El runtime por sí solo no permite compilar.

Comprueba las herramientas:

```powershell
git --version
dotnet --version
```

## Clonar el repositorio

```powershell
git clone https://github.com/green-dv/GrammarErrorDetection_API.git GrammarErrorDetection.API
cd GrammarErrorDetection.API
```

## Configurar el entorno

Desde la raíz del repositorio, copia el archivo de ejemplo:

```powershell
Copy-Item .env.example .env
```

En CMD, el comando equivalente es:

```bat
copy .env.example .env
```

Edita `.env` para ajustar el entorno, las direcciones de escucha y los niveles de logging:

| Variable | Propósito |
| --- | --- |
| `ENVIRONMENT` | Selecciona el entorno. `Development` habilita swagger. |
| `ASPNETCORE_URLS` | Direcciones HTTP y HTTPS en las que escucha la API. |
| `JWT_SECRET` | Secreto para la generación de Json WEB Tokens. |
| `DB_NAME` | Nombre de la base de datos. |
| `DB_PASSWORD` | Contraseña de la base de datos. |
| `DB_USER` | Usuario de la base de datos. |
| `DB_PORT` | Puerto de la base de datos. |
| `GMAIL_EMAIL` | Email que enviará los correos electrónicos. |
| `GMAIL_APPPASSWORD` | Contraseña de aplicación para enviar correos por gmail. |


Este bloque admite el formato sencillo del ejemplo: una variable por línea, valores sin comillas y comentarios en líneas independientes. Las variables se aplican a la sesión actual y a la API que inicies desde ella. Vuelve a importarlas si editas `.env` o abres otra terminal.

## Ejecutar la API

Desde la raíz del repositorio y después de cargar las variables:

```powershell
dotnet restore .\GrammarErrorDetection.API\GrammarErrorDetection.API.csproj
dotnet dev-certs https --trust
dotnet run --project .\GrammarErrorDetection.API\GrammarErrorDetection.API.csproj --no-launch-profile
```

El comando de certificados se utiliza para preparar HTTPS en el equipo local. `--no-launch-profile` permite usar las variables importadas sin aplicar los perfiles de `launchSettings.json`.

Con los valores de ejemplo:

- API: [https://localhost:7274](https://localhost:7274).
- Dirección HTTP: [http://localhost:5091](http://localhost:5091), con redirección a HTTPS.
- Documento OpenAPI en desarrollo: [https://localhost:7274/swagger/](https://localhost:7274/swagger/).

Para detener la aplicación, presiona `Ctrl+C`.

## Compilar

```powershell
dotnet build .\GrammarErrorDetection.API\GrammarErrorDetection.API.csproj
```

Este comando compila la API. Útil para comprobar si existen errores de compilación antes de iniciar la API.
