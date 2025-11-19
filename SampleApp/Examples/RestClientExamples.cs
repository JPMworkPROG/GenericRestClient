using GenericRestClient.Core;
using Microsoft.Extensions.Logging;
using SampleApp.Models;
using System.Text.Json;

namespace SampleApp.Examples;

public class RestClientExamples
{
    private readonly IRestClient _client;
    private readonly ILogger<RestClientExamples> _logger;

    public RestClientExamples(IRestClient client, ILogger<RestClientExamples> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task RunAllExamplesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════════════════");
        _logger.LogInformation("          EXEMPLOS DE USO - GenericRestClient");
        _logger.LogInformation("═══════════════════════════════════════════════════════════════\n");

        try
        {
            await Example1_GetWithTypedResponseAsync(cancellationToken);
            await Example2_GetWithJsonElementAsync(cancellationToken);
            await Example3_GetListAsync(cancellationToken);
            await Example4_PostWithTypedResponseAsync(cancellationToken);
            await Example5_PostWithJsonElementAsync(cancellationToken);
            await Example6_PutUpdateResourceAsync(cancellationToken);
            await Example7_DeleteResourceAsync(cancellationToken);
            await Example8_ErrorHandlingAsync(cancellationToken);
            await Example9_WorkingWithAnonymousTypesAsync(cancellationToken);
            await Example10_CancellationTokenUsageAsync(cancellationToken);

            _logger.LogInformation("\n═══════════════════════════════════════════════════════════════");
            _logger.LogInformation("      TODOS OS EXEMPLOS FORAM EXECUTADOS COM SUCESSO");
            _logger.LogInformation("═══════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante execução dos exemplos: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    // Exemplo 1: GET com resposta tipada
    public async Task Example1_GetWithTypedResponseAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 1] GET com Resposta Tipada");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        try
        {
            var user = await _client.GetAsync< JsonElement>(
                "user",
                cancellationToken);

            if (user.ValueKind != JsonValueKind.Undefined)
            {
                Console.WriteLine($"✓ Informações do usuário recuperadas com sucesso:");
                if (user.TryGetProperty("login", out var login))
                    Console.WriteLine($"  Login: {login.GetString()}");
                if (user.TryGetProperty("name", out var name))
                    Console.WriteLine($"  Nome: {name.GetString()}");
                if (user.TryGetProperty("public_repos", out var repos))
                    Console.WriteLine($"  Repositórios públicos: {repos.GetInt32()}");
            }
            else
            {
                Console.WriteLine("⚠ Nenhuma informação encontrada");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Erro ao buscar usuário: {ex.Message}");
        }
    }

    // Exemplo 2: GET com JsonElement para respostas dinâmicas
    public async Task Example2_GetWithJsonElementAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 2] GET com JsonElement (Resposta Dinâmica)");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        try
        {
            var response = await _client.GetAsync< JsonElement>(
                "user",
                cancellationToken);

            if (response.ValueKind != JsonValueKind.Undefined)
            {
                Console.WriteLine("✓ Resposta JSON recebida:");
                Console.WriteLine($"  {response.GetRawText()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Erro ao buscar dados: {ex.Message}");
        }
    }

    // Exemplo 3: GET para listar recursos
    public async Task Example3_GetListAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 3] GET para Listar Recursos");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        try
        {
            var repos = await _client.GetAsync< JsonElement>(
                "user/repos",
                cancellationToken);

            if (repos.ValueKind == JsonValueKind.Array)
            {
                var count = repos.GetArrayLength();
                Console.WriteLine($"✓ {count} repositórios recuperados:");
                foreach (var repo in repos.EnumerateArray().Take(5))
                {
                    if (repo.TryGetProperty("name", out var name))
                        Console.WriteLine($"  • {name.GetString()}");
                }
                if (count > 5)
                {
                    Console.WriteLine($"  ... e mais {count - 5} repositórios");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Erro ao buscar repositórios: {ex.Message}");
        }
    }

    // Exemplo 4: POST com resposta tipada
    public async Task Example4_PostWithTypedResponseAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 4] POST com Resposta Tipada");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        try
        {
            var payload = new
            {
                name = "test-repo-from-generic-rest-client",
                description = "Repositório criado via GenericRestClient",
                @private = false,
                auto_init = true
            };

            var createdRepo = await _client.PostAsync<object, JsonElement>(
                "user/repos",
                payload,
                cancellationToken);

            if (createdRepo.ValueKind == JsonValueKind.Object)
            {
                Console.WriteLine("✓ Repositório criado com sucesso:");
                if (createdRepo.TryGetProperty("full_name", out var fullName))
                    Console.WriteLine($"  Nome completo: {fullName.GetString()}");
                if (createdRepo.TryGetProperty("html_url", out var url))
                    Console.WriteLine($"  URL: {url.GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Erro ao criar repositório: {ex.Message}");
        }
    }

