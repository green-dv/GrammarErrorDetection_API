using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GrammarErrorDetection.Services.Services
{
    public sealed class UserService : IUserService
    {
        private readonly IUserRepository repository;

        public UserService(IUserRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            this.repository = repository;
        }

        public Task<List<UserEntity>> List(PaginationDto<UserList> request)
            => repository.List(request);

        public Task<UserEntity> Insert(UserInsert request)
            => repository.Insert(request);

        public Task<UserEntity> Update(UserUpdate request)
            => repository.Update(request);

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (Encoding.UTF8.GetByteCount(request.Password) > 72
                || Encoding.UTF8.GetByteCount(request.NewPassword) > 72)
                throw new ValidationException("La contraseña no puede superar 72 bytes UTF-8 para BCrypt.");

            var user = await FindActiveUserAsync(userId);
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                throw new ValidationException("La contraseña actual es incorrecta.");

            await repository.Update(new UserUpdate
            {
                Id = userId,
                Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword),
                ChangePassword = false
            });
        }

        public async Task RequirePasswordChangeAsync(Guid userId)
        {
            await FindActiveUserAsync(userId);
            await repository.Update(new UserUpdate { Id = userId, ChangePassword = true });
        }

        private async Task<UserEntity> FindActiveUserAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ValidationException("El identificador de usuario no puede estar vacío.");

            // User_List no recibe Id; se recorren sus páginas sin cambiar el contrato del SP.
            for (var page = 1; ; page++)
            {
                var users = await repository.List(new PaginationDto<UserList>
                {
                    PageNumber = page, PageSize = 100, Filter = new UserList { Active = true }
                });
                var user = users.SingleOrDefault(user => user.Id == userId && user.Active);
                if (user is not null)
                    return user;
                if (users.Count < 100)
                    throw new KeyNotFoundException("No se encontró el usuario activo.");
            }
        }
    }
}
