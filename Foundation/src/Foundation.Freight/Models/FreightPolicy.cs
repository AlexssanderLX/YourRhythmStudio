namespace Foundation.Freight.Models;

public sealed record FreightPolicy(
    decimal BaseFee,
    decimal RatePerKm,
    decimal MinimumCharge = 0,
    int RoundDigits = 2);