    // Exemplo 5: POST com JsonElement
    public async Task Example5_PostWithJsonElementAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 5] POST com JsonElement");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        try
        {
            var payload = new
            {
                title = "Issue de exemplo via GenericRestClient",
                body = "Esta é uma issue criada usando GenericRestClient para demonstrar POST com JsonElement"
            };

            var response = await _client.PostAsync<object, JsonElement>(
                "repos/microsoft/dotnet/issues",
                payload,
                cancellationToken);

            if (response.ValueKind == JsonValueKind.Object)
            {
                Console.WriteLine("✓ Issue criada:");
                if (response.TryGetProperty("number", out var number))
                    Console.WriteLine($"  Issue #{number.GetInt32()}");
                if (response.TryGetProperty("html_url", out var url))
                    Console.WriteLine($"  URL: {url.GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Erro ao criar issue (esperado se não tiver permissões): {ex.Message}");
        }
    }

    // Exemplo 6: PUT para atualizar recurso
    public async Task Example6_PutUpdateResourceAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 6] PUT para Atualizar Recurso");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        try
        {
            var updateRequest = new
            {
                name = "test-repo-from-generic-rest-client",
                description = "Descrição atualizada via PUT"
            };

            var updatedRepo = await _client.PutAsync<object, JsonElement>(
                "repos/{seu-usuario}/test-repo-from-generic-rest-client",
                updateRequest,
                cancellationToken);

            if (updatedRepo.ValueKind == JsonValueKind.Object)
            {
                Console.WriteLine("✓ Repositório atualizado com sucesso:");
                if (updatedRepo.TryGetProperty("description", out var description))
                    Console.WriteLine($"  Nova descrição: {description.GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Erro ao atualizar repositório (esperado se o repositório não existir): {ex.Message}");
        }
    }

    // Exemplo 7: DELETE para remover recurso
    public async Task Example7_DeleteResourceAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 7] DELETE para Remover Recurso");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        try
        {
            await _client.DeleteAsync(
                "repos/{seu-usuario}/test-repo-from-generic-rest-client",
                cancellationToken);

            Console.WriteLine("✓ Repositório deletado com sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Erro ao deletar repositório (esperado se o repositório não existir): {ex.Message}");
        }
    }

    // Exemplo 8: Tratamento de erros
    public async Task Example8_ErrorHandlingAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 8] Tratamento de Erros");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        // Erro 404 - Recurso não encontrado
        try
        {
            await _client.GetAsync< JsonElement>(
                "repos/usuario-inexistente/repositorio-inexistente",
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("✓ Erro 404 capturado corretamente:");
            Console.WriteLine($"  Mensagem: {ex.Message}");
        }

        // Erro de rede/timeout
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await _client.GetAsync< JsonElement>(
                "user",
                cts.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("✓ Timeout/Cancelamento capturado corretamente");
        }
    }

    // Exemplo 9: Trabalhando com tipos anônimos
    public async Task Example9_WorkingWithAnonymousTypesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 9] Trabalhando com Tipos Anônimos");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        try
        {
            var payload = new
            {
                name = "test-repo-anonymous-types",
                description = "Repositório criado usando tipos anônimos",
                @private = false
            };

            var response = await _client.PostAsync<object, JsonElement>(
                "user/repos",
                payload,
                cancellationToken);

            if (response.ValueKind == JsonValueKind.Object)
            {
                Console.WriteLine("✓ Repositório criado usando tipo anônimo:");
                if (response.TryGetProperty("name", out var name))
                    Console.WriteLine($"  Nome: {name.GetString()}");
                if (response.TryGetProperty("html_url", out var url))
                    Console.WriteLine($"  URL: {url.GetString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Erro (esperado se já existir um repositório com esse nome): {ex.Message}");
        }
    }

    // Exemplo 10: Uso de CancellationToken
    public async Task Example10_CancellationTokenUsageAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Exemplo 10] Uso de CancellationToken");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        try
        {
            using var cts = new CancellationTokenSource();
            
            var task = _client.GetAsync< JsonElement>(
                "user",
                cts.Token);

            await Task.Delay(100, cancellationToken);
            cts.Cancel();

            await task;
            Console.WriteLine("✓ Requisição completada");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("✓ Operação cancelada corretamente usando CancellationToken");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Erro: {ex.Message}");
        }
    }
}

