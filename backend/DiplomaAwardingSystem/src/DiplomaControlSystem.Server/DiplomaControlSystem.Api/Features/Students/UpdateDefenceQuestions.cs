using Core.Api.Extensions;
using Core.Domain.Entities.ArchiveGroup;
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

public static class UpdateDefenceQuestions
{
    public sealed record UpdateDefenceQuestionsRequest(IReadOnlyCollection<DefenceQuestionDto> Questions);

    public sealed record UpdateDefenceQuestionsResponse(
        int StudentId,
        IReadOnlyCollection<DefenceQuestionDto> Questions);

    internal sealed class Validator : AbstractValidator<UpdateDefenceQuestionsRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Questions)
                .NotNull()
                .Must(x => x.Count <= 5)
                .WithMessage("No more than 5 defence questions are allowed.");

            RuleForEach(x => x.Questions).ChildRules(question =>
            {
                question.RuleFor(x => x.AskedBy).NotEmpty().MaximumLength(256);
                question.RuleFor(x => x.Text).NotEmpty().MaximumLength(1000);
            });
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/students/{studentId:int}/qualification-work/defence-questions", Handle)
                .WithSummary("Updates qualification work defence questions")
                .Produces<UpdateDefenceQuestionsResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Students");
        }

        private static async Task<Results<Ok<UpdateDefenceQuestionsResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int studentId,
            [FromBody] UpdateDefenceQuestionsRequest request,
            [FromServices] IValidator<UpdateDefenceQuestionsRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(studentId, request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        StudentAccessService studentAccessService,
        DefenceQuestionAuthorOptionsProvider defenceQuestionAuthorOptionsProvider) : IScopedService
    {
        public async Task<Result<UpdateDefenceQuestionsResponse>> HandleAsync(
            int studentId,
            UpdateDefenceQuestionsRequest request,
            CancellationToken ct)
        {
            var accessResult = await studentAccessService.GetForCurrentSecretaryAsync(studentId, ct);
            if (accessResult.IsFailure)
            {
                return accessResult.ErrorDetails;
            }

            var authorOptions = await defenceQuestionAuthorOptionsProvider.GetByStudentIdAsync(studentId, ct);
            var questionsResult = MapQuestions(request.Questions, authorOptions);
            if (questionsResult.IsFailure)
            {
                return questionsResult.ErrorDetails;
            }

            var student = await context.Students
                .Include(s => s.QualificationWork)
                .FirstOrDefaultAsync(s => s.Id == studentId, ct);

            if (student is null)
            {
                return ErrorDetails.NotFound("Student.NotFound", "Student was not found.");
            }

            var qualificationWork = StudentDiplomaDataInitializer.EnsureQualificationWork(student);
            qualificationWork.DefenceQuestions.Clear();
            foreach (var question in questionsResult.Value!)
            {
                qualificationWork.DefenceQuestions.Add(question);
            }

            await context.SaveChangesAsync(ct);

            return new UpdateDefenceQuestionsResponse(
                student.Id,
                qualificationWork.DefenceQuestions.Select(DefenceQuestionDto.From).ToList());
        }

        private static Result<IReadOnlyCollection<DefenceQuestion>> MapQuestions(
            IReadOnlyCollection<DefenceQuestionDto> questions,
            IReadOnlyCollection<DefenceQuestionAuthorOptionDto> authorOptions)
        {
            if (questions.Count > 0 && authorOptions.Count == 0)
            {
                return ErrorDetails.Validation(
                    "DefenceQuestion.AuthorOptionsUnavailable",
                    "Student group does not have a diploma examination commission with question author options.");
            }

            var mappedQuestions = new List<DefenceQuestion>(questions.Count);
            foreach (var question in questions)
            {
                var authorShortName = DefenceQuestionAuthorOptionsProvider.GetCanonicalShortName(
                    authorOptions,
                    question.AskedBy);

                if (authorShortName is null)
                {
                    return ErrorDetails.Validation(
                        "DefenceQuestion.AuthorInvalid",
                        "Question author must be selected from the student group commission.");
                }

                mappedQuestions.Add(new DefenceQuestion(authorShortName, question.Text.Trim()));
            }

            return mappedQuestions;
        }
    }
}
