using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Repositories.Interfaces;
using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using GrammarErrorDetection.Models.Grammar.Roles;

namespace GrammarErrorDetection.Services.Services
{
    public sealed class AuthService : IAuthService
    {
        private static readonly TimeZoneInfo ResponseTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
        private readonly IUserRepository userRepository;
        private readonly JwtOptions jwtOptions;
        private readonly IRoleRepository roleRepository;
        public AuthService(IUserRepository userRepository, JwtOptions jwtOptions, IRoleRepository roleRepository)
        {
            ArgumentNullException.ThrowIfNull(userRepository);
            ArgumentNullException.ThrowIfNull(jwtOptions);
            ArgumentNullException.ThrowIfNull(roleRepository);
            this.userRepository = userRepository;
            this.jwtOptions = jwtOptions;
            this.roleRepository = roleRepository;
        }
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

            var user = await ValidateCredentialsAsync(request.Email.Trim(), request.Password);
            return await CreateLoginResponseAsync(user);
        }

        public async Task<LoginResponse> SignupAsync(SignupRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (Encoding.UTF8.GetByteCount(request.Password) > 72)
                throw new ValidationException("La contraseña no puede superar 72 bytes UTF-8 para BCrypt.");

            var roles = await roleRepository.List(new PaginationDto<RoleList>
            {
                PageNumber = 1,
                PageSize = 1,
                Filter = new RoleList { Name = "Student", Active = true }
            });
            var studentRole = roles.SingleOrDefault();
            if (studentRole is null || !studentRole.Active || studentRole.Name != "Student")
                throw new InvalidOperationException("No se encontró el rol activo Student para el registro.");

            var user = await userRepository.Insert(new UserInsert
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                CI = request.CI.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone?.Trim(),
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = studentRole.Id,
                Active = true
            });

            return await CreateLoginResponseAsync(user);
        }

        private async Task<LoginResponse> CreateLoginResponseAsync(UserEntity user)
        {
            var roles = await roleRepository.List(new PaginationDto<RoleList>
            {
                PageNumber = 1,
                PageSize = 1,
                Filter = new RoleList { Id = user.RoleId, Active = true }
            });
            var role = roles.SingleOrDefault();
            if (role is null || role.Id != user.RoleId || !role.Active || string.IsNullOrWhiteSpace(role.Name))
                throw new UnauthorizedAccessException("El usuario no tiene un rol válido.");

            // JWT representa la expiración con precisión de segundos.
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(
                DateTimeOffset.UtcNow.AddMinutes(jwtOptions.ExpirationMinutes).ToUnixTimeSeconds());
            return new LoginResponse
            {
                AccessToken = GenerateJwtToken(user, role.Name, expiresAt),
                ExpiresAt = TimeZoneInfo.ConvertTime(expiresAt, ResponseTimeZone),
                User = new UserAuthResponse
                {
                    Id = user.Id,
                    Name = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email,
                }
            };
        }

        private string GenerateJwtToken(UserEntity user, string roleName, DateTimeOffset expiresAt)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role_id", user.RoleId.ToString()),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
            var token = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                claims: claims,
                expires: expiresAt.UtcDateTime,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserEntity> ValidateCredentialsAsync(string email, string password)
        {
            var users = await userRepository.List(new PaginationDto<UserList>
            {
                PageNumber = 1,
                PageSize = 1,
                Filter = new UserList { Email = email, Active = true }
            });

            var user = users.SingleOrDefault();
            if (user is not null
                && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
                && user.Active
                && BCrypt.Net.BCrypt.Verify(password, user.Password))
                return user;

            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        }
    }
}
