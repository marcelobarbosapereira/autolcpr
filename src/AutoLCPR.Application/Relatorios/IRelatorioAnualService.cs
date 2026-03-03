namespace AutoLCPR.Application.Relatorios;

public interface IRelatorioAnualService
{
    byte[] GerarRelatorioAnual(int anoFiscal);
}
