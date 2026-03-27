using AutoLCPR.Application.DTOs;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;

namespace AutoLCPR.Application.Services;

public sealed class ExtratoRebanhoPdfParserService : IExtratoRebanhoPdfParserService
{
    public Task<ExtratoRebanhoPdfDTO> ParseAsync(string caminhoPdf, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(caminhoPdf))
        {
            throw new ArgumentException("O caminho do PDF nao foi informado.", nameof(caminhoPdf));
        }

        if (!File.Exists(caminhoPdf))
        {
            throw new FileNotFoundException("Arquivo de extrato nao encontrado.", caminhoPdf);
        }

        var extensao = Path.GetExtension(caminhoPdf);
        if (!string.Equals(extensao, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Formato invalido. O extrato deve estar em PDF.");
        }

        try
        {
            var resultado = ExtrairDadosPdf(caminhoPdf);
            resultado.ArquivoOrigem = caminhoPdf;
            resultado.ParserImplementado = true;
            resultado.Observacao = "Dados extraidos com sucesso do extrato do rebanho.";
            
            return Task.FromResult(resultado);
        }
        catch (Exception ex)
        {
            var resultado = new ExtratoRebanhoPdfDTO
            {
                ArquivoOrigem = caminhoPdf,
                ParserImplementado = false,
                Observacao = $"Erro ao processar PDF: {ex.Message}"
            };

            return Task.FromResult(resultado);
        }
    }

    private ExtratoRebanhoPdfDTO ExtrairDadosPdf(string caminhoPdf)
    {
        var dto = new ExtratoRebanhoPdfDTO();

        using (var pdfReader = new PdfReader(caminhoPdf))
        using (var pdfDocument = new PdfDocument(pdfReader))
        {
            var textoCompleto = ExtrairTextoCompletoDoPdf(pdfDocument);
            var linhas = textoCompleto.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Extrair nome da propriedade e inscricao
            ExtrairFichaSanitaria(textoCompleto, linhas, dto);

            // Extrair saldos e operacoes
            ExtrairSaldosEOperacoes(linhas, dto);
        }

        return dto;
    }

    private string ExtrairTextoCompletoDoPdf(PdfDocument pdfDocument)
    {
        var sb = new System.Text.StringBuilder();

        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
        {
            var page = pdfDocument.GetPage(i);
            var extrator = new SimpleTextExtractionStrategy();
            var texto = PdfTextExtractor.GetTextFromPage(page, extrator);
            sb.AppendLine(texto);
        }

        return sb.ToString();
    }

