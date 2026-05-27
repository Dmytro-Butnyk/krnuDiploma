using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Auth;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Auth;

public static class LoginWithGoogle
{
    public sealed record LoginWithGoogleRequest(string IdToken);

    public sealed record LoginWithGoogleResponse(
        string AccessToken,
        int ExpiresInSeconds,
        SecretaryProfileDto Secretary);

    public sealed record SecretaryProfileDto(
        int Id,
        string Email,
        string FullName,
        int SpecialtyId,
        string SpecialtyName,
        bool IsSuperSecretary);

    internal sealed class Validator : AbstractValidator<LoginWithGoogleRequest>
    {
        public Validator()
        {
            RuleFor(x => x.IdToken)
                .NotEmpty();
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/google", Handle)
                .AllowAnonymous()
                .WithSummary("Authenticates a registered secretary with Google")
                .Produces<LoginWithGoogleResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .WithTags("Auth");
        }

        private static async Task<Results<Ok<LoginWithGoogleResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromBody] LoginWithGoogleRequest request,
            [FromServices] IValidator<LoginWithGoogleRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(request, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        GoogleIdTokenValidator googleIdTokenValidator,
        JwtTokenService jwtTokenService,
        Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions) : IScopedService
    {
        public async Task<Result<LoginWithGoogleResponse>> HandleAsync(
            LoginWithGoogleRequest request,
            CancellationToken ct)
        {
            var googleUserResult = await googleIdTokenValidator.ValidateAsync(request.IdToken, ct);
            if (googleUserResult.IsFailure)
            {
                return googleUserResult.ErrorDetails;
            }

            var googleUser = googleUserResult.Value!;
            var secretary = await context.Secretaries
                .Include(s => s.Specialty)
                .FirstOrDefaultAsync(s => s.GoogleSubject == googleUser.Subject, ct);

            if (secretary is null)
            {
                secretary = await context.Secretaries
                    .Include(s => s.Specialty)
                    .FirstOrDefaultAsync(s => EF.Functions.ILike(s.Email, googleUser.Email), ct);
            }

            if (secretary is null)
            {
                return ErrorDetails.Unauthorized(
                    "Auth.SecretaryNotRegistered",
                    "Google account is not registered as a secretary.");
            }

            if (!secretary.IsActive)
            {
                return ErrorDetails.Forbidden(
                    "Secretary.Inactive",
                    "Secretary account is inactive.");
            }

            if (secretary.Specialty is null || !secretary.Specialty.IsActive)
            {
                return ErrorDetails.Forbidden(
                    "Specialty.Inactive",
                    "Secretary specialty is inactive.");
            }

            if (secretary.GoogleSubject is null)
            {
                secretary.GoogleSubject = googleUser.Subject;
                await context.SaveChangesAsync(ct);
            }
            else if (!string.Equals(secretary.GoogleSubject, googleUser.Subject, StringComparison.Ordinal))
            {
                return ErrorDetails.Unauthorized(
                    "Auth.GoogleAccountMismatch",
                    "Google account is not linked to this secretary.");
            }

            var accessToken = jwtTokenService.CreateAccessToken(secretary);
            return new LoginWithGoogleResponse(
                accessToken,
                jwtOptions.Value.AccessTokenMinutes * 60,
                new SecretaryProfileDto(
                    secretary.Id,
                    secretary.Email,
                    secretary.FullName,
                    secretary.SpecialtyId,
                    secretary.Specialty.Name,
                    secretary.IsSuperSecretary));
        }
    }
}
