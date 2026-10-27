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
using GrammarErrorDetection.Models.Grammar.VerificationCodes;
using System.Security.Cryptography;
using System.Globalization;
using OtpNet;
using GrammarErrorDetection.Services.Exceptions;

namespace GrammarErrorDetection.Services.Services
{
    public sealed class AuthService : IAuthService
    {
        private static readonly TimeZoneInfo ResponseTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
        private readonly IUserRepository userRepository;
        private readonly JwtOptions jwtOptions;
        private readonly IRoleRepository roleRepository;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly IAuthChallengeRepository challengeRepository;
        private readonly IEmailRepository emailRepository;
        private readonly TimeProvider clock;
        private const int MaxAttempts = 5;
        public AuthService(IUserRepository userRepository, JwtOptions jwtOptions, IRoleRepository roleRepository, IVerificationCodeService verificationCodeService, IAuthChallengeRepository challengeRepository, IEmailRepository emailRepository, TimeProvider clock)
        {
            this.userRepository = userRepository;
            this.jwtOptions = jwtOptions;
            this.roleRepository = roleRepository;
            this.verificationCodeService = verificationCodeService;
            this.challengeRepository = challengeRepository;
            this.emailRepository = emailRepository;
            this.clock = clock;
        }
        public async Task<LoginOptionsResponse> LoginAsync(LoginRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

            var user = await ValidateCredentialsAsync(request.Email.Trim(), request.Password);
            return new LoginOptionsResponse { Email = user.Email, IsTotpEnabled = user.IsTotpEnabled };
        }

        public async Task<AuthChallengeResponse> SendVerificationCodeAsync(SendVerificationCodeRequest request)
        {
            Validate(request);
            var email = request.Email.Trim();
            if (request.Purpose == "login")
            {
                Validate(new LoginRequest { Email = email, Password = request.Password ?? string.Empty });
                var user = await ValidateCredentialsAsync(email, request.Password!);
                return await CreateEmailChallengeAsync(user.Email, $"{user.FirstName} {user.LastName}", "login_email", user.Id);
            }

            var existing = await FindMatchingUserAsync(new UserList { Email = email }, user => SameEmail(user.Email, email));
            if (existing is not null)
                throw new AuthConflictException("El correo ya está registrado.");
            return await CreateEmailChallengeAsync(email, email, "signup_email", null);
        }

