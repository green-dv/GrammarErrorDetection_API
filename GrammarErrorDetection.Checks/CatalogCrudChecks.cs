using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Repositories.Repositories;

/// <summary>CRUD de módulos y aulas: contrato con los SP (dos cursores en el listado) y validación de los modelos.</summary>
internal static class CatalogCrudChecks
{
    public static async Task Run()
    {
        var next = Guid.NewGuid();
        var modules = new Executor("ModuleName", next);
        var moduleRepository = new ModuleNameRepository(modules, modules);

        var page = await moduleRepository.ListPage(new PaginationDto<ModuleNameList>
        {
            PageNumber = 2, PageSize = 5, Filter = new ModuleNameList { Name = "Summit", Active = true }
        });
        Check(page.TotalCount == Executor.TotalRecords && page.Items.Count == 2, "ModuleName: devuelve la página del primer DataTable y el total del segundo.");
        Check(page.Items[0].NextModuleName == "Siguiente" && page.Items[0].NextModule == next, "ModuleName: lee el nombre del módulo siguiente del listado.");
        Check(modules.Parameters.Select(p => p.Name).SequenceEqual(
                ["Id", "Name", "Level", "NextModule", "Active", "PageNumber", "PageSize", "Results", "Total"])
              && Equals(modules.Parameters.Single(p => p.Name == "PageNumber").Value, 2),
            "ModuleName: el listado envía los filtros, la página y los dos cursores.");

        var created = await moduleRepository.Insert(new ModuleNameInsert { Name = "Summit 3-1", Level = 19 });
        Check(modules.Procedure == "ModuleName_Insert" && created.NextModuleName is null && created.Level == 19,
            "ModuleName: el alta llama al SP y la fila sin NextModuleName se lee bien.");
        await moduleRepository.Update(new ModuleNameUpdate { Id = Guid.NewGuid(), ClearNextModule = true, Active = false });
        Check(modules.Procedure == "ModuleName_Update"
              && Equals(modules.Parameters.Single(p => p.Name == "ClearNextModule").Value, true)
              && Equals(modules.Parameters.Single(p => p.Name == "Active").Value, false)
              && modules.Parameters.Single(p => p.Name == "Name").Value is null,
            "ModuleName: deshabilitar y quitar el siguiente envía solo esos campos.");

        var rooms = new Executor("Classroom", next);
        var roomRepository = new ClassroomRepository(rooms, rooms);
        var roomPage = await roomRepository.ListPage(new PaginationDto<ClassroomList> { PageNumber = 1, PageSize = 10, Filter = new ClassroomList { Active = false } });
        Check(roomPage.TotalCount == Executor.TotalRecords && rooms.Parameters.Any(p => p.Name == "Total")
              && Equals(rooms.Parameters.Single(p => p.Name == "Active").Value, false),
            "Classroom: el listado devuelve el total y respeta el filtro de estado.");
        await Throws<ArgumentOutOfRangeException>(
            () => roomRepository.ListPage(new PaginationDto<ClassroomList> { PageNumber = 0, PageSize = 10 }),
            "Classroom: rechaza una página menor que 1 antes de llamar al SP.");

        Check(Invalid(new ModuleNameInsert { Name = "A", Level = 0 }), "ModuleName: el nivel debe ser positivo.");
        Check(!Invalid(new ModuleNameInsert { Name = "A", Level = 1 }), "ModuleName: un nivel positivo es válido.");
        Check(Invalid(new ClassroomInsert { Name = "A", Capacity = 0 }) && Invalid(new ClassroomInsert { Name = "A", Capacity = 1001 }),
            "Classroom: la capacidad debe estar entre 1 y 1000.");
        Check(!Invalid(new ClassroomInsert { Name = "A", Capacity = 1000 }) && !Invalid(new ClassroomUpdate { Id = Guid.NewGuid(), Capacity = null }),
            "Classroom: 1000 es válido y la capacidad es opcional al editar.");
        Check(Invalid(new ModuleNameUpdate { Id = next, NextModule = next }), "ModuleName: un módulo no puede ser su propio siguiente.");
        Console.WriteLine($"{checks} comprobaciones de módulos y aulas correctas.");
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

    private sealed class Executor(string entity, Guid next) : IStoredProcedurePageExecutor, IStoredProcedureExecutor
    {
        public const long TotalRecords = 18;
        public string? Procedure { get; private set; }
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];

        public Task<ResourceResponse<StoredProcedurePage>> ExecutePagedStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, string rowsCursorParameter = "Results", string totalCursorParameter = "Total")
        {
            Check(schema == "grammar" && procedureName == entity + "_List", "Debe llamar al SP de listado correcto.");
            Check(rowsCursorParameter == "Results" && totalCursorParameter == "Total", "Usa los cursores Results y Total.");
            Parameters = parameters;
            var rows = Table(withNextName: entity == "ModuleName");
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
            Parameters = parameters;
            var table = Table(withNextName: false);
            object? Value(string name) => parameters.SingleOrDefault(p => p.Name == name)?.Value;
            var row = Row(table, (string?)Value("Name") ?? "Existente", (bool?)Value("Active") ?? true);
            if (Value("Level") is int level && entity == "ModuleName") row["Level"] = level;
            table.Rows.Add(row);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }

        private DataTable Table(bool withNextName)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Active", typeof(bool));
            if (entity == "ModuleName")
            {
                table.Columns.Add("Level", typeof(int));
                table.Columns.Add("NextModule", typeof(Guid));
                if (withNextName) table.Columns.Add("NextModuleName", typeof(string));
            }
            else
            {
                table.Columns.Add("Capacity", typeof(int));
                table.Columns.Add("CreatedAt", typeof(DateTime));
                table.Columns.Add("UpdatedAt", typeof(DateTime));
            }
            return table;
        }

        private DataRow Row(DataTable table, string name, bool active)
        {
            var row = table.NewRow();
            row["Id"] = Guid.NewGuid();
            row["Name"] = name;
            row["Active"] = active;
            if (entity == "ModuleName")
            {
                row["Level"] = 1;
                if (table.Columns.Contains("NextModuleName")) { row["NextModule"] = next; row["NextModuleName"] = "Siguiente"; }
            }
            else
            {
                row["Capacity"] = 25;
                row["CreatedAt"] = DateTime.UtcNow;
                row["UpdatedAt"] = DateTime.UtcNow;
            }
            return row;
        }
    }
}
