using AutoLCPR.Domain.Entities;

namespace AutoLCPR.Application.DTOs
{
    /// <summary>
    /// DTO para Nota Fiscal importada
    /// </summary>
    public class NotaFiscalDTO
    {
        /// <summary>
        /// Chave de Acesso da NFe
        /// </summary>
        public string Chave { get; set; } = string.Empty;

        /// <summary>
        /// Natureza da Operação
        /// </summary>
        public string Natureza { get; set; } = string.Empty;

        /// <summary>
        /// Descrição concatenada (primeiras palavras dos produtos/serviços)
        /// </summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// CFOPs encontrados concatenados
        /// </summary>
        public string CFOP { get; set; } = string.Empty;

        /// <summary>
        /// Número da NF-e
        /// </summary>
        public string NumeroNota { get; set; } = string.Empty;

        /// <summary>
        /// Data de emissão da NF-e
        /// </summary>
        public DateTime? DataEmissao { get; set; }

        /// <summary>
        /// Nome do emitente
        /// </summary>
        public string EmitenteNome { get; set; } = string.Empty;

        /// <summary>
        /// CPF/CNPJ do emitente
        /// </summary>
        public string EmitenteCpfCnpj { get; set; } = string.Empty;

        /// <summary>
        /// Nome do destinatário/remetente
        /// </summary>
        public string DestinatarioNome { get; set; } = string.Empty;

        /// <summary>
        /// CPF/CNPJ do destinatário
        /// </summary>
        public string DestinatarioCpfCnpj { get; set; } = string.Empty;

        /// <summary>
        /// Valor total da nota
        /// </summary>
        public decimal ValorTotal { get; set; }

        /// <summary>
        /// Tipo de lançamento (Receita ou Despesa)
        /// </summary>
        public TipoLancamento Tipo { get; set; }
    }
}
