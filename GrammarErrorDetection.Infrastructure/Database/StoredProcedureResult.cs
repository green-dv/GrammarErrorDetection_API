namespace GrammarErrorDetection.Infrastructure.Database
{
    internal enum StoredProcedureResultKind { Rows, Cursor, Composite }

    public sealed class StoredProcedureResult
    {
        private StoredProcedureResult(StoredProcedureResultKind kind, string? outputParameter = null, string? schema = null, string? typeName = null)
        {
            Kind = kind;
            OutputParameter = outputParameter;
            Schema = schema;
            TypeName = typeName;
        }

        internal StoredProcedureResultKind Kind { get; }
        internal string? OutputParameter { get; }
        internal string? Schema { get; }
        internal string? TypeName { get; }

        public static StoredProcedureResult Rows { get; } = new(StoredProcedureResultKind.Rows);

        public static StoredProcedureResult Cursor(string outputParameter)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputParameter);
            return new(StoredProcedureResultKind.Cursor, outputParameter);
        }

        public static StoredProcedureResult Composite(string outputParameter, string schema, string typeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputParameter);
            ArgumentException.ThrowIfNullOrWhiteSpace(schema);
            ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
            return new(StoredProcedureResultKind.Composite, outputParameter, schema, typeName);
        }
    }
}
