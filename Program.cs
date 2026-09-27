/**
 * [EN]    Diploma Digital (MEC) use case — all 5 documents, KMS custody.
 *         Run: dotnet run
 * [PT-BR] Caso de uso Diploma Digital (MEC) — os 5 documentos, custódia KMS.
 *         Executar: dotnet run
 */
using SolidSign.Examples.UsecaseDiploma;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<XmlKmsSigningService>();
builder.Services.AddSingleton<DiplomaAssemblyService>();
var app = builder.Build();

string HashAlg(IConfiguration cfg) => cfg["SolidSign:Diploma:HashAlgorithm"] ?? "SHA256";
string Packaging(IConfiguration cfg) => cfg["SolidSign:Diploma:SignaturePackaging"] ?? "ENVELOPED";
string Canon(IConfiguration cfg) => cfg["SolidSign:Diploma:CanonicalizationMethod"] ?? "EXCLUSIVE";

async Task<IResult> HandleStep(HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg,
    string? nodeName, string? ns, string profile, bool removeFilter, string outputName)
{
    var form = await req.ReadFormAsync();
    var document = form.Files.GetFile("document");
    var kmsCode = form["kmsCode"].ToString();
    if (document is null || string.IsNullOrWhiteSpace(kmsCode))
        return Results.BadRequest(new { error = "document and kmsCode are required" });

    await using var stream = document.OpenReadStream();
    var signed = await svc.SignAsync(stream, document.FileName, kmsCode, nodeName, ns, profile,
        HashAlg(cfg), Packaging(cfg), Canon(cfg), removeFilter);
    return signed is not null ? Results.File(signed, "application/xml", outputName) : Results.Problem("Signing failed. Check logs.");
}

// ─── Documento 1/5 — Documentação Acadêmica de Registro ────────────────────
app.MapPost("/api/diploma/documentacao-academica/step1-representante", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, cfg["SolidSign:DocumentacaoAcademica:Step1:NodeName"], cfg["SolidSign:DocumentacaoAcademica:Step1:Namespace"],
        cfg["SolidSign:DocumentacaoAcademica:Step1:Profile"]!, bool.Parse(cfg["SolidSign:DocumentacaoAcademica:Step1:RemoveXPathFilter"]!), "documentacao_academica_step1_signed.xml"));

app.MapPost("/api/diploma/documentacao-academica/step2-emissora-dados", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, cfg["SolidSign:DocumentacaoAcademica:Step2:NodeName"], cfg["SolidSign:DocumentacaoAcademica:Step2:Namespace"],
        cfg["SolidSign:DocumentacaoAcademica:Step2:Profile"]!, bool.Parse(cfg["SolidSign:DocumentacaoAcademica:Step2:RemoveXPathFilter"]!), "documentacao_academica_step2_signed.xml"));

app.MapPost("/api/diploma/documentacao-academica/step3-envelope-final", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, cfg["SolidSign:DocumentacaoAcademica:Step3:NodeName"], cfg["SolidSign:DocumentacaoAcademica:Step3:Namespace"],
        cfg["SolidSign:DocumentacaoAcademica:Step3:Profile"]!, bool.Parse(cfg["SolidSign:DocumentacaoAcademica:Step3:RemoveXPathFilter"]!), "documentacao_academica_step3_signed.xml"));

// ─── Documento 2/5 — Diploma Digital (montagem + 2 etapas) ─────────────────
app.MapPost("/api/diploma/diploma/assemble", async (HttpRequest req, DiplomaAssemblyService svc) =>
{
    var form = await req.ReadFormAsync();
    var document = form.Files.GetFile("signedDocumentacaoAcademica");
    if (document is null) return Results.BadRequest(new { error = "signedDocumentacaoAcademica is required" });
    await using var stream = document.OpenReadStream();
    var assembled = await svc.AssembleAsync(stream);
    return assembled is not null ? Results.File(assembled, "application/xml", "diploma_montado.xml") : Results.BadRequest(new { error = "Assembly failed. Check logs." });
});

app.MapPost("/api/diploma/diploma/step1-registradora-dados", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, cfg["SolidSign:DiplomaDoc:Step1:NodeName"], cfg["SolidSign:DiplomaDoc:Step1:Namespace"],
        cfg["SolidSign:DiplomaDoc:Step1:Profile"]!, bool.Parse(cfg["SolidSign:DiplomaDoc:Step1:RemoveXPathFilter"]!), "diploma_step1_signed.xml"));

app.MapPost("/api/diploma/diploma/step2-envelope-final", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, cfg["SolidSign:DiplomaDoc:Step2:NodeName"], cfg["SolidSign:DiplomaDoc:Step2:Namespace"],
        cfg["SolidSign:DiplomaDoc:Step2:Profile"]!, bool.Parse(cfg["SolidSign:DiplomaDoc:Step2:RemoveXPathFilter"]!), "diploma_step2_signed.xml"));

// ─── Documento 3/5 — Histórico Escolar Digital ─────────────────────────────
app.MapPost("/api/diploma/historico-escolar/step1-secretaria-dados", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, cfg["SolidSign:HistoricoEscolar:Step1:NodeName"], cfg["SolidSign:HistoricoEscolar:Step1:Namespace"],
        cfg["SolidSign:HistoricoEscolar:Step1:Profile"]!, bool.Parse(cfg["SolidSign:HistoricoEscolar:Step1:RemoveXPathFilter"]!), "historico_escolar_step1_signed.xml"));

app.MapPost("/api/diploma/historico-escolar/step2-envelope-final", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, cfg["SolidSign:HistoricoEscolar:Step2:NodeName"], cfg["SolidSign:HistoricoEscolar:Step2:Namespace"],
        cfg["SolidSign:HistoricoEscolar:Step2:Profile"]!, bool.Parse(cfg["SolidSign:HistoricoEscolar:Step2:RemoveXPathFilter"]!), "historico_escolar_step2_signed.xml"));

// ─── Documento 4/5 — Currículo Escolar Digital (documento inteiro) ─────────
app.MapPost("/api/diploma/curriculo-escolar/step1-coordenador", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, null, null,
        cfg["SolidSign:CurriculoEscolar:Step1:Profile"]!, bool.Parse(cfg["SolidSign:CurriculoEscolar:Step1:RemoveXPathFilter"]!), "curriculo_escolar_step1_signed.xml"));

app.MapPost("/api/diploma/curriculo-escolar/step2-envelope-final", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, null, null,
        cfg["SolidSign:CurriculoEscolar:Step2:Profile"]!, bool.Parse(cfg["SolidSign:CurriculoEscolar:Step2:RemoveXPathFilter"]!), "curriculo_escolar_step2_signed.xml"));

// ─── Documento 5/5 — Lista de Diplomas Anulados / Arquivo de Fiscalização ──
app.MapPost("/api/diploma/lista-anulados/sign", (HttpRequest req, XmlKmsSigningService svc, IConfiguration cfg) =>
    HandleStep(req, svc, cfg, null, null,
        cfg["SolidSign:ListaAnulados:Profile"]!, bool.Parse(cfg["SolidSign:ListaAnulados:RemoveXPathFilter"]!), "lista_anulados_signed.xml"));

app.Run("http://0.0.0.0:5095");
