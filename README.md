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

Consulta [el flujo de autenticación](docs/authentication.md) para los contratos
de login, envío de códigos compartido con signup y verificación por correo o TOTP.

Los recursos `/api/VerificationCode` y `/api/Role` admiten:

- `GET`: listado con `PageNumber`, `PageSize` y filtros mediante query string (por ejemplo, `/api/Role?PageNumber=1&PageSize=10&Filter.Name=Admin`).
- `POST`: inserción con el modelo `Insert` correspondiente en el cuerpo JSON; devuelve HTTP 201.
- `PUT`: actualización con el modelo `Update` correspondiente en el cuerpo JSON; devuelve HTTP 200.

No hay operaciones DELETE. Los services devuelven entidades o listas; los controllers construyen `Wrapper<T>` con `Data` y, en listados, `Count` (cantidad de elementos de la página). Los errores los gestiona el middleware existente.

El CRUD `/api/Evaluation` administra las evaluaciones por inscripción y periodo,
con acceso para profesores (`Teacher`) y coordinadores (`Coordinator`) en GET,
POST y PUT. `ModuleName` incluye la relación opcional
`NextModuleId`. Consulta [evaluaciones y siguiente módulo](docs/evaluations-next-module.md)
para los contratos, ejemplos y el script PostgreSQL que debe aplicarse antes de usarlos.

`GET /api/Evaluation?PageNumber=1&PageSize=20&Filter.UserId=UUID_ESTUDIANTE`
permite filtrar evaluaciones por estudiante. `Filter.UserId` es opcional, de tipo UUID,
y se envía como parámetro `UserId` a `Evaluation_List`. El cursor devuelve `UserId`
y cada elemento del listado lo expone como `userId`, junto con los campos existentes.
El controller y el servicio del CRUD propagan ese filtro y resultado con el mismo
formato `Wrapper<T>`. `POST /api/Evaluation` y `PUT /api/Evaluation` conservan sus
cuerpos, llamadas a SP y respuestas: no reciben ni devuelven `userId`.
El SQL se administra externamente; no se crean scripts ni se modifica la BD desde la API.

El listado `POST /api/User/search` recibe paginación y filtros en el cuerpo JSON e incluye el nombre del rol, curso actual, promedio
actual, horario, docente, aula y último curso. El filtro `Filter.CurrentModuleNameId`
selecciona estudiantes que cursan el módulo. Consulta [el contrato de User_List](docs/user-summary.md).
El periodo actual se calcula en PostgreSQL con hora de Bolivia.
`Filter.ClassId` permite consultar los estudiantes inscritos en un curso concreto
mediante `POST /api/User/search`. Requiere ampliar `grammar."User_List"` con
el parámetro opcional `ClassId uuid` después de `CurrentModuleNameId` y filtrar
por inscripciones activas del curso antes de paginar. El contrato y el fragmento
SQL están en [User_List](docs/user-summary.md); el cambio no se aplica a la BD automáticamente.
La creación de usuarios sigue en `POST /api/User` y la actualización en `PUT /api/User`.

`GET /api/Class` incluye nombres de docente, módulo y aula, nivel del módulo,
capacidad del aula, cantidad de estudiantes y promedios de los dos exámenes.
Consulta [el contrato de Class_List](docs/class-list.md).

`PUT /api/Class/with-students` permite a `Coordinator` editar un curso y sincronizar
sus estudiantes. Recibe los mismos datos completos de `POST /api/Class/with-students`,
más `id`. `studentIds` es obligatorio y representa la lista final: conserva las
inscripciones incluidas, agrega a los nuevos y desactiva las omitidas. Una lista
vacía quita a todos; omitirla o enviar null se rechaza. La respuesta incluye el
curso, las asignaciones finales y `removedRegistrationIds`. No permite cambiar
módulo, año o bimestre mientras existan inscripciones activas. Reutiliza los CRUD
existentes, sin una transacción global. Consulta [asignación manual](docs/manual-class-assignment.md).

`GET /api/Class/eligible-students` admite `Filter.ClassId` opcional para consultar
desde la edición de un curso. Si todas sus inscripciones activas que reservan cupo
tienen los dos exámenes completos y válidos, devuelve una lista vacía. El curso debe
coincidir con el módulo, año y bimestre del filtro. Sin `ClassId` se conserva la
búsqueda para crear cursos. Un curso vacío o con notas pendientes no se considera
terminado. Esta validación del listado no bloquea por sí sola las actualizaciones.

## Solicitudes de inscripción

El historial de cursos se consulta con
`GET /api/Registration/mine/courses?PageNumber=1&PageSize=20` (Student, identidad del JWT)
o `GET /api/Registration/students/{studentId}/courses?PageNumber=1&PageSize=20`
(Coordinator). Ambos devuelven `Wrapper<List<StudentCourseHistoryDto>>` con
`registrationId`, `classId`, `moduleId`, `moduleName`, `registrationDate`,
`teacherName`, `exam1Score`, `exam2Score`, `parallel`, `startTime`, `endTime`,
`year`, `bimester` y `status`. El orden es fecha de inscripción descendente
(más reciente primero), con fechas nulas al final; los empates se resuelven
por UpdatedAt e ID descendentes. La paginación se aplica después de ordenar.
PageNumber y PageSize son opcionales (1 y 20 por defecto), ambos mayores a cero.

El historial incluye cursos anteriores inactivos y excluye inscripciones sin curso,
rechazadas o canceladas. Usa `Registration_List`, `Class_List` y `Evaluation_List`,
sin SQL adicional. Cada nota es el total de componentes del examen del estudiante,
no el promedio del curso. Los exámenes se calculan de forma independiente:
una nota ausente, incompleta, duplicada o inválida se devuelve como null; cero
es una nota válida. No necesita los campos opcionales Active, CreatedAt ni InvoiceLink
del cursor de inscripciones. `count` es la cantidad de elementos de la página.

