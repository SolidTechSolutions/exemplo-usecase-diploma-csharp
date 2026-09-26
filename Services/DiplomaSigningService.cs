using System.Text.Json;

namespace SolidSign.Examples.UsecaseDiploma;

/// <summary>
/// [EN]    Signs the Diploma Digital XML at a specific stage of the MEC "Diploma Digital" flow
///         using a SolidSign KMS-custodied certificate (POST /solidsign/dsig/xml/sign-kms).
///         Each stage corresponds to a different real-world signer role and is exposed as an
///         isolated endpoint in Program.cs — this service does not chain the 3 stages together,
///         since each one is normally executed by a different signer/system.
/// [PT-BR] Assina o XML do Diploma Digital em uma etapa específica do fluxo do MEC "Diploma
///         Digital" usando um certificado custodiado no KMS da SolidSign
///         (POST /solidsign/dsig/xml/sign-kms). Cada etapa corresponde a um papel real de
///         assinante distinto e é exposta como um endpoint isolado em Program.cs — este service
///         não encadeia as 3 etapas, já que cada uma normalmente é executada por um
///         assinante/sistema diferente.
/// </summary>
public class DiplomaSigningService(IConfiguration cfg, HttpClient http)
{
    private string BaseUrl => cfg["SolidSign:Api:BaseUrl"]!.TrimEnd('/');
    private string Auth => cfg["SolidSign:Api:Authorization"]!;
    private string HashAlg => cfg["SolidSign:Diploma:HashAlgorithm"] ?? "SHA256";
    private string SigPackaging => cfg["SolidSign:Diploma:SignaturePackaging"] ?? "ENVELOPED";
    private string CanonMethod => cfg["SolidSign:Diploma:CanonicalizationMethod"] ?? "EXCLUSIVE";

    public Task<byte[]?> SignStep1RepresentanteAsync(Stream document, string fileName, string kmsCode) =>
        SignAsync(document, fileName, kmsCode,
            cfg["SolidSign:Diploma:Step1:NodeName"]!, cfg["SolidSign:Diploma:Step1:Namespace"]!,
            cfg["SolidSign:Diploma:Step1:Profile"]!, cfg["SolidSign:Diploma:Step1:RemoveXPathFilter"]!);

    public Task<byte[]?> SignStep2EmissoraDadosAsync(Stream document, string fileName, string kmsCode) =>
        SignAsync(document, fileName, kmsCode,
            cfg["SolidSign:Diploma:Step2:NodeName"]!, cfg["SolidSign:Diploma:Step2:Namespace"]!,
            cfg["SolidSign:Diploma:Step2:Profile"]!, cfg["SolidSign:Diploma:Step2:RemoveXPathFilter"]!);

    public Task<byte[]?> SignStep3EnvelopeFinalAsync(Stream document, string fileName, string kmsCode) =>
        SignAsync(document, fileName, kmsCode,
            cfg["SolidSign:Diploma:Step3:NodeName"]!, cfg["SolidSign:Diploma:Step3:Namespace"]!,
            cfg["SolidSign:Diploma:Step3:Profile"]!, cfg["SolidSign:Diploma:Step3:RemoveXPathFilter"]!);

    /// <summary>
    /// [EN]    Calls SolidSign's XML KMS signing endpoint for a single document and returns the
    ///         signed XML bytes downloaded from the response link.
    /// [PT-BR] Chama o endpoint de assinatura XML via KMS da SolidSign para um único documento e
    ///         retorna os bytes do XML assinado baixados do link da resposta.
    /// </summary>
    private async Task<byte[]?> SignAsync(Stream document, string fileName, string kmsCode,
        string nodeName, string @namespace, string profile, string removeXPathFilter)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(document), "document[0]", fileName);
        form.Add(new StringContent(kmsCode), "kmsCode");
        form.Add(new StringContent(profile), "profile");
        form.Add(new StringContent(HashAlg), "hashAlgorithm");
        form.Add(new StringContent(SigPackaging), "signaturePackaging");
        form.Add(new StringContent(CanonMethod), "canonicalizationMethod");
        form.Add(new StringContent(nodeName), "signatureNodeName[0]");
        form.Add(new StringContent(@namespace), "signatureNodeNamespace[0]");
        form.Add(new StringContent(removeXPathFilter), "isRemoveXPathExclusionFilter");

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
