using System.Xml.Linq;

namespace SolidSign.Examples.UsecaseDiploma;

/// <summary>
/// [EN]    Assembles the Diploma XML by copying the (already signed) &lt;DadosDiploma&gt; node
///         out of the Documentação Acadêmica de Registro document and inserting it into the
///         Diploma envelope template (templates/diploma-template.xml), right before
///         &lt;DadosRegistro&gt;. The result is an UNSIGNED Diploma XML.
/// [PT-BR] Monta o XML do Diploma copiando o nó &lt;DadosDiploma&gt; (já assinado) de dentro do
///         documento de Documentação Acadêmica de Registro e inserindo-o no template do envelope
///         Diploma, logo antes de &lt;DadosRegistro&gt;. O resultado é um XML AINDA NÃO ASSINADO.
/// </summary>
public class DiplomaAssemblyService
{
    private const string TemplatePath = "templates/diploma-template.xml";

    public async Task<byte[]?> AssembleAsync(Stream signedDocumentacaoAcademica)
    {
        try
        {
            var academicDoc = await XDocument.LoadAsync(signedDocumentacaoAcademica, LoadOptions.None, default);
            var dadosDiploma = academicDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "DadosDiploma");
            if (dadosDiploma is null)
            {
                Console.Error.WriteLine("Could not find <DadosDiploma> in the supplied Documentação Acadêmica document.");
                return null;
            }

            var diplomaDoc = XDocument.Load(TemplatePath);
            var dadosRegistro = diplomaDoc.Descendants().First(e => e.Name.LocalName == "DadosRegistro");
            dadosRegistro.AddBeforeSelf(new XElement(dadosDiploma));

            using var ms = new MemoryStream();
            await diplomaDoc.SaveAsync(ms, SaveOptions.None, default);
            return ms.ToArray();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Unexpected error assembling the Diploma XML: {e.Message}");
            return null;
        }
    }
}
