using FluentValidation;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);
            var errors = _validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .Select(f => f.ErrorMessage)
                .ToList();

            if (errors.Count == 0)
                return await next();

            var responseType = typeof(TResponse);
            if (responseType.IsGenericType &&
                responseType.GetGenericTypeDefinition() == typeof(Response<>))
            {
                var innerType = responseType.GetGenericArguments()[0];
                var validationErrorMethod = typeof(Response<>)
                    .MakeGenericType(innerType)
                    .GetMethod("ValidationError")!;

                var result = validationErrorMethod.Invoke(null, [errors, "Doğrulama hatası."]);
                return (TResponse)result!;
            }

            throw new System.ComponentModel.DataAnnotations.ValidationException(string.Join(", ", errors));
        }
    }
}