    private void ExtrairFichaSanitaria(string textoCompleto, string[] linhas, ExtratoRebanhoPdfDTO dto)
    {
        var produtorFicha = ExtrairValorDoRotulo(
            linhas,
            textoCompleto,
            "Produtor",
            "Município",
            "Municipio",
            "Região/ZF",
            "Regiao/ZF",
            "Saldo");

        var municipioFicha = ExtrairValorDoRotulo(
            linhas,
            textoCompleto,
            "Município",
            "Municipio",
            "Região/ZF",
            "Regiao/ZF",
            "Saldo");

        // Estratégia 1 (preferencial): tentar extrair na própria linha da Ficha Sanitária.
        for (var i = 0; i < linhas.Length; i++)
        {
            var linha = linhas[i];
            if (!(linha.Contains("Ficha", StringComparison.OrdinalIgnoreCase)
                  && linha.Contains("Sanit", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Primeiro tenta somente a linha atual para evitar capturar campos de outras linhas.
            if (TentarExtrairFichaDeTrecho(linha, dto, produtorFicha, municipioFicha))
            {
                break;
            }

            // Se a linha atual vier truncada, tenta com a próxima linha como fallback curto.
            if (i + 1 < linhas.Length)
            {
                var trecho2Linhas = string.Concat(linha, " ", linhas[i + 1]);
                if (TentarExtrairFichaDeTrecho(trecho2Linhas, dto, produtorFicha, municipioFicha))
                {
                    break;
                }
            }
        }

        // Normaliza quebras para fallback global.
        var textoNormalizado = textoCompleto.Replace("\r", " ").Replace("\n", " ");

        // Fallback: busca inscrição perto da âncora "Ficha Sanit" mesmo sem o restante do padrão.
        if (string.IsNullOrWhiteSpace(dto.Inscricao))
        {
            var matchInscAnchor = Regex.Match(
                textoNormalizado,
                @"Ficha\s*Sanit[^:]*:\s*(?<insc>[0-9\s\.\-]{9,20})",
                RegexOptions.IgnoreCase);

            if (matchInscAnchor.Success)
            {
                var inscricaoNormalizada = NormalizarSomenteDigitos(matchInscAnchor.Groups["insc"].Value);
                if (inscricaoNormalizada.Length >= 9)
                {
                    dto.Inscricao = inscricaoNormalizada.Substring(0, 9);
                }
            }
        }

        // Fallback: busca nome após "<inscrição> -" com inscrição já normalizada.
        if (string.IsNullOrWhiteSpace(dto.NomePropriedade) && !string.IsNullOrWhiteSpace(dto.Inscricao))
        {
            var padraoNome = $@"{dto.Inscricao}\s*[-–—]\s*(?<nome>[^\r\n]+?)\s*(?:Produtor:|Munic[ií]pio:|Saldo|$)";
            var matchNome = Regex.Match(textoNormalizado, padraoNome, RegexOptions.IgnoreCase);
            if (matchNome.Success)
            {
                dto.NomePropriedade = LimparNomePropriedade(matchNome.Groups["nome"].Value, produtorFicha, municipioFicha);
            }
        }

        // Último fallback: procura padrão genérico "9 dígitos - nome" no texto.
        if (string.IsNullOrWhiteSpace(dto.Inscricao) || string.IsNullOrWhiteSpace(dto.NomePropriedade))
        {
            var matchGenerico = Regex.Match(
                textoNormalizado,
                @"(?<insc>\d{9})\s*[-–—]\s*(?<nome>[^\r\n]+?)\s*(?:Produtor:|Munic[ií]pio:|Saldo|$)",
                RegexOptions.IgnoreCase);

            if (matchGenerico.Success)
            {
                if (string.IsNullOrWhiteSpace(dto.Inscricao))
                {
                    dto.Inscricao = matchGenerico.Groups["insc"].Value;
                }

                if (string.IsNullOrWhiteSpace(dto.NomePropriedade))
                {
                    dto.NomePropriedade = LimparNomePropriedade(matchGenerico.Groups["nome"].Value, produtorFicha, municipioFicha);
                }
            }
        }

        if (string.IsNullOrWhiteSpace(dto.Inscricao) || string.IsNullOrWhiteSpace(dto.NomePropriedade))
        {
            dto.Observacao = "Falha parcial na leitura da Ficha Sanitaria (inscricao/nome).";
        }
    }

    private static bool TentarExtrairFichaDeTrecho(string trecho, ExtratoRebanhoPdfDTO dto, string? produtorFicha, string? municipioFicha)
    {
        var match = Regex.Match(
            trecho,
            @"Ficha\s*Sanit[^:]*:\s*(?<insc>[0-9\s\.\-]{9,20})\s*[-–—]\s*(?<nome>.+)$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return false;
        }

        var inscricaoNormalizada = NormalizarSomenteDigitos(match.Groups["insc"].Value);
        if (inscricaoNormalizada.Length >= 9)
        {
            dto.Inscricao = inscricaoNormalizada.Substring(0, 9);
        }

        dto.NomePropriedade = LimparNomePropriedade(match.Groups["nome"].Value, produtorFicha, municipioFicha);
        return !string.IsNullOrWhiteSpace(dto.Inscricao) && !string.IsNullOrWhiteSpace(dto.NomePropriedade);
    }

    private static string? LimparNomePropriedade(string? nomeBruto, string? produtorFicha, string? municipioFicha)
    {
        if (string.IsNullOrWhiteSpace(nomeBruto))
            return null;

        var nome = nomeBruto.Trim();
        nome = Regex.Replace(nome, @"\s+", " ").Trim();

        // Remove rótulos que podem vir na mesma linha após a propriedade.
        var marcadoresFim = new[]
        {
            "Produtor:",
            "Município:",
            "Municipio:",
            "Inscrição:",
            "INSCRICAO:",
            "Região/ZF:",
            "Regiao/ZF:",
            "Área Espécie:",
            "Area Especie:",
            "Hectares",
            "Saldo"
        };

        foreach (var marcador in marcadoresFim)
        {
            var pos = nome.IndexOf(marcador, StringComparison.OrdinalIgnoreCase);
            if (pos > 0)
            {
                nome = nome.Substring(0, pos).Trim();
            }
        }

        nome = RemoverSufixoCampo(nome, produtorFicha);
        nome = RemoverSufixoCampo(nome, municipioFicha);
        nome = TruncarAntesDoPrimeiroToken(nome, produtorFicha);
        nome = TruncarAntesDoPrimeiroToken(nome, municipioFicha);

        // Alguns PDFs inserem pontuações/caracteres de controle entre palavras do nome.
        // Ex.: "FAZENDA VERTENTE.FRESCA" pode virar corte em "FAZENDA VERTENTE".
        var nomeParaMatch = Regex.Replace(nome, @"[\.,;:_|]+", " ");
        nomeParaMatch = Regex.Replace(nomeParaMatch, @"\s+", " ").Trim();

        // Quando o extrator achata linhas, o nome da propriedade pode vir seguido do nome do produtor
        // e do município. Nestes casos, preserva apenas o bloco iniciando por um prefixo rural comum.
        var matchNomeRural = Regex.Match(
            nomeParaMatch,
            @"\b(?<nome>(FAZENDA|SITIO|SÍTIO|CHACARA|CHÁCARA|ESTANCIA|ESTÂNCIA|RANCHO|GRANJA|RECANTO|GLEBA|LOTE)\b(?:\s+[A-Za-zÀ-ÖØ-öø-ÿ0-9][A-Za-zÀ-ÖØ-öø-ÿ0-9\-/]*){0,8})",
            RegexOptions.IgnoreCase);

        if (matchNomeRural.Success)
        {
            nome = matchNomeRural.Groups["nome"].Value.Trim();
        }

        // Remove múltiplos espaços e pontuação residual no final.
        nome = Regex.Replace(nome, @"\s+", " ").Trim().Trim('-', ':', ';', ',', '.');

        return string.IsNullOrWhiteSpace(nome) ? null : nome;
    }

    private static string? ExtrairValorDoRotulo(string[] linhas, string textoCompleto, string rotuloBase, params string[] proximosRotulos)
    {
        var padraoLinha = $@"\b{Regex.Escape(rotuloBase)}\s*:\s*(?<valor>.+)$";
        foreach (var linha in linhas)
        {
            var match = Regex.Match(linha, padraoLinha, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                continue;
            }

            var valor = match.Groups["valor"].Value.Trim();
            return string.IsNullOrWhiteSpace(valor) ? null : valor;
        }

        // Fallback quando o extrator quebra linha/coluna: busca no texto completo normalizado.
        var textoNormalizado = Regex.Replace(textoCompleto, @"\s+", " ");
        var proximos = proximosRotulos != null && proximosRotulos.Length > 0
            ? string.Join("|", proximosRotulos.Select(Regex.Escape))
            : "$";

        var padraoTexto = $@"\b{Regex.Escape(rotuloBase)}\s*:\s*(?<valor>.+?)(?:\b(?:{proximos})\b\s*:|$)";
        var matchTexto = Regex.Match(textoNormalizado, padraoTexto, RegexOptions.IgnoreCase);
        if (matchTexto.Success)
        {
            var valor = matchTexto.Groups["valor"].Value.Trim();
            return string.IsNullOrWhiteSpace(valor) ? null : valor;
        }

        return null;
    }

    private static string RemoverSufixoCampo(string nome, string? valorCampo)
    {
        if (string.IsNullOrWhiteSpace(valorCampo))
        {
            return nome;
        }

        var valor = Regex.Replace(valorCampo.Trim(), @"\s+", " ");
        var nomeNormalizado = Regex.Replace(nome, @"\s+", " ");
        var indice = nomeNormalizado.IndexOf(valor, StringComparison.OrdinalIgnoreCase);
        if (indice > 0 && indice >= nomeNormalizado.Length / 2)
        {
            var candidato = nomeNormalizado.Substring(0, indice).Trim();
            if (CortePreservaEstruturaNomeRural(nomeNormalizado, candidato))
            {
                return candidato;
            }
        }

        return nomeNormalizado;
    }

    private static string TruncarAntesDoPrimeiroToken(string nome, string? valorCampo)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(valorCampo))
        {
            return nome;
        }

        var tokens = Regex.Split(valorCampo.Trim(), @"\s+")
            .Where(t => t.Length >= 3)
            .ToArray();

        if (tokens.Length == 0)
        {
            return nome;
        }

        var nomeProcessado = nome;
        foreach (var token in tokens)
        {
            var padrao = $@"\b{Regex.Escape(token)}\b";
            var match = Regex.Match(nomeProcessado, padrao, RegexOptions.IgnoreCase);
            if (match.Success && match.Index > 0 && match.Index >= nomeProcessado.Length / 2)
            {
                var candidato = nomeProcessado.Substring(0, match.Index).Trim();
                if (CortePreservaEstruturaNomeRural(nomeProcessado, candidato))
                {
                    nomeProcessado = candidato;
                    break;
                }
            }
        }

        return nomeProcessado;
    }

    private static bool CortePreservaEstruturaNomeRural(string original, string candidato)
    {
        if (string.IsNullOrWhiteSpace(candidato))
        {
            return false;
        }

        var palavrasOriginal = Regex.Matches(original, @"[A-Za-zÀ-ÖØ-öø-ÿ0-9]+", RegexOptions.IgnoreCase).Count;
        var palavrasCandidato = Regex.Matches(candidato, @"[A-Za-zÀ-ÖØ-öø-ÿ0-9]+", RegexOptions.IgnoreCase).Count;

        var comecaComPrefixoRural = Regex.IsMatch(
            original,
            @"^\s*(FAZENDA|SITIO|SÍTIO|CHACARA|CHÁCARA|ESTANCIA|ESTÂNCIA|RANCHO|GRANJA|RECANTO|GLEBA|LOTE)\b",
            RegexOptions.IgnoreCase);

        // Para nome rural, evita cortes que derrubam para menos de 3 palavras quando o original tinha 3 ou mais.
        if (comecaComPrefixoRural && palavrasOriginal >= 3 && palavrasCandidato < 3)
        {
            return false;
        }

        return true;
    }

    private static string NormalizarSomenteDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return Regex.Replace(valor, "\\D", string.Empty);
    }

