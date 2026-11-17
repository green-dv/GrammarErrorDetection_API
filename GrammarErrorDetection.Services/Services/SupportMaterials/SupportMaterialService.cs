using GrammarErrorDetection.Models.Grammar.Dashboards;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Models.Grammar.FileTypes;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Models.Grammar.Classes;

namespace GrammarErrorDetection.Services.Services
{
    public sealed class SupportMaterialService : ISupportMaterialService
    {
        private readonly ISupportMaterialRepository repository;

        public SupportMaterialService(ISupportMaterialRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            this.repository = repository;
        }

        public Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request, ClaimsPrincipal? viewer = null)
        {
            ArgumentNullException.ThrowIfNull(request);
            var viewerId = StudentId(viewer);
            if (request.Filter?.OnlyHelpful == true && viewerId is null)
                throw new ValidationException("Solo los estudiantes pueden filtrar los materiales que marcaron como útiles.");
            return repository.List(request, viewerId);
        }

        public Task<PagedList<SupportMaterialListItemDto>> ListPage(PaginationDto<SupportMaterialList> request, ClaimsPrincipal? viewer = null)
        {
            ArgumentNullException.ThrowIfNull(request);
            var viewerId = StudentId(viewer);
            if (request.Filter?.OnlyHelpful == true && viewerId is null)
                throw new ValidationException("Solo los estudiantes pueden filtrar los materiales que marcaron como útiles.");
            return repository.ListPage(request, viewerId);
        }

        // Solo el estudiante tiene interacciones propias (vistas y "útil"); para otros roles no se consulta.
        private static Guid? StudentId(ClaimsPrincipal? viewer) =>
            viewer?.Identity?.IsAuthenticated == true && viewer.IsInRole("Student")
            && Guid.TryParse(viewer.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) && id != Guid.Empty
                ? id : null;

        public async Task<SupportMaterialListItemDto> Get(Guid id, ClaimsPrincipal? viewer = null)
        {
            if (id == Guid.Empty) throw new ArgumentException("El identificador de material no puede estar vacío.", nameof(id));
            var materials = await repository.List(new PaginationDto<SupportMaterialList>
            {
                PageNumber = 1, PageSize = 1, Filter = new SupportMaterialList { Id = id }
            }, StudentId(viewer));
            return materials.SingleOrDefault(item => item.Id == id)
                ?? throw new KeyNotFoundException("No se encontró el material de apoyo.");
        }

        public Task<SupportMaterialEntity> Insert(SupportMaterialInsert request) => repository.Insert(request);
        public Task<SupportMaterialEntity> Update(SupportMaterialUpdate request) => repository.Update(request);
    }

    public sealed class FileTypeService(IFileTypeRepository repository) : IFileTypeService
    {
        public Task<List<FileTypeEntity>> List(PaginationDto<FileTypeList> request) => repository.List(request);

        public Task<PagedList<FileTypeEntity>> ListPage(PaginationDto<FileTypeList> request) => repository.ListPage(request);

        public async Task<FileTypeEntity> Get(Guid id)
        {
            if (id == Guid.Empty) throw new ArgumentException("El ID no puede estar vacío.", nameof(id));
            var items = await repository.List(new PaginationDto<FileTypeList>
            {
                PageNumber = 1, PageSize = 1, Filter = new FileTypeList { Id = id }
            });
            return items.FirstOrDefault(item => item.Id == id)
                ?? throw new KeyNotFoundException("No se encontró el registro.");
        }
    }
}

namespace GrammarErrorDetection.Services.Services.SupportMaterials
{
    /// <summary>
    /// Registra lo que el estudiante hace con el material recomendado (vistas y "¿Te sirvió?").
    /// Esas interacciones alimentan el puntaje personalizado de SupportMaterial_Recommendation_List.
    /// Solo el propio estudiante registra sus interacciones: el usuario sale siempre del token.
    /// </summary>
    public sealed class SupportMaterialInteractionService(ISupportMaterialRepository materials) : ISupportMaterialInteractionService
    {
        public Task<SupportMaterialInteractionEntity> RegisterView(ClaimsPrincipal user, Guid supportMaterialId,
            SupportMaterialViewRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var student = ResolveStudent(user);
            ValidateMaterial(supportMaterialId);
            Validator.ValidateObject(request, new ValidationContext(request), true);
            return materials.RegisterView(student, supportMaterialId, request.RegistrationId, request.Downloaded);
        }