        public async Task<LoginResponse> SignupAsync(SignupConfirmationRequest request)
        {
            ValidateSignup(request);
            await EnsureSignupAvailableAsync(request);
            var roles = await roleRepository.List(new PaginationDto<RoleList>
            {
                PageNumber = 1,
                PageSize = 1,
                Filter = new RoleList { Name = "Student", Active = true }
            });
            var studentRole = roles.SingleOrDefault();
            if (studentRole is null || !studentRole.Active || studentRole.Name != "Student")
                throw new InvalidOperationException("No se encontró el rol activo Student para el registro.");

            var challenge = await GetAttemptAsync(request.ChallengeId, ["signup_email"]);
            if (challenge.UserId is not null || !SameEmail(challenge.Email, request.Email.Trim())
                || !VerifyHash(request.Code, challenge.CodeHash))
                throw InvalidCode();
            // Consumir antes de insertar impide registrar dos usuarios usando el mismo desafío.
            await ConsumeAsync(challenge.Id);
            var user = await userRepository.Insert(new UserInsert
            {
                FirstName = request.FirstName.Trim(),
                FechaNacimiento = request.FechaNacimiento,
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

        public async Task<LoginResponse> VerifyMfaLoginAsync(VerifyMfaRequest request)
        {
            Validate(request);
            var challenge = await GetAttemptAsync(request.ChallengeId, ["login_email"]);
            var user = await FindUserAsync(challenge.Email ?? "");
            if (user is null || user.Id != challenge.UserId || !user.Active)
                throw InvalidCode();

            if (!VerifyHash(request.Code, challenge.CodeHash))
                throw InvalidCode();

            await ConsumeAsync(challenge.Id);
            return await CreateLoginResponseAsync(user);
        }

        public async Task<LoginResponse> VerifyTotpLoginAsync(VerifyTotpLoginRequest request)
        {
            Validate(request);
            var user = await ValidateCredentialsAsync(request.Email.Trim(), request.Password);
            if (!user.IsTotpEnabled || string.IsNullOrWhiteSpace(user.TotpSecret) || !VerifyTotp(user.TotpSecret, request.Code, out var step))
                throw InvalidCode();

            // Registro interno para impedir reutilizar el mismo paso TOTP entre logins.
            var matchedStep = "totp:" + step.ToString(CultureInfo.InvariantCulture);
            var challenge = await verificationCodeService.Insert(new VerificationCodeInsert
            {
                UserId = user.Id, Email = user.Email, Purpose = "login_totp",
                CodeHash = matchedStep, AttemptCount = 0, Active = true
            });
            await ConsumeAsync(challenge.Id, matchedStep);
            return await CreateLoginResponseAsync(user);
        }

        public async Task<TotpSetupResponse> GenerateTotpSetupAsync(Guid userId, string email)
        {
            var user = await GetAuthenticatedUserAsync(userId, email);
            if (user.IsTotpEnabled)
                throw new ValidationException("TOTP ya está habilitado.");

            var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
            if (!await challengeRepository.TrySetTotpSecretAsync(user.Id, user.Email, secret))
                throw new ValidationException("TOTP ya está habilitado o el usuario no está activo.");
            var issuer = Uri.EscapeDataString("GrammarErrorDetection");
            var label = Uri.EscapeDataString(user.Email);
            return new TotpSetupResponse
            {
                OtpAuthUri = $"otpauth://totp/{issuer}:{label}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30"
            };
        }

        public async Task ConfirmTotpSetupAsync(Guid userId, string email, TotpCodeRequest request)
        {
            Validate(request);
            var user = await GetAuthenticatedUserAsync(userId, email);
            if (user.IsTotpEnabled || string.IsNullOrWhiteSpace(user.TotpSecret)
                || !VerifyTotp(user.TotpSecret, request.Code, out _))
                throw InvalidCode();

            if (!await challengeRepository.TryEnableTotpAsync(user.Id, user.Email, user.TotpSecret))
                throw InvalidCode();
        }

        private async Task<AuthChallengeResponse> CreateEmailChallengeAsync(string email, string name, string purpose, Guid? userId)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);
            var challenge = await verificationCodeService.Insert(new VerificationCodeInsert
            {
                UserId = userId, Email = email, Purpose = purpose,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(code), AttemptCount = 0, Active = true
            });
            try
            {
                await emailRepository.SendVerificationEmailAsync(name, email, $"tu código de verificación es: {code}", "Código de verificación");
            }
            catch
            {
                await verificationCodeService.Update(new VerificationCodeUpdate { Id = challenge.Id, Active = false });
                throw;
            }
            return new AuthChallengeResponse
            {
                ChallengeId = challenge.Id, Email = email,
                Method = "email",
                ExpiresAt = ToResponseTime(challenge.ExpirationTime)
            };
        }

        private async Task<VerificationCodeEntity> GetAttemptAsync(Guid id, string[] purposes)
            => await challengeRepository.RegisterAttemptAsync(id, purposes, clock.GetUtcNow().UtcDateTime, MaxAttempts)
                ?? throw InvalidCode();

        private async Task ConsumeAsync(Guid id, string? totpStep = null)
        {
            if (!await challengeRepository.TryConsumeAsync(id, clock.GetUtcNow().UtcDateTime, totpStep))
                throw InvalidCode();
        }

        private bool VerifyTotp(string secret, string code, out long step)
            => new Totp(Base32Encoding.ToBytes(secret)).VerifyTotp(clock.GetUtcNow().UtcDateTime,
                code, out step, new VerificationWindow(previous: 1, future: 1));

        private static bool VerifyHash(string code, string hash)
        {
            try { return BCrypt.Net.BCrypt.Verify(code, hash); }
            catch (BCrypt.Net.SaltParseException) { return false; }
        }

        private static void Validate(object request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        }

        private static void ValidateSignup(SignupRequest request)
        {
            Validate(request);
            if (Encoding.UTF8.GetByteCount(request.Password) > 72)
                throw new ValidationException("La contraseña no puede superar 72 bytes UTF-8 para BCrypt.");
        }

        private async Task EnsureSignupAvailableAsync(SignupRequest request)
        {
            var email = request.Email.Trim();
            var ci = request.CI.Trim();
            var byEmail = await FindMatchingUserAsync(new UserList { Email = email }, user => SameEmail(user.Email, email));
            var byCi = await FindMatchingUserAsync(new UserList { CI = ci }, user => user.CI == ci);
            if (byEmail is not null || byCi is not null)
                throw new AuthConflictException("El correo o la cédula de identidad ya están registrados.");
        }

        private Task<UserEntity?> FindUserAsync(string email)
            => FindMatchingUserAsync(new UserList { Email = email, Active = true },
                user => user.Active && SameEmail(user.Email, email));

        private async Task<UserEntity?> FindMatchingUserAsync(UserList filter, Func<UserEntity, bool> matches)
        {
            // User_List aplica ILIKE parcial; la coincidencia exacta puede estar en otra página.
            for (var page = 1; ; page++)
            {
                var users = await userRepository.List(new PaginationDto<UserList>
                {
                    PageNumber = page, PageSize = 100, Filter = filter
                });
                var user = users.SingleOrDefault(matches);
                if (user is not null)
                    return user;
                if (users.Count < 100)
                    return null;
            }
        }

        private async Task<UserEntity> GetAuthenticatedUserAsync(Guid id, string email)
        {
            var user = await FindUserAsync(email);
            return user is not null && user.Id == id && user.Active ? user : throw new UnauthorizedAccessException();
        }

        private static bool SameEmail(string? left, string right)
            => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static UnauthorizedAccessException InvalidCode()
            => new("El código es incorrecto, expiró o ya fue utilizado.");

        private static DateTimeOffset ToResponseTime(DateTime value)
            => value.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(value, ResponseTimeZone.GetUtcOffset(value))
                : TimeZoneInfo.ConvertTime(new DateTimeOffset(value), ResponseTimeZone);

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
                clock.GetUtcNow().AddMinutes(jwtOptions.ExpirationMinutes).ToUnixTimeSeconds());
            return new LoginResponse
            {
                AccessToken = GenerateJwtToken(user, role.Name, expiresAt),
                ExpiresAt = TimeZoneInfo.ConvertTime(expiresAt, ResponseTimeZone),
                User = new UserAuthResponse
                {
                    Id = user.Id,
                    Name = $"{user.FirstName} {user.LastName}".Trim(),
                    FechaNacimiento = user.FechaNacimiento,
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
            var user = await FindUserAsync(email);
            if (user is not null
                && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
                && user.Active
                && BCrypt.Net.BCrypt.Verify(password, user.Password))
                return user;

            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        }
    }
}
