using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Aceca.Adm.Helper
{
    /// <summary>
    /// Resolve URL de asset (~/...) com cache-busting manual (timestamp do arquivo),
    /// sem depender do LinkTagHelper/ScriptTagHelper (asp-append-version) - dentro de um
    /// mesmo @section, só a PRIMEIRA tag &lt;link&gt;/&lt;script&gt; com asp-append-version é
    /// processada pelo Tag Helper; as demais ficam com o "~/" literal no HTML final
    /// (404 "MIME type ('')" no console, CSS/JS não carrega). Reproduzido isolando a
    /// variável (arquivo, ordem, conteúdo entre as tags) - o problema é sempre "a partir
    /// da 2ª tag do section", não do arquivo específico.
    /// </summary>
    public static class AssetUrlHelper
    {
        public static string ComVersao(IUrlHelper url, IWebHostEnvironment env, string caminhoApp)
        {
            var caminhoFisico = Path.Combine(env.WebRootPath, caminhoApp.TrimStart('~', '/'));

            var versao = File.Exists(caminhoFisico)
                ? File.GetLastWriteTimeUtc(caminhoFisico).Ticks.ToString()
                : "0";

            return $"{url.Content(caminhoApp)}?v={versao}";
        }
    }
}
