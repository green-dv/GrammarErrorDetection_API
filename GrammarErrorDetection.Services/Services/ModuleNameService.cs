using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services
{
    public sealed class ModuleNameService : IModuleNameService
    {
        private readonly IModuleNameRepository repository;

        public ModuleNameService(IModuleNameRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            this.repository = repository;
        }

        public Task<List<ModuleNameEntity>> List(PaginationDto<ModuleNameList> request)
            => repository.List(request);

        public Task<ModuleNameEntity> Insert(ModuleNameInsert request)
            => repository.Insert(request);

        public Task<ModuleNameEntity> Update(ModuleNameUpdate request)
            => repository.Update(request);
    }
}

