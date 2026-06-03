namespace PriceWise.Application.Common;

public sealed class Result<T> : Result
{
    private readonly T? value;

    private Result(T value)
        : base(true, Error.None)
    {
        this.value = value;
    }

    private Result(Error error)
        : base(false, error)
    {
    }

    public T Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new(error);
}
