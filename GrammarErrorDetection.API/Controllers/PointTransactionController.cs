using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.PointTransactions;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class PointTransactionController(IPointTransactionService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Student,Teacher,Coordinator")]
    public async Task<Wrapper<List<PointTransactionEntity>>> List([FromQuery] PointTransactionListQuery request)
    {
        request.Filter ??= new();
        request.Filter.UserId = ReadUserId(request.Filter.UserId);
        var data = await service.List(request);
        return new() { Data = data, Count = data.Count };
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Coordinator")]
    public async Task<Wrapper<PointTransactionEntity>> Insert([FromBody] PointTransactionInsert request)
    {
        var data = await service.Insert(request);
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    [HttpPut]
    [Authorize(Roles = "Teacher,Coordinator")]
    public async Task<Wrapper<PointTransactionEntity>> Update([FromBody] PointTransactionUpdate request)
        => new() { Data = await service.Update(request) };

    [HttpGet("total")]
    [Authorize(Roles = "Student,Teacher,Coordinator")]
    public async Task<Wrapper<PointTransactionTotalDto>> Total([FromQuery] PointTransactionTotalQuery request)
    {
        request.UserId = ReadUserId(request.UserId);
        return new() { Data = await service.Total(request) };
    }

    private Guid? ReadUserId(Guid? requestedId)
    {
        if (User.IsInRole("Teacher") || User.IsInRole("Coordinator"))
            return requestedId;

        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
            ? id : throw new UnauthorizedAccessException("El token no contiene un identificador de usuario válido.");
    }
}
