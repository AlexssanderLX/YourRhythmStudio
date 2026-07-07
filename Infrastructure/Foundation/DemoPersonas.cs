using YourRhythmStudio.Domain;

namespace YourRhythmStudio.Infrastructure.Foundation;

/// <summary>
/// Contas demo do MVP: permitem entrar como cada tipo de usuário e cair no
/// dashboard correspondente, sem depender do MySQL. O papel de domínio
/// (escola/professor/aluno) é resolvido pelo e-mail e injetado como claim no login.
/// </summary>
public sealed record DemoPersona(string Email, string DisplayName, string Role);

public static class DemoPersonas
{
    public const string DemoPassword = "YourRhythm@123";

    public static readonly IReadOnlyList<DemoPersona> All = new[]
    {
        new DemoPersona("escola@yourrhythm.local", "Escola Harmonia", YourRhythmRoles.SchoolOwner),
        new DemoPersona("professor@yourrhythm.local", "Helena Reis", YourRhythmRoles.Teacher),
        new DemoPersona("aluno@yourrhythm.local", "Sofia Almeida", YourRhythmRoles.Student),
    };

    /// <summary>Resolve o papel YourRhythm a partir do e-mail (case-insensitive).</summary>
    public static string? ResolveRole(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        return All.FirstOrDefault(p => string.Equals(p.Email, trimmed, StringComparison.OrdinalIgnoreCase))?.Role;
    }
}
