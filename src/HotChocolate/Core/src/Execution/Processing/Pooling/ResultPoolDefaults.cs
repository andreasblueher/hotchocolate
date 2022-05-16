namespace HotChocolate.Execution.Processing.Pooling;

internal static class ResultPoolDefaults
{
    public const int MaximumRetained = 512;
    public const int BucketSize = 16;
    public const int MaximumAllowedCapacity = 256;
}