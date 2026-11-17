using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Models.Grammar.UserLevels;
using GrammarErrorDetection.Repositories.Repositories;

/// <summary>CRUD de niveles de usuario y tipos de condición: contrato con los SP y validación de los modelos.</summary>
internal static class LevelsAndConditionsChecks
{
    public static async Task Run()
    {
        var levels = new Executor("UserLevel");
        var levelRepository = new UserLevelRepository(levels, levels);
        var page = await levelRepository.List(new PaginationDto<UserLevelList>
        {
            PageNumber = 1, PageSize = 100, Filter = new UserLevelList { Active = true }
        });
        Check(page.TotalCount == Executor.TotalRecords && page.Items.Count == 2 && page.Items.All(item => item.TotalCount == Executor.TotalRecords),
            "UserLevel: el listado devuelve el total del segundo DataTable y lo conserva en cada fila (compatibilidad con la WEB de puntos).");
        Check(page.Items[0].UserCount == 322, "UserLevel: lee la cantidad de usuarios por nivel.");
        Check(levels.Parameters.Select(p => p.Name).SequenceEqual(
                ["Id", "Level", "Name", "MinPoints", "MaxPoints", "Active", "CreatedAt", "PageNumber", "PageSize", "Results", "Total"]),
            "UserLevel: el listado envía los filtros, la página y los dos cursores.");

        var created = await levelRepository.Insert(new UserLevelInsert { Level = 7, Name = "Leyenda", MinPoints = 2500 });
        Check(levels.Procedure == "UserLevel_Insert" && created.Level == 7 && created.MinPoints == 2500 && created.UserCount == 0
              && levels.Parameters.Select(p => p.Name).SequenceEqual(["Level", "Name", "MinPoints", "Active", "UserLevel"]),
            "UserLevel: el alta llama al SP y lee la fila devuelta (sin UserCount).");
        var id = Guid.NewGuid();
        var disabled = await levelRepository.Update(new UserLevelUpdate { Id = id, Active = false });
        Check(levels.Procedure == "UserLevel_Update" && disabled.Id == id && !disabled.Active
              && levels.Parameters.Single(p => p.Name == "Name").Value is null && levels.Parameters.Single(p => p.Name == "MinPoints").Value is null,
            "UserLevel: deshabilitar envía solo el id y Active = false.");
        var calls = levels.WriteCalls;
        await Throws<ValidationException>(() => levelRepository.Insert(new UserLevelInsert { Level = 0, Name = "X", MinPoints = 1 }), "UserLevel: rechaza el nivel 0.");
        await Throws<ValidationException>(() => levelRepository.Insert(new UserLevelInsert { Level = 1, Name = "X", MinPoints = -1 }), "UserLevel: rechaza puntos negativos.");
        await Throws<ValidationException>(() => levelRepository.Insert(new UserLevelInsert { Level = 1, Name = " ", MinPoints = 1 }), "UserLevel: rechaza el nombre vacío.");
        Check(levels.WriteCalls == calls, "UserLevel: un nivel inválido no llega a la base de datos.");

        var conditions = new Executor("ConditionType");
        var conditionRepository = new ConditionTypeRepository(conditions, conditions);
        var conditionPage = await conditionRepository.List(new PaginationDto<ConditionTypeList>
        {
            PageNumber = 2, PageSize = 5, Filter = new ConditionTypeList { Code = "TEXT", Active = true }
        });
        Check(conditionPage.TotalCount == Executor.TotalRecords && conditionPage.Items.Count == 2 && conditionPage.Items[0].UsageCount == 21,
            "ConditionType: el listado devuelve el total y la cantidad de condiciones activas que usan cada tipo.");
        Check(conditions.Parameters.Select(p => p.Name).SequenceEqual(
                ["Id", "Code", "Name", "Description", "ValueType", "Active", "CreatedAt", "PageNumber", "PageSize", "Results", "Total"])
              && Equals(conditions.Parameters.Single(p => p.Name == "PageNumber").Value, 2),
            "ConditionType: el listado envía los filtros, la página y los dos cursores.");
        var updated = await conditionRepository.Update(new ConditionTypeUpdate { Id = id, Name = "Nuevo nombre", Active = false });
        Check(conditions.Procedure == "ConditionType_Update" && updated.UsageCount == 0 && !updated.Active
              && conditions.Parameters.Single(p => p.Name == "Code").Value is null && conditions.Parameters.Single(p => p.Name == "ValueType").Value is null,
            "ConditionType: editar no reenvía Code ni ValueType (no se pueden cambiar).");

        Check(Invalid(new UserLevelUpdate { Id = id, MinPoints = -5 }) && !Invalid(new UserLevelUpdate { Id = id, MinPoints = null }),
            "UserLevel: la edición valida los puntos solo si se envían.");
        Console.WriteLine($"{checks} comprobaciones de niveles y tipos de condición correctas.");
    }

