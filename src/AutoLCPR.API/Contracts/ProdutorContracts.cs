namespace AutoLCPR.API.Contracts;

public sealed record ProdutorRequest(string Nome, string Cpf, string? InscricaoEstadual);

public sealed record ProdutorResponse(int Id, string Nome, string Cpf, string InscricaoEstadual, DateTime CreatedAt, DateTime? UpdatedAt);
