using GenericRestClient.Core;
using GenericRestClient.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
   .AddEnvironmentVariables();

// Registrar RestClient com configuração flexível
var httpClientBuilder = builder.Services.ConfigureGenericRestClient(builder.Configuration);

// Configurar handlers baseado nas opções (exemplo de flexibilidade)
var options = builder.Configuration.GetSection("ApiClient").Get<GenericRestClient.Configuration.ApiClientOptions>();
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

if (options?.RateLimit?.Enabled == true)
{
   httpClientBuilder.AddRateLimit();
}

if (options?.Retry?.Enabled == true)
{
   httpClientBuilder.AddRetry();
}

var host = builder.Build();

var client = host.Services.GetRequiredService<IRestClient>();

// ==================== TESTE REAL DE GET ====================

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         TESTE DE GET - GenericRestClient                      ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

try
{
    // ======================== TESTE 1: Buscar Posts ========================
    Console.WriteLine(" TESTE 1: Buscando posts do JSONPlaceholder");
    Console.WriteLine("─────────────────────────────────────────────\n");

    var posts = await client.GetAsync<object, JsonElement>(
        "connect/userinfo",
        CancellationToken.None);

    if (posts.ValueKind != JsonValueKind.Undefined)
    {
        Console.WriteLine($" {posts}");
        Console.WriteLine();
    }

    // ======================== TESTE 1: Buscar Posts ========================
    Console.WriteLine(" TESTE 2: Buscando posts do JSONPlaceholder");
    Console.WriteLine("─────────────────────────────────────────────\n");

    var posts2 = await client.GetAsync<object, JsonElement>(
        "connect/userinfo",
        CancellationToken.None);

    if (posts.ValueKind != JsonValueKind.Undefined)
    {
        Console.WriteLine($" {posts2}");
        Console.WriteLine();
    }

    await Task.Delay(TimeSpan.FromMinutes(2));

    // ======================== TESTE 1: Buscar Posts ========================
    Console.WriteLine(" TESTE 3: Buscando posts do JSONPlaceholder");
    Console.WriteLine("─────────────────────────────────────────────\n");

    var posts3 = await client.GetAsync<object, JsonElement>(
        "connect/userinfo",
        CancellationToken.None);

    if (posts.ValueKind != JsonValueKind.Undefined)
    {
        Console.WriteLine($" {posts3}");
        Console.WriteLine();
    }

    Console.WriteLine("TESTE 2: Testando tratamento de erro (404)");
    Console.WriteLine("──────────────────────────────────────────────\n");

    try
    {
        var notFound = await client.GetAsync<object, JsonElement>(
            "posts/99999",
            CancellationToken.None);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($" Erro esperado capturado: {ex.Message}");
        Console.WriteLine();
    }

    Console.WriteLine("TESTE 3: Criando novo post");
    Console.WriteLine("─────────────────────────────────────────────\n");

    var novoPost = new
    {
        title = "Title Field Test",
        body = "Validando POST com GenericRestClient.",
        userId = 1
    };

    var postCriado = await client.PostAsync<object, JsonElement>(
        "posts",
        novoPost,
        CancellationToken.None);

    if (postCriado.ValueKind != JsonValueKind.Undefined)
    {
        Console.WriteLine("Sucesso! Post criado (simulação JSONPlaceholder):");
        Console.WriteLine($"   ID: {postCriado.GetProperty("id")}");
        Console.WriteLine($"   Título: {postCriado.GetProperty("title")}");
        Console.WriteLine($"   Corpo: {postCriado.GetProperty("body")}");
        Console.WriteLine();
    }

    Console.WriteLine("TESTE 4: Atualizando post existente");
    Console.WriteLine("─────────────────────────────────────────────\n");

    var postAtualizado = new
    {
        id = 1,
        title = "Title Field Test - Atualizado",
        body = "Validando PUT com GenericRestClient.",
        userId = 1
    };

    var resultadoPut = await client.PutAsync<object, JsonElement>(
        "posts/1",
        postAtualizado,
        CancellationToken.None);

    if (resultadoPut.ValueKind != JsonValueKind.Undefined)
    {
        Console.WriteLine("Sucesso! Post atualizado (simulação JSONPlaceholder):");
        Console.WriteLine($"   ID: {resultadoPut.GetProperty("id")}");
        Console.WriteLine($"   Título: {resultadoPut.GetProperty("title")}");
        Console.WriteLine($"   Corpo: {resultadoPut.GetProperty("body")}");
        Console.WriteLine();
    }

    Console.WriteLine("TESTE 5: Deletando post");
    Console.WriteLine("─────────────────────────────────────────────\n");

    await client.DeleteAsync(
        "posts/1",
        CancellationToken.None);

    Console.WriteLine("Sucesso! DELETE retornou status esperado (sem conteúdo).");
    Console.WriteLine();

    Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║      TESTES DE GET/POST/PUT/DELETE CONCLUÍDOS COM SUCESSO         ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
}
catch (Exception ex)
{
    Console.WriteLine($"Erro durante os testes de escrita: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}
