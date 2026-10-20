using System.Data;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;

namespace GrammarErrorDetection.Repositories.Utils
{
    public static class ConvertToEntity
    {
        public static EvaluationEntity EvaluationToEntity(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);
            return new EvaluationEntity
            {
                Id = (Guid)row["Id"],
                RegistrationId = (Guid)row["RegistrationId"],
                Period = row.Field<int?>("Period"),
                WritingScore = row.Field<int?>("WritingScore"),
                GrammarScore = row.Field<int?>("GrammarScore"),
                VocabularyScore = row.Field<int?>("VocabularyScore"),
                ReadingScore = row.Field<int?>("ReadingScore"),
                ListeningScore = row.Field<int?>("ListeningScore"),
                OralScore = row.Field<int?>("OralScore"),
                AcademicPerformance = row.Field<int?>("AcademicPerformance"),
                CreatedAt = (DateTime)row["CreatedAt"],
                UpdatedAt = (DateTime)row["UpdatedAt"],
                Active = (bool)row["Active"]
            };
        }

        /// <summary>
        /// Convierte una fila con las columnas de grammar."ModuleName" a su entidad.
        /// </summary>
        /// <param name="row">Fila con las columnas de la entidad, incluidos sus campos anulables.</param>
        public static ModuleNameEntity ModuleNameToEntity(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);

            return new ModuleNameEntity
            {
                Id = (Guid)row["Id"],
                Name = (string)row["Name"],
                Level = (int)row["Level"],
                NextModule = row.Field<Guid?>("NextModule"),
                Active = (bool)row["Active"]
            };
        }

        /// <summary>
        /// Convierte una fila con las columnas de grammar."Classroom" a su entidad.
        /// </summary>
        /// <param name="row">Fila con las columnas de la entidad, incluidos sus campos anulables.</param>
        public static ClassroomEntity ClassroomToEntity(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);

            return new ClassroomEntity
            {
                Id = (Guid)row["Id"],
                Name = (string)row["Name"],
                Capacity = (int)row["Capacity"],
                CreatedAt = (DateTime)row["CreatedAt"],
                UpdatedAt = (DateTime)row["UpdatedAt"],
                Active = (bool)row["Active"]
            };
        }

        /// <summary>
        /// Convierte el cursor enriquecido de Class_List a su entidad.
        /// </summary>
        /// <param name="row">Fila con las columnas de la entidad, incluidos sus campos anulables.</param>
        public static ClassEntity ClassListToEntity(DataRow row)
        {
            var entity = ClassToEntity(row);
            entity.TeacherName = row.Field<string?>("TeacherName");
            entity.ModuleName = row.Field<string?>("ModuleName");
            entity.ModuleLevel = row.IsNull("ModuleLevel") ? null : Convert.ToInt32(row["ModuleLevel"]);
            entity.ClassroomName = row.Field<string?>("ClassroomName");
            entity.ClassroomCapacity = row.IsNull("ClassroomCapacity") ? null : Convert.ToInt32(row["ClassroomCapacity"]);
            entity.StudentCount = row.IsNull("StudentCount") ? null : Convert.ToInt64(row["StudentCount"]);
            entity.Exam1Average = row.IsNull("Exam1Average") ? null : Convert.ToDecimal(row["Exam1Average"]);
            entity.Exam2Average = row.IsNull("Exam2Average") ? null : Convert.ToDecimal(row["Exam2Average"]);
            return entity;
        }

        public static ClassEntity ClassToEntity(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);

            return new ClassEntity
            {
                Id = (Guid)row["Id"],
                TeacherId = (Guid)row["TeacherId"],
                ModuleNameId = (Guid)row["ModuleNameId"],
                Bimester = (int)row["Bimester"],
                Year = (int)row["Year"],
                StartDate = row.Field<DateTime?>("StartDate"),
                EndDate = row.Field<DateTime?>("EndDate"),
                Parallel = row.Field<string?>("Parallel"),
                ClassroomId = (Guid)row["ClassroomId"],
                StartTime = row["StartTime"] is TimeOnly startTime
                    ? startTime : TimeOnly.FromTimeSpan((TimeSpan)row["StartTime"]),
                EndTime = row["EndTime"] is TimeOnly endTime
                    ? endTime : TimeOnly.FromTimeSpan((TimeSpan)row["EndTime"]),
                CreatedAt = (DateTime)row["CreatedAt"],
                UpdatedAt = (DateTime)row["UpdatedAt"],
                Active = (bool)row["Active"]
            };
        }

        /// <summary>
        /// Convierte una fila con las columnas de grammar."Registration" a su entidad.
        /// </summary>
        /// <param name="row">Fila con las columnas de la entidad, incluidos sus campos anulables.</param>
        public static RegistrationEntity RegistrationToEntity(DataRow row)
            => RegistrationToEntity(row, "Id");

        public static RegistrationEntity RegistrationListToEntity(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);
            return new RegistrationEntity
            {
                Id = row.Field<Guid>("RegistrationId"),
                UserId = row.Field<Guid>("UserId"),
                ClassId = row.Field<Guid?>("ClassId"),
                RegistrationDate = row.Field<DateTime?>("RegistrationDate"),
                Status = row.Field<string>("Status") ?? throw new DataException("Falta Status en Registration_List."),
                UpdatedAt = row.Field<DateTime>("UpdatedAt"),
                InvoiceLink = row.Table.Columns.Contains("InvoiceLink") ? row.Field<string?>("InvoiceLink") : null,
                CreatedAt = row.Table.Columns.Contains("CreatedAt") ? row.Field<DateTime?>("CreatedAt") : null,
                Active = row.Table.Columns.Contains("Active") ? row.Field<bool?>("Active") : null
            };
        }

        private static RegistrationEntity RegistrationToEntity(DataRow row, string idColumn)
        {
            ArgumentNullException.ThrowIfNull(row);

            return new RegistrationEntity
            {
                Id = (Guid)row[idColumn],
                UserId = (Guid)row["UserId"],
                ClassId = row.Field<Guid?>("ClassId"),
                RegistrationDate = row.Field<DateTime?>("RegistrationDate"),
                Status = (string)row["Status"],
                InvoiceLink = row.Field<string?>("InvoiceLink"),
                CreatedAt = (DateTime)row["CreatedAt"],
                UpdatedAt = (DateTime)row["UpdatedAt"],
                Active = (bool)row["Active"]
            };
        }

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

