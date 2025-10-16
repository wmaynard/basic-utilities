namespace Maynard.Auth;

[AttributeUsage(validOn: AttributeTargets.Method | AttributeTargets.Class)]
public class RequireAuth(AuthType type = AuthType.StandardToken) : Attribute
{
    public readonly AuthType Type = type;
}

[Flags]
public enum AuthType
{
    Permissions = 0b_0001, // TODO
    StandardToken = 0b_0010,
    AdminToken = 0b_0110,
    Secret = 0b_1110,
    Optional = 0b_1111
}

[AttributeUsage(validOn: AttributeTargets.Method)]
public class NoAuth : Attribute { }