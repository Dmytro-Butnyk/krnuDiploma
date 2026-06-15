using DiplomaControlSystem.Api.Contracts.Common;

namespace DiplomaControlSystem.Api.Infrastructure.CommissionHeads;

internal interface ICommissionHeadRequest
{
    string FullName { get; }
    PersonNameFormsDto? NameForms { get; }
    string Position { get; }
    string Company { get; }
    string Specialty { get; }
}
