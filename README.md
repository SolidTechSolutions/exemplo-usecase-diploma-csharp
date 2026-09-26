# 🇧🇷 SolidSign API - Caso de Uso: Diploma Digital (MEC) — C#

Este projeto demonstra a integração com a **SolidSign API** para o caso de uso real do **Diploma Digital** do MEC: um XML `DocumentacaoAcademicaRegistro` assinado por **3 assinantes diferentes**, em sequência, usando certificados custodiados no **KMS SolidSign**.

Diferente dos [exemplos genéricos de assinatura XML](https://github.com/SolidTechSolutions?q=integracao-xml), que expõem um único endpoint parametrizável, este repositório expõe **um endpoint isolado por etapa real do fluxo**, já pré-configurado com os valores corretos de `signatureNodeName`, `profile` e `isRemoveXPathExclusionFilter` de cada assinante.

## Fluxo (Diploma inicial)

| Etapa | Endpoint | Assinante | Certificado | Perfil | Nó assinado |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | `POST /api/diploma/step1-representante` | IES Representantes (reitor, decano…) | e-CPF | `ADRT` | `DadosDiploma` |
| 2 | `POST /api/diploma/step2-emissora-dados` | IES Emissora | e-CNPJ | `ADRT` | `DadosDiploma` (filtro XPath removido) |
| 3 | `POST /api/diploma/step3-envelope-final` | IES Emissora | e-CNPJ | `ADRA` | `DocumentacaoAcademicaRegistro` (envelope final) |

A etapa 1 pode ser repetida uma vez por assinante representante (1..n). O XML de saída de cada etapa é a entrada da etapa seguinte. Cada endpoint recebe o XML (`document`) e o `kmsCode` do assinante daquela etapa, e retorna o XML assinado.

## Configuração (appsettings.json)

| Atributo | Descrição |
| :--- | :--- |
| `SolidSign:Api:BaseUrl` / `Authorization` | URL base e token JWT (Bearer) da SolidSign API. |
| `SolidSign:Diploma:HashAlgorithm` / `SignaturePackaging` / `CanonicalizationMethod` | Parâmetros de assinatura compartilhados pelas 3 etapas. |
| `SolidSign:Diploma:Step{1,2,3}:*` | Nó/namespace/perfil/filtro XPath específicos de cada etapa — já pré-preenchidos com os valores reais do Diploma Digital do MEC. |

## Stack
1. .NET 8 (Minimal API)

## Como Executar

1. Preencha `SolidSign:Api:Authorization` em `appsettings.json`.
2. `dotnet run`
3. Envie o XML original e o `kmsCode` do primeiro representante para `POST /api/diploma/step1-representante`.
4. Pegue o XML retornado e envie para `POST /api/diploma/step2-emissora-dados` com o `kmsCode` da IES Emissora.
5. Pegue o XML retornado e envie para `POST /api/diploma/step3-envelope-final` com o mesmo `kmsCode` da IES Emissora.

```
curl -X POST http://localhost:5095/api/diploma/step1-representante \
  -F "document=@doc-academica.xml" -F "kmsCode=$KMS_REPRESENTANTE" -o step1-signed.xml

curl -X POST http://localhost:5095/api/diploma/step2-emissora-dados \
  -F "document=@step1-signed.xml" -F "kmsCode=$KMS_IES_EMISSORA" -o step2-signed.xml

curl -X POST http://localhost:5095/api/diploma/step3-envelope-final \
  -F "document=@step2-signed.xml" -F "kmsCode=$KMS_IES_EMISSORA" -o diploma-final.xml
```

## Outras variantes do Diploma Digital

Este exemplo cobre o cenário mais completo (Diploma inicial, 3 assinaturas). O MEC também prevê: Diploma + Registro, Histórico Escolar e Diploma sem registro específico — todos seguem o mesmo padrão de `signatureNodeName`/`profile`/`isRemoveXPathExclusionFilter` por etapa. Consulte a [documentação da trilha Diploma Digital](https://solidsign.com.br/developers/diploma) para a tabela completa de cada variante.

## Outros métodos de certificação

Este exemplo usa certificado **custodiado no KMS**. Para HSM em nuvem de terceiros ou assinatura via navegador (PKCS#1), use os mesmos parâmetros de etapa nos exemplos genéricos [`exemplo-csharp-integracao-xml-cloud`](https://github.com/SolidTechSolutions/exemplo-csharp-integracao-xml-cloud) e [`exemplo-csharp-integracao-xml-pkcs1`](https://github.com/SolidTechSolutions/exemplo-csharp-integracao-xml-pkcs1).

## Tratamento de Erros
O sistema intercepta erros **400 Bad Request** e **500 Internal Server Error** e loga o JSON detalhado da SolidSign para facilitar o debug de credenciais ou parâmetros inválidos.

---

# 🇬🇧 SolidSign API - Use Case: Digital Diploma (MEC) — C#

This project demonstrates the integration with the **SolidSign API** for the real-world **Digital Diploma** use case from the Brazilian Ministry of Education (MEC): a `DocumentacaoAcademicaRegistro` XML signed by **3 different signers**, in sequence, using certificates custodied in **SolidSign's KMS**.

Unlike the [generic XML signing examples](https://github.com/SolidTechSolutions?q=integracao-xml), which expose a single parameterizable endpoint, this repository exposes **one isolated endpoint per real flow step**, already pre-configured with the correct `signatureNodeName`, `profile` and `isRemoveXPathExclusionFilter` values for each signer.

## Flow (initial Diploma)

| Step | Endpoint | Signer | Certificate | Profile | Signed node |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | `POST /api/diploma/step1-representante` | Institution representatives (rector, dean…) | e-CPF | `ADRT` | `DadosDiploma` |
| 2 | `POST /api/diploma/step2-emissora-dados` | Issuing institution | e-CNPJ | `ADRT` | `DadosDiploma` (XPath filter removed) |
| 3 | `POST /api/diploma/step3-envelope-final` | Issuing institution | e-CNPJ | `ADRA` | `DocumentacaoAcademicaRegistro` (final envelope) |

Step 1 can be repeated once per representative signer (1..n). Each step's output XML is the next step's input. Each endpoint takes the XML (`document`) and that step's signer `kmsCode`, and returns the signed XML.

## Configuration (appsettings.json)

| Attribute | Description |
| :--- | :--- |
| `SolidSign:Api:BaseUrl` / `Authorization` | SolidSign API base URL and JWT (Bearer) token. |
| `SolidSign:Diploma:HashAlgorithm` / `SignaturePackaging` / `CanonicalizationMethod` | Signature parameters shared across the 3 steps. |
| `SolidSign:Diploma:Step{1,2,3}:*` | Node/namespace/profile/XPath filter specific to each step — already pre-filled with the real MEC Digital Diploma values. |

## Stack
1. .NET 8 (Minimal API)

## How to Run

1. Fill in `SolidSign:Api:Authorization` in `appsettings.json`.
2. `dotnet run`
3. Send the original XML and the first representative's `kmsCode` to `POST /api/diploma/step1-representante`.
4. Take the returned XML and send it to `POST /api/diploma/step2-emissora-dados` with the issuing institution's `kmsCode`.
5. Take the returned XML and send it to `POST /api/diploma/step3-envelope-final` with the same issuing institution `kmsCode`.

## Other Digital Diploma variants

This example covers the most complete scenario (initial Diploma, 3 signatures). MEC also defines: Diploma + Registration, Academic Transcript, and Diploma without a specific registration — all follow the same per-step pattern. See the [Digital Diploma trail docs](https://solidsign.com.br/developers/diploma) for the full table of each variant.

## Other certification methods

This example uses a **KMS-custodied** certificate. For a third-party cloud HSM or browser-side (PKCS#1) signing, apply the same per-step parameters to the generic [`exemplo-csharp-integracao-xml-cloud`](https://github.com/SolidTechSolutions/exemplo-csharp-integracao-xml-cloud) and [`exemplo-csharp-integracao-xml-pkcs1`](https://github.com/SolidTechSolutions/exemplo-csharp-integracao-xml-pkcs1) examples.

## Error Handling
The system intercepts **400 Bad Request** and **500 Internal Server Error** responses and logs the detailed JSON from SolidSign to assist in debugging invalid credentials or parameters.

---

# 🇪🇸 SolidSign API - Caso de Uso: Diploma Digital (MEC) — C#

Este proyecto demuestra la integración con la **SolidSign API** para el caso de uso real del **Diploma Digital** del MEC: un XML `DocumentacaoAcademicaRegistro` firmado por **3 firmantes diferentes**, en secuencia, usando certificados custodiados en el **KMS de SolidSign**.

A diferencia de los [ejemplos genéricos de firma XML](https://github.com/SolidTechSolutions?q=integracao-xml), que exponen un único endpoint parametrizable, este repositorio expone **un endpoint aislado por etapa real del flujo**, ya preconfigurado con los valores correctos de `signatureNodeName`, `profile` e `isRemoveXPathExclusionFilter` de cada firmante.

## Flujo (Diploma inicial)

| Etapa | Endpoint | Firmante | Certificado | Perfil | Nodo firmado |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | `POST /api/diploma/step1-representante` | Representantes de la IES (rector, decano…) | e-CPF | `ADRT` | `DadosDiploma` |
| 2 | `POST /api/diploma/step2-emissora-dados` | IES Emisora | e-CNPJ | `ADRT` | `DadosDiploma` (filtro XPath eliminado) |
| 3 | `POST /api/diploma/step3-envelope-final` | IES Emisora | e-CNPJ | `ADRA` | `DocumentacaoAcademicaRegistro` (sobre final) |

La etapa 1 puede repetirse una vez por firmante representante (1..n). El XML de salida de cada etapa es la entrada de la siguiente. Cada endpoint recibe el XML (`document`) y el `kmsCode` del firmante de esa etapa, y devuelve el XML firmado.

## Configuración (appsettings.json)

| Atributo | Descripción |
| :--- | :--- |
| `SolidSign:Api:BaseUrl` / `Authorization` | URL base y token JWT (Bearer) de la SolidSign API. |
| `SolidSign:Diploma:HashAlgorithm` / `SignaturePackaging` / `CanonicalizationMethod` | Parámetros de firma compartidos por las 3 etapas. |
| `SolidSign:Diploma:Step{1,2,3}:*` | Nodo/namespace/perfil/filtro XPath específicos de cada etapa — ya preconfigurados con los valores reales del Diploma Digital del MEC. |

## Stack
1. .NET 8 (Minimal API)

## Cómo Ejecutar

1. Complete `SolidSign:Api:Authorization` en `appsettings.json`.
2. `dotnet run`
3. Envíe el XML original y el `kmsCode` del primer representante a `POST /api/diploma/step1-representante`.
4. Tome el XML devuelto y envíelo a `POST /api/diploma/step2-emissora-dados` con el `kmsCode` de la IES Emisora.
5. Tome el XML devuelto y envíelo a `POST /api/diploma/step3-envelope-final` con el mismo `kmsCode` de la IES Emisora.

## Otras variantes del Diploma Digital

Este ejemplo cubre el escenario más completo (Diploma inicial, 3 firmas). El MEC también define: Diploma + Registro, Historial Académico y Diploma sin registro específico — todos siguen el mismo patrón por etapa. Consulte la [documentación de la ruta Diploma Digital](https://solidsign.com.br/developers/diploma) para la tabla completa de cada variante.

## Otros métodos de certificación

Este ejemplo usa un certificado **custodiado en el KMS**. Para HSM en la nube de terceros o firma desde el navegador (PKCS#1), aplique los mismos parámetros por etapa a los ejemplos genéricos [`exemplo-csharp-integracao-xml-cloud`](https://github.com/SolidTechSolutions/exemplo-csharp-integracao-xml-cloud) y [`exemplo-csharp-integracao-xml-pkcs1`](https://github.com/SolidTechSolutions/exemplo-csharp-integracao-xml-pkcs1).

## Gestión de Errores
El sistema intercepta errores **400 Bad Request** y **500 Internal Server Error** y registra el JSON detallado de SolidSign para facilitar la depuración de credenciales o parámetros inválidos.