    private static int checks;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    private static bool Invalid(object model) => !Validator.TryValidateObject(model, new ValidationContext(model), [], validateAllProperties: true);

    private static async Task Throws<T>(Func<Task> action, string message) where T : Exception
    {
        try { await action(); }
        catch (T) { checks++; return; }
        throw new Exception(message);
    }

    private sealed class Executor(string entity) : IStoredProcedurePageExecutor, IStoredProcedureExecutor
    {
        public const long TotalRecords = 8;
        public string? Procedure { get; private set; }
        public int WriteCalls { get; private set; }
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];

        public Task<ResourceResponse<StoredProcedurePage>> ExecutePagedStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, string rowsCursorParameter = "Results", string totalCursorParameter = "Total")
        {
            Check(schema == "grammar" && procedureName == entity + "_List", "Debe llamar al SP de listado correcto.");
            Check(rowsCursorParameter == "Results" && totalCursorParameter == "Total", "Usa los cursores Results y Total.");
            Parameters = parameters;
            var rows = Table(withListColumns: true);
            rows.Rows.Add(Row(rows, "Uno", true));
            rows.Rows.Add(Row(rows, "Dos", true));
            var total = new DataTable();
            total.Columns.Add("TotalCount", typeof(long));
            total.Rows.Add(TotalRecords);
            return Task.FromResult(ResourceResponse<StoredProcedurePage>.Ok(new StoredProcedurePage(rows, total)));
        }

        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            Procedure = procedureName;
            WriteCalls++;
            Parameters = parameters;
            var table = Table(withListColumns: false);
            object? Value(string name) => parameters.SingleOrDefault(p => p.Name == name)?.Value;
            var row = Row(table, (string?)Value("Name") ?? "Existente", (bool?)Value("Active") ?? true);
            if (Value("Id") is Guid id) row["Id"] = id;
            if (entity == "UserLevel")
            {
                if (Value("Level") is int level) row["Level"] = level;
                if (Value("MinPoints") is long points) row["MinPoints"] = points;
            }
            table.Rows.Add(row);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }

        private DataTable Table(bool withListColumns)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Active", typeof(bool));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            if (entity == "UserLevel")
            {
                table.Columns.Add("Level", typeof(int));
                table.Columns.Add("MinPoints", typeof(long));
                if (withListColumns) table.Columns.Add("UserCount", typeof(long));
            }
            else
            {
                table.Columns.Add("Code", typeof(string));
                table.Columns.Add("Description", typeof(string));
                table.Columns.Add("ValueType", typeof(string));
                if (withListColumns) table.Columns.Add("UsageCount", typeof(long));
            }
            return table;
        }

        private DataRow Row(DataTable table, string name, bool active)
        {
            var row = table.NewRow();
            row["Id"] = Guid.NewGuid();
            row["Name"] = name;
            row["Active"] = active;
            row["CreatedAt"] = DateTime.UtcNow;
            row["UpdatedAt"] = DateTime.UtcNow;
            if (entity == "UserLevel")
            {
                row["Level"] = 1;
                row["MinPoints"] = 0L;
                if (table.Columns.Contains("UserCount")) row["UserCount"] = 322L;
            }
            else
            {
                row["Code"] = "TEXTS_ANALYZED";
                row["ValueType"] = "Integer";
                if (table.Columns.Contains("UsageCount")) row["UsageCount"] = 21L;
            }
            return row;
        }
    }
}
