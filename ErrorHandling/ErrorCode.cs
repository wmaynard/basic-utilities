namespace Maynard.ErrorHandling;

internal enum ErrorCode
{
    Unknown = 0,
    ExtensionMethodFailure = 1_000,
    EnumOutOfBounds = 1_100,
    EnumFlagsMisused = 1_200,
    DependencyInjectionFailure = 1_300,
    DatabaseFailure = 1_400,
    ConnectionFailure = 1_500,
    InvalidValue = 1_600,
    NotConfigured = 1_700,
    ExpiredToken = 1_800,
    InvalidToken = 1_900,
    InvalidJson = 2_000,
    InvalidRequestData = 2_100,
}