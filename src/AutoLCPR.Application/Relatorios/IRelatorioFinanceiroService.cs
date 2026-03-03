using AutoLCPR.Domain.Entities;

namespace AutoLCPR.Application.Relatorios;

public interface IRelatorioFinanceiroService
{
    byte[] GerarRelatorioFinanceiro(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo);
}
