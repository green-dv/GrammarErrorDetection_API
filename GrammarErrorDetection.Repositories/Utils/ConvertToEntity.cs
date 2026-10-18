using System.Data;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;

namespace GrammarErrorDetection.Repositories.Utils
{
    public static class ConvertToEntity
    {
        /// <summary>
        /// Convierte una fila con las columnas de grammar."User" a su entidad.
        /// </summary>
        /// <param name="row">Fila con las columnas de la entidad, incluidos sus campos anulables.</param>
        public static UserEntity UserToEntity(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);

            return new UserEntity
            {
                Id = (Guid)row["Id"],
                FirstName = (string)row["FirstName"],
                LastName = (string)row["LastName"],
                CI = (string)row["CI"],
                Email = (string)row["Email"],
                Password = (string)row["Password"],
                Phone = row.Field<string?>("Phone"),
                RoleId = (Guid)row["RoleId"],
                HasAuthenticator = (bool)row["HasAuthenticator"],
                TotpSecret = row.Field<string?>("TotpSecret"),
                IsTotpEnabled = (bool)row["IsTotpEnabled"],
                ChangePassword = (bool)row["ChangePassword"],
                CreatedAt = (DateTime)row["CreatedAt"],
                UpdatedAt = (DateTime)row["UpdatedAt"],
                Active = (bool)row["Active"]
            };
        }

        /// <summary>
        /// Convierte una fila con las columnas de grammar."VerificationCode" a su entidad.
        /// </summary>
        /// <param name="row">Fila con las columnas de la entidad, incluidos sus campos anulables.</param>
        public static VerificationCodeEntity VerificationCodeToEntity(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);

            return new VerificationCodeEntity
            {
                Id = (Guid)row["Id"],
                CodeHash = (string)row["CodeHash"],
                Purpose = (string)row["Purpose"],
                AttemptCount = (int)row["AttemptCount"],
                ConsumedAt = row.Field<DateTime?>("ConsumedAt"),
                ExpirationTime = (DateTime)row["ExpirationTime"],
                UserId = row.Field<Guid?>("UserId"),
                Email = row.Field<string?>("Email"),
                CreatedAt = (DateTime)row["CreatedAt"],
                UpdatedAt = (DateTime)row["UpdatedAt"],
                Active = (bool)row["Active"]
            };
        }

        /// <summary>
        /// Convierte una fila con las columnas de grammar."Role" a su entidad.
        /// </summary>
        /// <param name="row">Fila con las columnas de la entidad, incluidos sus campos anulables.</param>
        public static RoleEntity RoleToEntity(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);

            return new RoleEntity
            {
                Id = (Guid)row["Id"],
                Name = (string)row["Name"],
                Active = (bool)row["Active"]
            };
        }
    }
}

