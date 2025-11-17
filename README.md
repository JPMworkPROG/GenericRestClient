# GenericRestClient

Cliente REST genérico para .NET 9.0 com suporte a autenticação, rate limiting e retry automático. Desenvolvido como solução para integração com APIs REST de forma resiliente e configurável.

## 📋 Sobre o Projeto

O GenericRestClient é uma biblioteca .NET que fornece uma abstração simplificada para realizar requisições HTTP com suporte nativo a:

- **Autenticação**: Bearer Token, OAuth2 e API Key
- **Rate Limiting**: Controle de taxa de requisições por minuto
- **Retry Automático**: Política configurável de retry com backoff exponencial/linear
- **Resiliência**: Tratamento automático de falhas transitórias usando Polly

## 🚀 Funcionalidades

### Autenticação
- **Bearer Token**: Autenticação via token Bearer
- **OAuth2**: Suporte completo a OAuth2 com refresh automático de tokens
- **API Key**: Suporte a API Key via header ou query string

### Rate Limiting
- Controle de requisições por minuto
- Fila automática de requisições
- Descarte quando limite é atingido

### Retry/Backoff
- Retry automático para códigos 429 e 5xx
- Tratamento de exceções transitórias (timeout, DNS, etc.)
- Suporte a header `Retry-After`
- Backoff exponencial ou linear configurável

### Operações HTTP
- `GET`: Buscar recursos
- `POST`: Criar recursos
- `PUT`: Atualizar recursos
- `DELETE`: Remover recursos

## 🔧 Instalação

Adicione o projeto GenericRestClient à sua solução.

## 🎯 Início Rápido

### 1. Configuração no appsettings.json

```json
{
  "ApiClient": {
    "BaseUrl": "https://api.exemplo.com/",
    "Authentication": {
      "Enabled": true,
      "Type": "OAuth2",
      "ClientId": "seu-client-id",
      "ClientSecret": "seu-client-secret",
      "TokenEndpoint": "https://auth.exemplo.com/token"
    },
    "RateLimit": {
      "Enabled": true,
      "RequestsPerMinute": 60
    },
    "Retry": {
      "Enabled": true,
      "MaxRetries": 3,
      "BaseDelayMilliseconds": 500,
      "UseExponentialBackoff": true
    }
  }
}
```

### 2. Registro no Program.cs

```csharp
using GenericRestClient.Core;
using GenericRestClient.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
   .AddEnvironmentVariables();

// Registrar RestClient
var httpClientBuilder = builder.Services.ConfigureGenericRestClient(builder.Configuration);

// Configurar handlers
var options = builder.Configuration.GetSection("ApiClient")
    .Get<GenericRestClient.Configuration.ApiClientOptions>();

// Authentication Handler
if (options?.Authentication?.Enabled == true)
{
    var authType = options.Authentication.Type?.Trim().ToUpperInvariant();
    switch (authType)
    {
        case "BEARER":
            httpClientBuilder.AddBearerAuthentication();
            break;
        case "APIKEY":
            httpClientBuilder.AddApiKeyAuthentication();
            break;
        case "OAUTH2":
            httpClientBuilder.AddOAuth2Authentication();
            break;
    }
}

// RateLimit Handler
if (options?.RateLimit?.Enabled == true)
{
    httpClientBuilder.AddRateLimit();
}

// Retry Handler
if (options?.Retry?.Enabled == true)
{
    httpClientBuilder.AddRetry();
}

var host = builder.Build();
var client = host.Services.GetRequiredService<IRestClient>();
```

### 3. Uso do Cliente

```csharp
// GET
var user = await client.GetAsync<object, User>("users/123");

// POST
var newUser = new { Name = "João", Email = "joao@exemplo.com" };
var created = await client.PostAsync<object, User>("users", newUser);

// PUT
var updated = await client.PutAsync<object, User>("users/123", newUser);

// DELETE
await client.DeleteAsync("users/123");
```

## 📚 Estrutura do Projeto

```
GenericRestClient/
├── GenericRestClient/          # Biblioteca principal
│   ├── Core/                   # IRestClient e RestClient
│   ├── Configuration/          # Opções de configuração
│   ├── Handlers/               # Handlers HTTP
│   │   ├── Authentication/     # Handlers de autenticação
│   │   ├── RateLimitHandler.cs
│   │   └── RetryHandler.cs
│   ├── Extensions/              # Extensões de configuração
│   └── Authentication/         # Providers de autenticação
├── GenericRestClient.Tests/    # Testes unitários
└── documentation/              # Documentação
    ├── Desafio Técnico...pdf   # Especificação completa
    └── RequestPipelineFlow.md  # Fluxo de handlers
```

## 🔄 Fluxo de Requisições

O GenericRestClient utiliza o padrão **DelegatingHandler** do .NET para criar um pipeline de processamento:

```
Requisição → Retry → RateLimit → Authentication → HttpClient → Servidor
Resposta  ← Retry ← RateLimit ← Authentication ← HttpClient ← Servidor
```

Para mais detalhes sobre o fluxo, consulte a [documentação de fluxo de requisições](./documentation/RequestPipelineFlow.md).

## 🧪 Testes

O projeto inclui testes unitários na pasta `GenericRestClient.Tests`. Execute os testes com:

```bash
dotnet test
```

## 🛠️ Tecnologias Utilizadas

- **.NET 9.0**: Framework base
- **Polly 8.6.4**: Biblioteca de resiliência e retry
- **Microsoft.Extensions.Http**: Integração com HttpClientFactory
- **Microsoft.Extensions.Options**: Configuração baseada em opções
- **System.Text.Json**: Serialização JSON

## 📚 Referências

- [Documentação do Desafio Técnico](./documentation/Desafio%20Técnico%20–%20Desenvolvimento%20de%20Cliente%20REST%20Genérico%20em%20.NET%209.0-v2.pdf)
- [Fluxo de Requisições e Handlers](./documentation/RequestPipelineFlow.md)
- [Polly Documentation](https://www.pollydocs.org/)
- [.NET HttpClient Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient)

## 🤝 Contribuindo

Este é um projeto de desafio técnico. Para sugestões ou melhorias, consulte a documentação de especificação.

---

**Desenvolvido com .NET 9.0** 🚀
