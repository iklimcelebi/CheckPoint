using System.ComponentModel.DataAnnotations;

namespace CheckPoint.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TrimmedStringLengthAttribute(int maximumLength) : StringLengthAttribute(maximumLength)
{
    public override bool IsValid(object? value) =>
        value is not string text || base.IsValid(text.Trim());
}
