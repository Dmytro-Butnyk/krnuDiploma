using FluentValidation;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;

internal abstract class DiplomaExaminationCommissionUpdateValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : DiplomaExaminationCommissionUpdateRequest
{
    protected DiplomaExaminationCommissionUpdateValidator()
    {
        Include(new DiplomaExaminationCommissionCommonValidator<TRequest>());
    }
}