El estudiante usa su identidad del JWT; los cuerpos de solicitud no admiten
`userId`, estado ni activación. Las rutas nuevas están bajo `/api/Registration`:

| Rol | Método y ruta | Uso |
|---|---|---|
| Student | `GET /mine?PageNumber=1&PageSize=20` | Consultar sus propias inscripciones, incluyendo rechazadas. |
| Student | `GET /available-courses?PageNumber=1&PageSize=20&Filter.Year=2027&Filter.Bimester=1` | Cursos elegibles del año y bimestre seleccionados. |
| Student | `POST /apply` | Crear o editar la solicitud con `{ "classId": "uuid", "invoiceLink": "opcional" }`. |
| Coordinator | `GET /?PageNumber=1&PageSize=20&Filter.Status=Pending` | Consultar las solicitudes pendientes. |
| Coordinator | `PUT /{id}/decision` | Resolver con `{ "approved": true }` o `{ "approved": false }`. |

El listado devuelve `classId`, `moduleNameId`, `moduleName`, `classroomId`,
`classroomName`, `parallel`, `startTime`, `endTime`, fechas, año, bimestre y
`availableSeats`. Filtra cursos, módulos y aulas activos, fechas no terminadas
y cupo disponible. El cupo disponible para el solicitante excluye su propia
reserva, para permitir conservar o editar una inscripción pendiente en un curso lleno.

Se toma la última inscripción activa de un periodo anterior que no esté rechazada
ni cancelada, aunque su curso ya esté inactivo. Con ambos exámenes completos y
válidos, una nota final de **70 o más** habilita exclusivamente `NextModule`;
una nota **menor a 70** habilita el mismo módulo. Sin notas completas no se ofrecen
cursos. Sin historial se ofrecen los módulos de entrada (ningún módulo los señala
como `NextModule`). Aprobar un módulo terminal no habilita otro módulo.

La solicitud siempre queda `Pending` y activa. Se edita la pendiente del mismo
año/bimestre, conservando su ID; también se puede reutilizar una rechazada de ese
periodo o una pendiente sin curso. Si existen varias pendientes activas en el periodo,
se exige al coordinador resolver la ambigüedad. Una inscripción activa `Approved`
bloquea nuevas solicitudes y modificaciones del estudiante en ese año/bimestre;
las aprobaciones históricas permiten continuar en periodos posteriores.

La decisión exige una inscripción activa `Pending`. Aprobar vuelve a validar
elegibilidad y cupo. Rechazar guarda `Status=Rejected` y `Active=false`, por lo que
los listados de pertenencia al curso que filtran inscripciones activas dejan de
incluirla. El CRUD previo del coordinador también impone `Active=false` al rechazar.
El listado general de inscripciones queda reservado a `Coordinator`; el estudiante
consulta `/mine` y no puede cambiar el usuario mediante filtros.

Los dos listados públicos (`GET /api/Registration` y `GET /api/Registration/mine`)
usan el nuevo `Registration_List` con los filtros opcionales `Filter.UserId`,
`Filter.ModuleNameId`, `Filter.RegistrationDate` (YYYY-MM-DD) y `Filter.Status`,
además de `PageNumber` y `PageSize` mayores a cero. En `/mine`, `UserId` siempre
se obtiene del JWT y reemplaza cualquier valor recibido.

Cada elemento de `data` contiene `registrationId`, `userId`, `classId`, `firstName`, `lastName`, `ci`,
`phone`, `moduleName`, `moduleId`, `registrationDate`, `status` y `updatedAt`. `moduleName`
y `moduleId`, junto con `classId`, pueden ser null si todavía no hay curso; `phone` y `registrationDate` también
admiten null. `count` indica la cantidad de elementos de la página.
El procedimiento no recibe filtros `Id`, `ClassId`, `InvoiceLink`, `CreatedAt`
ni `Active`. La columna `RegistrationId` se expone como `registrationId` y permite
resolver la solicitud mediante `PUT /api/Registration/{registrationId}/decision`.

Los flujos internos de elegibilidad, generación, edición y aprobación también usan
`Registration_List`; el repositorio no realiza consultas directas a tablas.
El SQL y su despliegue se administran fuera de este repositorio.
Los listados públicos proyectan los doce campos anteriores. El mapeo de
`Registration_List` se separa del compuesto completo retornado por Insert/Update:
no exige `InvoiceLink`, `CreatedAt` ni `Active`. Si faltan, se conservan como null;
si el SP los devuelve, se leen sus valores reales. Insert/Update mantienen el mapeo
completo de tabla. No se modifica aquí el SQL del procedimiento.
Las validaciones de activación y cupo requieren un valor real de `Active`;
si el cursor no lo incluye, esas operaciones informan que falta el dato, sin
considerar activas todas las inscripciones ni ejecutar escrituras con esa suposición.
La API solo requiere ejecutar los procedimientos, sin SELECT sobre las tablas.
Insert y Update conservan sus procedimientos y respuestas de entidad completa.
Se mantiene el modelo
de operaciones CRUD independientes: solicitudes simultáneas entre validación y
escritura requieren restricciones o bloqueo transaccional en PostgreSQL para
garantizar la unicidad y el cupo bajo concurrencia.

Comprobaciones del servicio con repositorios en memoria:

```powershell
dotnet run --project GrammarErrorDetection.Checks/GrammarErrorDetection.Checks.csproj
```

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
