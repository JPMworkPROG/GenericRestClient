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

// Registrar GenericRestClient com todos os handlers configurados
builder.Services.AddGenericRestClient(builder.Configuration);

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

    // ======================== TESTE 2: Buscar Usuário ========================
    Console.WriteLine("TESTE 2: Buscando dados de usuário");
    Console.WriteLine("─────────────────────────────────────────────\n");

    var user = await client.GetAsync<object, JsonElement>(
        "users/1",
        CancellationToken.None);

    if (user.ValueKind != JsonValueKind.Undefined)
    {
        Console.WriteLine("Sucesso! Dados do usuário:");
        Console.WriteLine($"   ID: {user.GetProperty("id")}");
        Console.WriteLine($"   Nome: {user.GetProperty("name")}");
        Console.WriteLine($"   Email: {user.GetProperty("email")}");
        Console.WriteLine($"   Website: {user.GetProperty("website")}");
        Console.WriteLine();
    }

    // ======================== TESTE 3: Buscar Lista de Posts ========================
    Console.WriteLine("TESTE 3: Buscando lista de posts de um usuário");
    Console.WriteLine("─────────────────────────────────────────────────────\n");

    var userPosts = await client.GetAsync<object, JsonElement>(
        "posts?userId=1&_limit=3",
        CancellationToken.None);

    if (userPosts.ValueKind == JsonValueKind.Array)
    {
        Console.WriteLine($"Sucesso! Encontrados {userPosts.GetArrayLength()} posts:");

        int count = 1;
        foreach (var post in userPosts.EnumerateArray())
        {
            Console.WriteLine($"   {count}. {post.GetProperty("title")}");
            count++;
        }
        Console.WriteLine();
    }

    // ======================== TESTE 4: Buscar Comentários ========================
    Console.WriteLine("TESTE 4: Buscando comentários de um post");
    Console.WriteLine("───────────────────────────────────────────\n");

    var comments = await client.GetAsync<object, JsonElement>(
        "posts/1/comments?_limit=2",
        CancellationToken.None);

    if (comments.ValueKind == JsonValueKind.Array)
    {
        Console.WriteLine($"Sucesso! Encontrados {comments.GetArrayLength()} comentários:");

        int count = 1;
        foreach (var comment in comments.EnumerateArray())
        {
            Console.WriteLine($"   {count}. {comment.GetProperty("name")}");
            Console.WriteLine($"      Email: {comment.GetProperty("email")}");
            count++;
        }
        Console.WriteLine();
    }

    // ======================== TESTE 5: Tratamento de Erro (404) ========================
    Console.WriteLine("TESTE 5: Testando tratamento de erro (404)");
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

    // ======================== RESUMO FINAL ========================
    Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                    TESTES CONCLUÍDOS COM SUCESSO            ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
}
catch (Exception ex)
{
    Console.WriteLine($"Erro durante os testes: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}