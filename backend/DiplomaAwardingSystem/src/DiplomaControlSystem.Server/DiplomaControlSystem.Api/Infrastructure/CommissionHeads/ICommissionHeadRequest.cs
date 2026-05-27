namespace DiplomaControlSystem.Api.Infrastructure.CommissionHeads;

internal interface ICommissionHeadRequest
{
    string FullName { get; }
    string Position { get; }
    string Company { get; }
    string Specialty { get; }
}
