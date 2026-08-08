namespace FlooringManager.Api.Auth;

public sealed class SupabaseAuthOptions
{
    public const string SectionName = "Supabase";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = "authenticated";
}