using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Configuration;

namespace GrammarErrorDetection.Services.Services
{
    public sealed class VerificationCodeService : IVerificationCodeService
    {
        private readonly IVerificationCodeRepository repository;
        private readonly VerificationCodeOptions options;
        private readonly TimeProvider clock;

        public VerificationCodeService(
            IVerificationCodeRepository repository,
            VerificationCodeOptions options,
            TimeProvider clock)
        {
            ArgumentNullException.ThrowIfNull(repository);
            this.repository = repository;
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(clock);
            this.options = options;
            this.clock = clock;
        }

        public Task<List<VerificationCodeEntity>> List(PaginationDto<VerificationCodeList> request)
            => repository.List(request);

        public Task<VerificationCodeEntity> Insert(VerificationCodeInsert request)
        {
            ArgumentNullException.ThrowIfNull(request);
            request.ExpirationTime = clock.GetUtcNow().UtcDateTime.AddMinutes(options.ExpirationMinutes);
            return repository.Insert(request);
        }

        public Task<VerificationCodeEntity> Update(VerificationCodeUpdate request)
            => repository.Update(request);
    }
}
