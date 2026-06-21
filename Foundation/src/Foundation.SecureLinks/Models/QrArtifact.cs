namespace Foundation.SecureLinks.Models;

public sealed record QrArtifact(
    string ContentType,
    string FileName,
    byte[] Bytes,
    string DataUri);
