# SampleApp - Exemplos de Uso do GenericRestClient

Esta aplicação demonstra diversos casos de uso da biblioteca `GenericRestClient`.

## 📋 Estrutura do Projeto

```
SampleApp/
├── Models/              # Modelos de dados para exemplos
│   ├── User.cs
│   └── Post.cs
├── Examples/            # Exemplos organizados
│   └── RestClientExamples.cs
├── Program.cs           # Configuração e inicialização
└── appsettings.json     # Configurações da API
```

## 🚀 Exemplos Implementados

### Exemplo 1: GET com Resposta Tipada
Demonstra como fazer uma requisição GET e receber a resposta como um objeto tipado (`User`).

```csharp
var user = await client.GetAsync<User>("users/1", cancellationToken);
```

### Exemplo 2: GET com JsonElement
Demonstra como trabalhar com respostas dinâmicas usando `JsonElement`, útil quando a estrutura da resposta não é conhecida em tempo de compilação.

```csharp
var response = await client.GetAsync<JsonElement>("users/1", cancellationToken);
```

### Exemplo 3: GET para Listar Recursos
Demonstra como recuperar uma lista de recursos.

```csharp
var users = await client.GetAsync<List<User>>("users", cancellationToken);
```

### Exemplo 4: POST com Resposta Tipada
Demonstra como criar um recurso usando POST com payload e resposta tipados.

```csharp
var newPost = new CreatePostRequest(...);
var createdPost = await client.PostAsync<CreatePostRequest, Post>("posts", newPost, cancellationToken);
```

### Exemplo 5: POST com JsonElement
Demonstra como criar recursos usando objetos anônimos e `JsonElement`.

```csharp
var payload = new { userId = 1, title = "...", body = "..." };
var response = await client.PostAsync<object, JsonElement>("posts", payload, cancellationToken);
```

### Exemplo 6: PUT para Atualizar Recurso
Demonstra como atualizar um recurso existente usando PUT.

```csharp
var updateRequest = new UpdatePostRequest(...);
var updatedPost = await client.PutAsync<UpdatePostRequest, Post>("posts/1", updateRequest, cancellationToken);
```

### Exemplo 7: DELETE para Remover Recurso
Demonstra como remover um recurso usando DELETE.

```csharp
await client.DeleteAsync("posts/1", cancellationToken);
```

### Exemplo 8: Tratamento de Erros
Demonstra como tratar diferentes tipos de erros:
- Erros HTTP (404, 500, etc.)
- Timeouts e cancelamentos
- Erros de rede

### Exemplo 9: Trabalhando com Tipos Anônimos
Demonstra como usar tipos anônimos para criar payloads dinâmicos.

### Exemplo 10: Uso de CancellationToken
Demonstra como usar `CancellationToken` para cancelar requisições.

## 🔧 Configuração

A aplicação utiliza o arquivo `appsettings.json` para configurar:
- **BaseUrl**: URL base da API
- **Authentication**: Configuração de autenticação (Bearer, OAuth2, API Key)
- **RateLimit**: Limite de requisições por minuto
- **Retry**: Política de retry automático

## 🎯 Executando os Exemplos

Para executar os exemplos:

```bash
cd SampleApp
dotnet run
```

Os exemplos serão executados sequencialmente, demonstrando diferentes aspectos do uso da biblioteca.

## 📝 Notas

- Os exemplos utilizam a API configurada em `appsettings.json`
- Alguns exemplos podem falhar dependendo da API configurada (404, timeout, etc.)
- Os exemplos de tratamento de erros são intencionais e demonstram como a biblioteca lida com falhas

