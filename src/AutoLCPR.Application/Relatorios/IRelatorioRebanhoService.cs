namespace AutoLCPR.Application.Relatorios;

public interface IRelatorioRebanhoService
{
    byte[] GerarRelatorioRebanho(int ano);
}
