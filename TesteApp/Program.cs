using GenericRestClient.Configuration;
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

// Configurar opções do ApiClient
builder.Services.Configure<ApiClientOptions>(
   builder.Configuration.GetSection(ApiClientOptions.SectionName));

// Registrar RestClient com seus handlers
builder.Services.AddGenericRestClient();

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
        "posts/1",
        CancellationToken.None);

    if (posts.ValueKind != JsonValueKind.Undefined)
    {
        Console.WriteLine(" Sucesso! Response recebido:");
        Console.WriteLine($"   ID: {posts.GetProperty("id")}");
        Console.WriteLine($"   Título: {posts.GetProperty("title")}");
        Console.WriteLine($"   Corpo: {posts.GetProperty("body")}");
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
