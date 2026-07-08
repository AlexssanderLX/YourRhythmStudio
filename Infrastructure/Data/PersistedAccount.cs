namespace YourRhythmStudio.Infrastructure.Data;

/// <summary>
/// Persiste os dados de autenticação no MySQL para que as contas sobrevivam a reinicializações.
/// Foundation.Access usa InMemoryAccountStore; este registro permite recarregar as contas na subida.
/// </summary>
public sealed class PersistedAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string PlatformRole { get; set; } = "None";
    public string? PwdAlgorithm { get; set; }
    public int PwdIterations { get; set; }
    public string? PwdSaltBase64 { get; set; }
    public string? PwdHashBase64 { get; set; }
    public DateTime? PwdUpdatedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public string? SecurityStamp { get; set; }
}
