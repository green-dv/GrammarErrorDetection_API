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
        public AuthService(IUserRepository userRepository, JwtOptions jwtOptions, IRoleRepository roleRepository,
            IVerificationCodeService verificationCodeService, IAuthChallengeRepository challengeRepository,
            IEmailRepository emailRepository, TimeProvider clock)
        {
            ArgumentNullException.ThrowIfNull(userRepository);
            ArgumentNullException.ThrowIfNull(jwtOptions);
            ArgumentNullException.ThrowIfNull(roleRepository);
            this.userRepository = userRepository;
            this.jwtOptions = jwtOptions;
            this.roleRepository = roleRepository;
            this.verificationCodeService = verificationCodeService ?? throw new ArgumentNullException(nameof(verificationCodeService));
            this.challengeRepository = challengeRepository ?? throw new ArgumentNullException(nameof(challengeRepository));
            this.emailRepository = emailRepository ?? throw new ArgumentNullException(nameof(emailRepository));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }
        public async Task<AuthChallengeResponse> LoginAsync(LoginRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

            var user = await ValidateCredentialsAsync(request.Email.Trim(), request.Password);
            if (user.IsTotpEnabled && string.IsNullOrWhiteSpace(user.TotpSecret))
                throw new InvalidOperationException("El usuario tiene TOTP habilitado sin un secreto configurado.");

            return await CreateChallengeAsync(user.Email, $"{user.FirstName} {user.LastName}",
                user.IsTotpEnabled ? "login_totp" : "login_email", user.Id);
        }

        public async Task<AuthChallengeResponse> SendSignupVerificationCodeAsync(SignupRequest request)
        {
            ValidateSignup(request);
            await EnsureSignupAvailableAsync(request);
            return await CreateChallengeAsync(request.Email.Trim(), $"{request.FirstName} {request.LastName}", "signup_email", null);
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
            var challenge = await GetAttemptAsync(request.ChallengeId, ["login_email", "login_totp"]);
            var user = await FindUserAsync(challenge.Email ?? "");
            if (user is null || user.Id != challenge.UserId || !user.Active)
                throw InvalidCode();

            string? matchedStep = null;
            if (challenge.Purpose == "login_totp")
            {
                if (!user.IsTotpEnabled || string.IsNullOrWhiteSpace(user.TotpSecret)
                    || !VerifyTotp(user.TotpSecret, request.Code, out var step))
                    throw InvalidCode();
                matchedStep = "totp:" + step.ToString(CultureInfo.InvariantCulture);
            }
            else if (user.IsTotpEnabled || !VerifyHash(request.Code, challenge.CodeHash))
            {
                throw InvalidCode();
            }

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

        private async Task<AuthChallengeResponse> CreateChallengeAsync(string email, string name, string purpose, Guid? userId)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);
            var challenge = await verificationCodeService.Insert(new VerificationCodeInsert
            {
                UserId = userId, Email = email, Purpose = purpose,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(code), AttemptCount = 0, Active = true
            });
            if (purpose != "login_totp")
            {
                try
                {
                    await emailRepository.SendVerificationEmailAsync(name, email,
                        $"tu código de verificación es: {code}", "Código de verificación");
                }
                catch
                {
                    await verificationCodeService.Update(new VerificationCodeUpdate { Id = challenge.Id, Active = false });
                    throw;
                }
            }
            return new AuthChallengeResponse
            {
                ChallengeId = challenge.Id, Email = email,
                Method = purpose == "login_totp" ? "totp" : "email",
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
            var byEmail = await userRepository.List(new PaginationDto<UserList>
            {
                PageNumber = 1, PageSize = 1, Filter = new UserList { Email = request.Email.Trim() }
            });
            var byCi = await userRepository.List(new PaginationDto<UserList>
            {
                PageNumber = 1, PageSize = 1, Filter = new UserList { CI = request.CI.Trim() }
            });
            if (byEmail.Any(user => SameEmail(user.Email, request.Email.Trim()))
                || byCi.Any(user => user.CI == request.CI.Trim()))
                throw new AuthConflictException("El correo o la cédula de identidad ya están registrados.");
        }

        private async Task<UserEntity?> FindUserAsync(string email)
        {
            var users = await userRepository.List(new PaginationDto<UserList>
            {
                PageNumber = 1, PageSize = 1, Filter = new UserList { Email = email, Active = true }
            });
            return users.SingleOrDefault(user => SameEmail(user.Email, email));
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