    private void ExtrairSaldosEOperacoes(string[] linhas, ExtratoRebanhoPdfDTO dto)
    {
        int saldoInicial = 0;
        int totalNascimentos = 0;
        int totalMortesConsumo = 0;
        int totalEntradas = 0;
        int totalSaidas = 0;
        int saldoFinal = 0;

        bool encontrouAbertura = false;

        for (int i = 0; i < linhas.Length; i++)
        {
            var linha = linhas[i].Trim();

            if (string.IsNullOrWhiteSpace(linha))
                continue;

            // Procurar pelo primeiro "SALDO ANTERIOR"
            if (linha.Contains("SALDO ANTERIOR", StringComparison.OrdinalIgnoreCase))
            {
                encontrouAbertura = true;
                // O proximo "Saldo:" eh o saldo inicial
                for (int j = i + 1; j < linhas.Length && j < i + 5; j++)
                {
                    var proxLinha = linhas[j].Trim();
                    if (proxLinha.StartsWith("Saldo:", StringComparison.OrdinalIgnoreCase))
                    {
                        saldoInicial = ExtrairValorGeralDeSaldo(proxLinha);
                        break;
                    }
                }
                continue;
            }

            if (!encontrouAbertura)
                continue;

            // Processar operacoes - verificar a primeira coluna/palavra
            var palavrasLinha = linha.Split(new[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
            
            if (palavrasLinha.Length == 0)
                continue;

            var operacao = palavrasLinha[0];

            // Nascimentos
            if (operacao.Equals("CR-NASCIMENTO", StringComparison.OrdinalIgnoreCase))
            {
                totalNascimentos += ExtrairValorGeralDeOperacao(linha);
            }
            // Mortes
            else if (operacao.Equals("DB-MORTE", StringComparison.OrdinalIgnoreCase))
            {
                totalMortesConsumo += Math.Abs(ExtrairValorGeralDeOperacao(linha));
            }
            // Consumo
            else if (operacao.Equals("DB-CONSUMO", StringComparison.OrdinalIgnoreCase))
            {
                totalMortesConsumo += Math.Abs(ExtrairValorGeralDeOperacao(linha));
            }
            // Entradas
            else if ((operacao.Equals("CR-eGTA", StringComparison.OrdinalIgnoreCase) || 
                      operacao.StartsWith("CR-ENTRADA", StringComparison.OrdinalIgnoreCase)))
            {
                if (!EhMovimentacaoEra(linha, operacao))
                {
                    var valor = ExtrairValorGeralDeOperacao(linha);
                    if (valor > 0)
                        totalEntradas += valor;
                }
            }
            // Saidas
            else if (operacao.Equals("DB-eGTA", StringComparison.OrdinalIgnoreCase))
            {
                if (!EhMovimentacaoEra(linha, operacao))
                {
                    totalSaidas += Math.Abs(ExtrairValorGeralDeOperacao(linha));
                }
            }
            else if (operacao.StartsWith("DB-SAIDA", StringComparison.OrdinalIgnoreCase))
            {
                if (!EhMovimentacaoEra(linha, operacao))
                {
                    totalSaidas += Math.Abs(ExtrairValorGeralDeOperacao(linha));
                }
            }

            // Procurar por "Saldo:" para atualizar o saldo final
            if (linha.StartsWith("Saldo:", StringComparison.OrdinalIgnoreCase))
            {
                saldoFinal = ExtrairValorGeralDeSaldo(linha);
            }
        }

        dto.SaldoInicial = saldoInicial > 0 ? saldoInicial : null;
        dto.Nascimentos = totalNascimentos > 0 ? totalNascimentos : null;
        dto.MortesConsumos = totalMortesConsumo > 0 ? totalMortesConsumo : null;
        dto.Entradas = totalEntradas > 0 ? totalEntradas : null;
        dto.Saidas = totalSaidas > 0 ? totalSaidas : null;
        dto.SaldoFinal = saldoFinal > 0 ? saldoFinal : null;
    }

    private int ExtrairValorGeralDeOperacao(string linha)
    {
        // A coluna "Geral" eh o ultimo numero na linha
        var partes = linha.Split(new[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (partes.Length > 0 && int.TryParse(partes[partes.Length - 1], out int valor))
        {
            return valor;
        }

        return 0;
    }

    private static bool EhMovimentacaoEra(string linha, string operacao)
    {
        // Regra de negócio: entradas/saídas com ERA não entram nos totais.
        return operacao.Contains("ERA", StringComparison.OrdinalIgnoreCase)
               || linha.Contains(" ERA ", StringComparison.OrdinalIgnoreCase)
               || linha.Contains("-ERA", StringComparison.OrdinalIgnoreCase)
               || linha.Contains("/ERA", StringComparison.OrdinalIgnoreCase)
               || linha.Contains("(ERA", StringComparison.OrdinalIgnoreCase)
               || linha.Contains("ERA)", StringComparison.OrdinalIgnoreCase);
    }

    private int ExtrairValorGeralDeSaldo(string linha)
    {
        // Procurar por "Saldo:" e extrair o ultimo numero inteiro da linha
        if (!linha.Contains("Saldo:", StringComparison.OrdinalIgnoreCase))
            return 0;

        var partes = linha.Split(new[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
        
        // O ultimo elemento eh o valor do saldo geral
        if (partes.Length > 0 && int.TryParse(partes[partes.Length - 1], out int valor))
        {
            return valor;
        }

        return 0;
    }
}
