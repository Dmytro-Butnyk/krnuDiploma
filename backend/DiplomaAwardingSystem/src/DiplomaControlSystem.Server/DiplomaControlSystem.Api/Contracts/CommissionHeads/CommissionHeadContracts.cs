using DiplomaControlSystem.Api.Contracts.Common;

namespace DiplomaControlSystem.Api.Contracts.CommissionHeads;

public static class CommissionHeadContracts
{
    public sealed record CommissionHeadDto(
        int Id,
        string FullName,
        PersonNameFormsDto NameForms,
        string Position,
        string Company,
        string Specialty,
        bool IsDeleted);
}
