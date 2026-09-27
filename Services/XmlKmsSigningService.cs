using System.Text.Json;

namespace SolidSign.Examples.UsecaseDiploma;

/// <summary>
/// [EN]    Generic single-document XAdES signing call via a SolidSign KMS-custodied certificate
///         (POST /solidsign/dsig/xml/sign-kms). Shared by every document/step of the Diploma
///         Digital use case; when nodeName is null/empty, the whole document is signed (no
///         signatureNodeName/signatureNodeNamespace sent).
/// [PT-BR] Chamada genérica de assinatura XAdES via certificado custodiado no KMS da SolidSign.
///         Compartilhada por todos os documentos/etapas do caso de uso Diploma Digital; quando
///         nodeName é nulo/vazio, o documento inteiro é assinado (sem signatureNodeName/Namespace).
/// </summary>
public class XmlKmsSigningService(IConfiguration cfg, HttpClient http)
{
    private string BaseUrl => cfg["SolidSign:Api:BaseUrl"]!.TrimEnd('/');
    private string Auth => cfg["SolidSign:Api:Authorization"]!;

    public async Task<byte[]?> SignAsync(Stream document, string fileName, string kmsCode,
        string? nodeName, string? @namespace, string profile, string hashAlgorithm,
        string signaturePackaging, string canonicalizationMethod, bool removeXPathFilter)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(document), "document[0]", fileName);
        form.Add(new StringContent(kmsCode), "kmsCode");
        form.Add(new StringContent(profile), "profile");
        form.Add(new StringContent(hashAlgorithm), "hashAlgorithm");
        form.Add(new StringContent(signaturePackaging), "signaturePackaging");
        form.Add(new StringContent(canonicalizationMethod), "canonicalizationMethod");
        if (!string.IsNullOrWhiteSpace(nodeName))
        {
            form.Add(new StringContent(nodeName), "signatureNodeName[0]");
            form.Add(new StringContent(@namespace ?? ""), "signatureNodeNamespace[0]");
        }
        form.Add(new StringContent(removeXPathFilter.ToString().ToLowerInvariant()), "isRemoveXPathExclusionFilter");

        var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/solidsign/dsig/xml/sign-kms") { Content = form };
        req.Headers.TryAddWithoutValidation("Authorization", Auth);
        var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"SolidSign API error {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");
            return null;
        }

        var signResp = JsonSerializer.Deserialize<SignResponse>(await resp.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var href = signResp?.Documents.FirstOrDefault()?.HalLinks?.Self?.Href
                ?? signResp?.Documents.FirstOrDefault()?.Links?.FirstOrDefault(l => l.Rel == "self")?.Href;
        if (href is null)
        {
            Console.Error.WriteLine("SolidSign response had no download link.");
            return null;
        }

        var dlReq = new HttpRequestMessage(HttpMethod.Get, href);
        dlReq.Headers.TryAddWithoutValidation("Authorization", Auth);
        var dlResp = await http.SendAsync(dlReq);
        return dlResp.IsSuccessStatusCode ? await dlResp.Content.ReadAsByteArrayAsync() : null;
    }
}
