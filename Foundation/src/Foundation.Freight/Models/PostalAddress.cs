namespace Foundation.Freight.Models;

public sealed record PostalAddress(
    string PostalCode,
    string? Street = null,
    string? Number = null,
    string? Neighborhood = null,
    string? City = null,
    string? State = null,
    string? CountryCode = null);
