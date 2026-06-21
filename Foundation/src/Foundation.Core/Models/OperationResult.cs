namespace Foundation.Core.Models;

public class OperationResult
{
    protected OperationResult(bool isSuccess, OperationError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public OperationError? Error { get; }

    public static OperationResult Success() => new(true, null);

    public static OperationResult Failure(OperationError error) => new(false, error);
}

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(T value) : base(true, null)
    {
        Value = value;
    }

    private OperationResult(OperationError error) : base(false, error)
    {
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value) => new(value);

    public new static OperationResult<T> Failure(OperationError error) => new(error);
}
