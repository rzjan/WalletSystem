namespace WalletSystem.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("Un resultado exitoso no puede tener error.");
        }
        if (!isSuccess && error is not null)
        {
            throw new InvalidOperationException("Un resutado fallido debe tener un error.");
        }
        IsSuccess = isSuccess;
        Error = error;
    }
    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");

    protected internal Result(T? value, bool isSuccess, string? error)
       : base(isSuccess, error)
    {
        _value = value;
    }
}