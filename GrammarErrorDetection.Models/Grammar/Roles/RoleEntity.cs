namespace GrammarErrorDetection.Models.Grammar.Roles
{
    public class RoleEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
