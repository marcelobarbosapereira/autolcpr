using System.Globalization;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace AutoLCPR.Application.Relatorios.Common;

internal static class RelatorioGraficoFactory
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static byte[] CriarGraficoComparativo(IReadOnlyList<ResumoMensalDto> dados)
    {
        const int largura = 1000;
        const int altura = 380;

        using var superficie = SKSurface.Create(new SKImageInfo(largura, altura));
        var canvas = superficie.Canvas;
        canvas.Clear(SKColors.White);

        var margemEsquerda = 70f;
        var margemDireita = 30f;
        var margemTopo = 35f;
        var margemInferior = 55f;

        var areaLargura = largura - margemEsquerda - margemDireita;
        var areaAltura = altura - margemTopo - margemInferior;

        using var linhaPaint = new SKPaint { Color = new SKColor(180, 180, 180), StrokeWidth = 1f, IsAntialias = true };
        using var textoPaint = new SKPaint { Color = new SKColor(70, 70, 70), TextSize = 16f, IsAntialias = true };
        using var receitaPaint = new SKPaint { Color = new SKColor(34, 139, 34), IsAntialias = true };
        using var despesaPaint = new SKPaint { Color = new SKColor(220, 53, 69), IsAntialias = true };

        canvas.DrawLine(margemEsquerda, margemTopo, margemEsquerda, margemTopo + areaAltura, linhaPaint);
        canvas.DrawLine(margemEsquerda, margemTopo + areaAltura, margemEsquerda + areaLargura, margemTopo + areaAltura, linhaPaint);

        var maxValor = dados.Any() ? dados.Max(item => Math.Max(item.Receita, item.Despesa)) : 0m;
        if (maxValor <= 0)
        {
            maxValor = 1m;
        }

        var slot = areaLargura / 12f;
        var barraLargura = Math.Max(8f, slot * 0.28f);

        for (var indice = 0; indice < 12; indice++)
        {
            var item = dados.FirstOrDefault(x => x.Mes == indice + 1)
                ?? new ResumoMensalDto
                {
                    Mes = indice + 1,
                    NomeMes = PtBr.DateTimeFormat.GetAbbreviatedMonthName(indice + 1),
                    Receita = 0m,
                    Despesa = 0m
                };

            var centro = margemEsquerda + (slot * indice) + (slot / 2f);
            var alturaReceita = (float)(item.Receita / maxValor) * areaAltura;
            var alturaDespesa = (float)(item.Despesa / maxValor) * areaAltura;

            var receitaRect = SKRect.Create(centro - barraLargura - 2, margemTopo + areaAltura - alturaReceita, barraLargura, alturaReceita);
            var despesaRect = SKRect.Create(centro + 2, margemTopo + areaAltura - alturaDespesa, barraLargura, alturaDespesa);

            canvas.DrawRect(receitaRect, receitaPaint);
            canvas.DrawRect(despesaRect, despesaPaint);

            var nomeMesCurto = PtBr.DateTimeFormat.GetAbbreviatedMonthName(indice + 1).Replace(".", string.Empty);
            canvas.DrawText(nomeMesCurto, centro - 13, altura - 22, textoPaint);
        }

        canvas.DrawText("Receitas", largura - 210, 30, textoPaint);
        canvas.DrawRect(SKRect.Create(largura - 285, 18, 22, 12), receitaPaint);
        canvas.DrawText("Despesas", largura - 110, 30, textoPaint);
        canvas.DrawRect(SKRect.Create(largura - 185, 18, 22, 12), despesaPaint);

        using var imagem = superficie.Snapshot();
        using var data = imagem.Encode(SKEncodedImageFormat.Png, 100);
        if (data == null)
        {
            throw new InvalidOperationException("Falha ao codificar o gráfico comparativo.");
        }

        return data.ToArray();
    }
}