namespace Maynard.ErrorHandling;

internal enum ErrorCode
{
    ExtensionMethodFailure = 1_000,
    EnumOutOfBounds = 1_100,
    EnumFlagsMisused = 1_200,
    DependencyInjectionFailure = 1_300,
    DatabaseFailure = 1_400,
    ConnectionFailure = 1_500,
    InvalidValue = 1_600
}