/**
 * [EN]    Diploma Digital (MEC) use case — 3 isolated signer endpoints, KMS custody.
 *         Run: dotnet run
 *         Step 1: POST http://localhost:5095/api/diploma/step1-representante
 *         Step 2: POST http://localhost:5095/api/diploma/step2-emissora-dados
 *         Step 3: POST http://localhost:5095/api/diploma/step3-envelope-final
 *
 * [PT-BR] Caso de uso Diploma Digital (MEC) — 3 endpoints isolados por assinante, custódia KMS.
 *         Executar: dotnet run
 */
using SolidSign.Examples.UsecaseDiploma;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<DiplomaSigningService>();
var app = builder.Build();

async Task<IResult> HandleStep(HttpRequest req, DiplomaSigningService svc,
    Func<Stream, string, string, Task<byte[]?>> signFn, string outputName)
{
    var form = await req.ReadFormAsync();
    var document = form.Files.GetFile("document");
    var kmsCode = form["kmsCode"].ToString();
    if (document is null || string.IsNullOrWhiteSpace(kmsCode))
        return Results.BadRequest(new { error = "document and kmsCode are required" });

    await using var stream = document.OpenReadStream();
    var signed = await signFn(stream, document.FileName, kmsCode);
    return signed is not null
        ? Results.File(signed, "application/xml", outputName)
        : Results.Problem("Signing failed. Check logs.");
}

app.MapPost("/api/diploma/step1-representante", (HttpRequest req, DiplomaSigningService svc) =>
    HandleStep(req, svc, svc.SignStep1RepresentanteAsync, "diploma_step1_signed.xml"));

app.MapPost("/api/diploma/step2-emissora-dados", (HttpRequest req, DiplomaSigningService svc) =>
    HandleStep(req, svc, svc.SignStep2EmissoraDadosAsync, "diploma_step2_signed.xml"));

app.MapPost("/api/diploma/step3-envelope-final", (HttpRequest req, DiplomaSigningService svc) =>
    HandleStep(req, svc, svc.SignStep3EnvelopeFinalAsync, "diploma_step3_signed.xml"));

app.Run("http://0.0.0.0:5095");
