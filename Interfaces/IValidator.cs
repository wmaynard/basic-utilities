using Maynard.ErrorHandling;

namespace Maynard.Interfaces;

public interface IValidator
{
    public void Validate(out List<string> errors);

    public void Validate()
    {
        Validate(out List<string> errors);
        if (errors.Any())
            throw new InternalException("An object failed validation.", ErrorCode.InvalidValue, new
            {
                Type = GetType().Name,
                Errors = errors
            });
    }
}