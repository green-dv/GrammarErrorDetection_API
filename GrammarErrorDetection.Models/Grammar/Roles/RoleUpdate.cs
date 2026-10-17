namespace GrammarErrorDetection.Models.Grammar.Roles
{
    public class RoleUpdate
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }

        public bool? Active { get; set; }
    }
}