        public Task<SupportMaterialInteractionEntity> SetFeedback(ClaimsPrincipal user, Guid supportMaterialId,
            SupportMaterialFeedbackRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var student = ResolveStudent(user);
            ValidateMaterial(supportMaterialId);
            Validator.ValidateObject(request, new ValidationContext(request), true);
            var helpful = request.Helpful ?? throw new ValidationException("Helpful es obligatorio.");
            // El comentario solo explica un "no me sirvió"; con un voto positivo se descarta.
            var comment = helpful || string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
            return materials.SetFeedback(student, supportMaterialId, helpful, comment);
        }

        private static Guid ResolveStudent(ClaimsPrincipal user)
        {
            ArgumentNullException.ThrowIfNull(user);
            if (user.Identity?.IsAuthenticated != true
                || !Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actor) || actor == Guid.Empty)
                throw new UnauthorizedAccessException();
            if (!user.IsInRole("Student"))
                throw new ReportAccessDeniedException("Solo los estudiantes registran su avance con el material de apoyo.");
            return actor;
        }

        private static void ValidateMaterial(Guid supportMaterialId)
        {
            if (supportMaterialId == Guid.Empty) throw new ValidationException("El material es obligatorio.");
        }
    }

    /// <summary>
    /// Recomienda material de apoyo según las clasificaciones de error más frecuentes del estudiante.
    /// El ranking lo calcula SupportMaterial_Recommendation_List; aquí se aplica el control de acceso
    /// y se agrupan las filas por clasificación.
    /// </summary>
    public sealed class SupportMaterialRecommendationService(ISupportMaterialRepository materials, IClassRepository classes)
        : ISupportMaterialRecommendationService
    {
        public async Task<List<SupportMaterialRecommendationDto>> Recommend(ClaimsPrincipal user,
            SupportMaterialRecommendationQuery request)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(request);
            Validator.ValidateObject(request, new ValidationContext(request), true);
            var (userId, classId) = await ResolveScope(user, request);

            var rows = await materials.ListRecommendations(userId, classId, request.CategoryLimit, request.MaterialsPerCategory);
            return rows
                .GroupBy(row => row.Material.ErrorClassificationId)
                .Select(group =>
                {
                    var first = group.First();
                    return new SupportMaterialRecommendationDto
                    {
                        ErrorClassificationId = group.Key,
                        ErrorClassificationName = first.ErrorClassificationName,
                        ErrorClassificationDescription = first.ErrorClassificationDescription,
                        ErrorCount = first.ErrorCount,
                        RelatedTextCount = first.RelatedTextCount,
                        ErrorPercentage = first.ErrorPercentage,
                        LastErrorAt = first.LastErrorAt,
                        Rank = first.CategoryRank,
                        Materials = group.Select(row => row.Material).OrderBy(material => material.Rank).ToList()
                    };
                })
                .OrderBy(category => category.Rank)
                .ToList();
        }

        // Estudiante: solo sus propios errores. Docente: estudiantes de sus cursos (ClassId obligatorio).
        // Coordinador: cualquier estudiante. La inscripción aprobada en ClassId la valida la base de datos.
        private async Task<(Guid UserId, Guid? ClassId)> ResolveScope(ClaimsPrincipal user, SupportMaterialRecommendationQuery request)
        {
            if (user.Identity?.IsAuthenticated != true
                || !Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actor) || actor == Guid.Empty)
                throw new UnauthorizedAccessException();

            if (user.IsInRole("Coordinator"))
                return (request.UserId ?? throw new ValidationException("UserId es obligatorio."), request.ClassId);

            if (user.IsInRole("Teacher"))
            {
                if (request.UserId is not Guid student) throw new ValidationException("UserId es obligatorio.");
                if (request.ClassId is not Guid classId) throw new ValidationException("ClassId es obligatorio para docentes.");
                var course = (await classes.List(new PaginationDto<ClassList>
                {
                    PageNumber = 1, PageSize = 1, Filter = new() { Id = classId, TeacherId = actor }
                })).SingleOrDefault(item => item.Id == classId);
                if (course is null || course.TeacherId != actor) Denied();
                return (student, classId);
            }

            if (user.IsInRole("Student"))
            {
                if (request.UserId.HasValue && request.UserId != actor) Denied();
                return (actor, request.ClassId);
            }

            Denied();
            return default;
        }

        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        private static void Denied() =>
            throw new ReportAccessDeniedException("No tienes acceso a las recomendaciones del estudiante solicitado.");
    }
}
