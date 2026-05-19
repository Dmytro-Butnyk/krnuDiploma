using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Students;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Students;

public static class AddStudent
{
    public sealed record Request(string SecretaryEmail, string FullName);

    public sealed record Response(int StudentId, string FullName, int GroupId);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SecretaryEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(256);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/groups/{groupId:int}/students", Handle)
                .WithSummary("Adds a student to a group with default diploma data")
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Students");
        }

        private static async Task<Results<Created<Response>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int groupId,
            [FromBody] Request request,
            [FromServices] IValidator<Request> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(groupId, request, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Created($"/api/students/{result.Value!.StudentId}", result.Value);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<Response>> HandleAsync(
            int groupId,
            Request request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var group = await context.Groups
                .FirstOrDefaultAsync(
                    g => g.Id == groupId && g.SpecialtyId == secretary.SpecialtyId,
                    ct);

            if (group is null)
            {
                return ErrorDetails.NotFound(
                    "Group.NotFound",
                    "Group was not found or does not belong to secretary specialty.");
            }

            var fullName = request.FullName.Trim();
            var studentExists = await context.Students
                .AnyAsync(
                    student => student.GroupId == groupId && student.FullName == fullName,
                    ct);

            if (studentExists)
            {
                return ErrorDetails.Conflict(
                    "Student.AlreadyExists",
                    "Student with the same full name already exists in this group.");
            }

            var student = StudentDraftFactory.Create(fullName);
            group.Students.Add(student);

            await context.SaveChangesAsync(ct);

            return new Response(student.Id, student.FullName, group.Id);
        }
    }
}
