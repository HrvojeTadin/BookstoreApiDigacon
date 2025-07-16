namespace BookstoreApi.DTOs;
public static class AuthRoles
{
    public const string Read = "Read";
    public const string ReadWrite = "ReadWrite";
}

public static class AuthPolicies
{
    public const string RequireReadRole = "RequireReadRole";
    public const string RequireReadWriteRole = "RequireReadWriteRole";
}
