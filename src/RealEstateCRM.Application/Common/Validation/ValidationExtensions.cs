using FluentValidation;
using RealEstateCRM.Application.Common.Exceptions;

namespace RealEstateCRM.Application.Common.Validation;

public static class ValidationExtensions
{
    /// <summary>Runs the validator and throws a 400 AppException (consistent ProblemDetails shape) if invalid.</summary>
    public static async Task ValidateAndThrowAppExceptionAsync<T>(
        this IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
            throw new AppException(message, 400);
        }
    }
}
