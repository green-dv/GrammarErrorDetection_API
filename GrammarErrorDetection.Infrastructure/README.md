# GrammarErrorDetection.Infrastructure

Biblioteca .NET 10 para consumir APIs JSON y ejecutar procedimientos de PostgreSQL.
No depende de Models ni de Repositories.

## Contratos

- `IHttpApiClient.GetAsync<TResponse>(uri)`.
- `IHttpApiClient.PostAsync<TRequest, TResponse>(uri, request)`.
- `IStoredProcedureExecutor.ExecuteStoredProcedureAsync(schema, procedureName, parameters, result)`.

HTTP devuelve `ResourceResponse<TResponse>`; PostgreSQL devuelve `ResourceResponse<DataTable>`.
La respuesta contiene `Success`, `Data`, `Error` (código y mensaje) y `StatusCode` para HTTP.
`GetDataOrThrow()` permite propagar la excepción original en consumidores que trabajan con excepciones.

## Cliente HTTP

El consumidor proporciona un `HttpClient` con su BaseAddress, headers y timeout.
El cliente admite rutas relativas o URL absolutas y utiliza JSON con las convenciones web de System.Text.Json.
Los errores HTTP, de transporte, timeout y JSON se devuelven en Error.
Una respuesta HTTP 204/205 o con longitud de contenido cero es exitosa con Data nulo.

```csharp
using var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.example.com/")
};
IHttpApiClient client = new HttpApiClient(httpClient);
var response = await client.GetAsync<MyResponse>("resource");
```

En ASP.NET Core se puede registrar con `AddHttpClient<IHttpApiClient, HttpApiClient>()`.
La vida del HttpClient la administra quien lo proporciona; HttpApiClient dispone las peticiones y respuestas.

## Procedimientos almacenados

El consumidor proporciona un `NpgsqlDataSource` compartido. Cada ejecución obtiene y libera su propia conexión.
Los nombres se reciben por separado y se escapan como identificadores; los valores se envían como parámetros.
Se admiten direcciones Input, Output e InputOutput. Los OUT se incluyen como NULL en el CALL.

```csharp
await using var source = NpgsqlDataSource.Create(connectionString);
IStoredProcedureExecutor executor = new StoredProcedureExecutor(source);

var response = await executor.ExecuteStoredProcedureAsync(
    "grammar",
    "Role_List",
    [
        new("PageNumber", NpgsqlDbType.Integer, 1),
        new("PageSize", NpgsqlDbType.Integer, 10),
        new("Results", NpgsqlDbType.Refcursor, "role_list", ParameterDirection.InputOutput)
    ],
    StoredProcedureResult.Cursor("Results"));

using var table = response.GetDataOrThrow();
```

Importa System.Data, Npgsql, NpgsqlTypes y los namespaces Database, Http e Interfaces de esta biblioteca según el ejemplo usado.

Resultados admitidos:

- `StoredProcedureResult.Rows`: filas directas; valor predeterminado.
- `StoredProcedureResult.Cursor("Results")`: CALL y FETCH en la misma conexión y transacción.
- `StoredProcedureResult.Composite("User", "grammar", "User")`: desglosa un OUT compuesto a columnas tipadas.

Los SP ejecutados deben admitir una transacción administrada por el cliente.
Cada operación confirma su transacción antes de devolver la tabla; su transformación a modelos pertenece al repositorio.
Los errores de PostgreSQL usan SQLSTATE como código; una salida compuesta nula devuelve NOT_FOUND.
Los argumentos de programación inválidos lanzan excepciones de validación.

## Uso desde repositorios

Los repositorios reciben `IStoredProcedureExecutor` por constructor.
Conservan la elección de los SP, la creación de parámetros y el uso de ConvertToEntity.
Los errores internos se propagan mediante GetDataOrThrow para mantener sus contratos de retorno de entidades y listas.

