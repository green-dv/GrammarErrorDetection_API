# GrammarErrorDetection.API

Backend para un sistema de detección de errores gramaticales. Su propósito es ofrecer una API HTTP para recibir texto, coordinar su análisis y devolver los resultados mediante respuestas estructuradas.

El sistema web consiste en un sistema de gestión de notas de estudiantes y cursos en el centro de Enseñanza y Traducción de Idiomas (CETI) ubicado en La Paz, Bolivia. Los estudiantes pueden subir oraciones para detectar errores gramaticales mediante técnicas y herramientas de procesamiento de lenguaje natural NLP.

## Tecnologías

- **C# y .NET 10**: lenguaje y plataforma de los proyectos.
- **ASP.NET Core Web API**: aplicación HTTP configurada para usar controladores.
- **Microsoft.AspNetCore.OpenApi 10.0.9**: generación del documento OpenAPI.
- **Bibliotecas de clases .NET**: separación de modelos, servicios, repositorios e infraestructura.
- **Npgsql 10.0.3**: acceso a PostgreSQL mediante procedimientos almacenados.
- **Git**: control de versiones.

## Estructura

```text
GrammarErrorDetection.API/
├── GrammarErrorDetection.API.slnx
├── GrammarErrorDetection.API/           # Aplicación web y configuración
├── GrammarErrorDetection.Models/        # Modelos compartidos
├── GrammarErrorDetection.Repositories/  # Capa prevista para acceso a datos
├── GrammarErrorDetection.Services/      # Capa prevista para lógica de negocio
├── GrammarErrorDetection.Infrastructure/ # Acceso a PostgreSQL y APIs externas
├── .env.example
├── .gitignore
└── README.md
```

La solución contiene los cinco proyectos. Repositories referencia Models e Infrastructure; Infrastructure concentra las llamadas HTTP y la ejecución de SP. Consulta [su documentación](GrammarErrorDetection.Infrastructure/README.md) para conocer los contratos y ejemplos de uso.

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
| `VERIFICATION_CODE_EXPIRATION_MINUTES` | Duracion del codigo de verificacion en minutos; entero positivo, 10 por defecto. |
| `GMAIL_EMAIL` | Email que enviará los correos electrónicos. |
| `GMAIL_APPPASSWORD` | Contraseña de aplicación para enviar correos por gmail. |


Este bloque admite el formato sencillo del ejemplo: una variable por línea, valores sin comillas y comentarios en líneas independientes. Las variables se aplican a la sesión actual y a la API que inicies desde ella. Vuelve a importarlas si editas `.env` o abres otra terminal.

## Ejecutar la API

La insercion de un codigo de verificacion calcula `ExpirationTime` en el servidor usando
`VERIFICATION_CODE_EXPIRATION_MINUTES`; no es necesario enviarlo en el JSON y cualquier
valor recibido para ese campo se ignora. Reinicia la API despues de editar `.env`.
Las fechas UTC o con offset de `ExpirationTime` y `ConsumedAt` se convierten a hora de
Bolivia antes de escribirlas en PostgreSQL (`timestamp without time zone`).
Una fecha sin zona se interpreta como hora de Bolivia. En Update, una expiracion
no nula sigue permitiendo modificar la fecha; `NULL` conserva el valor actual.

Pruebas de conversion de fechas y expiracion configurable:

```powershell
dotnet run --project tests/VerificationCodeChecks
```

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

## CRUD de usuarios, códigos de verificación y roles

Los recursos `/api/User`, `/api/VerificationCode` y `/api/Role` admiten:

- `GET`: listado con `PageNumber`, `PageSize` y filtros mediante query string (por ejemplo, `/api/Role?PageNumber=1&PageSize=10&Filter.Name=Admin`).
- `POST`: inserción con el modelo `Insert` correspondiente en el cuerpo JSON; devuelve HTTP 201.
- `PUT`: actualización con el modelo `Update` correspondiente en el cuerpo JSON; devuelve HTTP 200.

No hay operaciones DELETE. Los services devuelven entidades o listas; los controllers construyen `Wrapper<T>` con `Data` y, en listados, `Count` (cantidad de elementos de la página). Los errores los gestiona el middleware existente.

El CRUD `/api/Evaluation` administra las evaluaciones por inscripción y periodo,
con acceso para coordinadores. `ModuleName` incluye la relación opcional
`NextModuleId`. Consulta [evaluaciones y siguiente módulo](docs/evaluations-next-module.md)
para los contratos, ejemplos y el script PostgreSQL que debe aplicarse antes de usarlos.

El listado administrativo `GET /api/User/summary` agrega curso actual, último curso
y nota final del último curso. Consulta [el contrato de User_List_Summary](docs/user-summary.md)
para los filtros, columnas del cursor y script SQL correspondiente.

Configura la conexión PostgreSQL mediante `ConnectionStrings:Grammar` en la configuración de ASP.NET Core o la variable de entorno `ConnectionStrings__Grammar`, por ejemplo:

```powershell
$env:ConnectionStrings__Grammar = 'Host=localhost;Port=5432;Database=grammar;Username=postgres;Password=tu-clave'
```

## Compilar la solución

```powershell
dotnet build .\GrammarErrorDetection.API\GrammarErrorDetection.API.csproj
```

Este comando compila la API. Útil para comprobar si existen errores de compilación antes de iniciar la API.

Para compilar todos los proyectos de la solución:

```powershell
dotnet build .\GrammarErrorDetection.API.slnx
```
