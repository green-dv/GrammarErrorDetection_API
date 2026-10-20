namespace GrammarErrorDetection.Models.Grammar.ModuleNames
{
    public class ModuleNameEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Level { get; set; }

        public Guid? NextModule { get; set; }

        public bool Active { get; set; } = true;
    }
}

