using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Contracts.Common;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Students;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Students;

public static class UpdateStudentName
{
    public sealed record UpdateStudentNameRequest(
        string LastName,
        string FirstName,
        string MiddleName,
        PersonNameFormsDto? NameForms);

    public sealed record UpdateStudentNameResponse(int StudentId, string FullName, PersonNameFormsDto NameForms);

    internal sealed class Validator : AbstractValidator<UpdateStudentNameRequest>
    {
        public Validator()
        {
            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.MiddleName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.NameForms!.Nominative).MaximumLength(256).When(x => x.NameForms is not null);
            RuleFor(x => x.NameForms!.Genitive).MaximumLength(256).When(x => x.NameForms is not null);
            RuleFor(x => x.NameForms!.Dative).MaximumLength(256).When(x => x.NameForms is not null);
            RuleFor(x => x.NameForms!.Signature).MaximumLength(256).When(x => x.NameForms is not null);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/students/{studentId:int}/name", Handle)
                .WithSummary("Updates student full name")
                .Produces<UpdateStudentNameResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Students");
        }

        private static async Task<Results<Ok<UpdateStudentNameResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int studentId,
            [FromBody] UpdateStudentNameRequest request,
            [FromServices] IValidator<UpdateStudentNameRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(studentId, request, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        StudentAccessService studentAccessService) : IScopedService
    {
        public async Task<Result<UpdateStudentNameResponse>> HandleAsync(
            int studentId,
            UpdateStudentNameRequest request,
            CancellationToken ct)
        {
            var accessResult = await studentAccessService.GetForCurrentSecretaryAsync(studentId, ct);
            if (accessResult.IsFailure)
            {
                return accessResult.ErrorDetails;
            }

            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
            if (student is null)
            {
                return ErrorDetails.NotFound(
                    "Student.NotFound",
                    "Student was not found.");
            }

            var name = StudentNameParser.Build(request.LastName, request.FirstName, request.MiddleName);
            student.FullName = name.FullName;
            student.NameForms = request.NameForms?.ToDomain(student.FullName)
                ?? Core.Domain.Entities.PersonNameForms.FromDefault(student.FullName);

            await context.SaveChangesAsync(ct);

            return new UpdateStudentNameResponse(student.Id, student.FullName, PersonNameFormsDto.From(student.NameForms));
        }
    }
}
