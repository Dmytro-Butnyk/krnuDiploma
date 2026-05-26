namespace DiplomaControlSystem.Api.Contracts.CommissionHeads;

public static class CommissionHeadContracts
{
    public sealed record CommissionHeadDto(
        int Id,
        string FullName,
        string Position,
        string Company,
        string Specialty,
        bool IsDeleted);
}
