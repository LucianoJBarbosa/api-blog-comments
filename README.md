## API Blog Comments

Uma API em ASP.NET Core para gerenciar posts de blog e seus comentários.
O foco é demonstrar boas práticas de validação com **DataAnnotations**, uso de DTOs,
documentação com OpenAPI e preparação para um cenário futuro com banco de dados real.

### Tecnologias

- ASP.NET Core Web API (`net10.0`)
- Entity Framework Core (banco em memória neste projeto)
- OpenAPI (arquivo de especificação `OpenAPI.yaml`)
- Autenticação e autorização com JWT

### Endpoints principais

- `GET /api/posts` – lista posts com quantidade de comentários.
- `POST /api/posts` – cria um novo post (requere autenticação).
- `GET /api/posts/{id}` – retorna um post específico com seus comentários.
- `POST /api/posts/{id}/comments` – adiciona um comentário a um post (requere autenticação).
- `POST /api/auth/login` – autentica um usuário e retorna um token JWT.

### Validação e DataAnnotations

As entidades e DTOs utilizam **DataAnnotations** para validar os dados de entrada e modelar as regras de domínio:

- `BlogPost` e `Comment` possuem atributos como `[Required]` e `[StringLength]` para garantir títulos, conteúdos e textos de comentários válidos.
- Os DTOs (`CreateBlogPostDto`, `CreateCommentDto`) também usam DataAnnotations, fazendo com que requisições inválidas retornem automaticamente `400 Bad Request` com detalhes dos erros.

Isso garante que, mesmo usando um banco em memória, a modelagem e a validação estejam prontas para serem reaproveitadas quando for configurado um provedor de dados real.

### Persistência e preparo para banco real

Atualmente a API utiliza um **banco em memória** configurado no `Program.cs`, adequado para cenários de desenvolvimento e demonstração.

Quando você quiser evoluir para um banco persistente (por exemplo **Azure SQL** ou **SQL Server**), o fluxo típico será:

1. Adicionar o pacote do provedor EF Core correspondente (`Microsoft.EntityFrameworkCore.SqlServer`, por exemplo).
2. Alterar a configuração do `DbContext` para usar a connection string adequada.
3. Criar e aplicar migrations com `dotnet ef migrations add` / `dotnet ef database update`.
4. Ajustar a connection string via `appsettings.json` ou variáveis de ambiente (ideal para Azure).

O desenho das entidades, relacionamentos e DataAnnotations já está pronto para essa migração futura.

### Autenticação e autorização (JWT)

A API expõe um fluxo simples de autenticação baseado em **JWT (JSON Web Token)**:

- `POST /api/auth/login` recebe um `username` e `password` e, quando válidos, retorna um token JWT.
- O token deve ser enviado nas chamadas autenticadas usando o header `Authorization: Bearer {token}`.
- Endpoints de leitura (`GET /api/posts`, `GET /api/posts/{id}`) são públicos.
- Endpoints de escrita (`POST /api/posts`, `POST /api/posts/{id}/comments`) exigem um token JWT válido.

No ambiente atual de desenvolvimento, existe um usuário fixo de exemplo:

- **Usuário:** `admin`
- **Senha:** `admin123`

Nota: As credenciais e a chave JWT estão expostas para fins de demonstração da demo. Em produção, seriam utilizados User Secrets ou Azure Key Vault.

Para obter um token:

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'
```

Depois, utilize o valor do campo `token` retornado no header:

```http
Authorization: Bearer {token}
```

As configurações de emissão e validação do token (Issuer, Audience, Key, ExpirationMinutes) ficam em `appsettings*.json` na seção `Jwt`.
Em um ambiente real, a chave (`Key`) deve ser protegida por mecanismos seguros (variáveis de ambiente, Azure Key Vault, etc.).

### CORS

A API já está configurada com **CORS** para permitir o consumo a partir de um front-end (exemplo: SPA em `localhost:4200`).
Os domínios permitidos podem ser facilmente ajustados no `Program.cs` conforme o ambiente e o cliente que irá consumir a API.

### Execução local

Pré-requisitos:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

Comandos:

```bash
cd api-blog-comments-dev
dotnet run
```

Por padrão, a aplicação sobe em uma porta HTTPS configurada pelo ASP.NET (por exemplo `https://localhost:5001`).

### Execução em container Docker

O repositório inclui um `Dockerfile` com build multi-stage (SDK + runtime ASP.NET).
Para construir e executar a API em um container:

```bash
# na raiz do projeto
docker build -t api-blog-comments:latest .

docker run -d \
  -p 5000:80 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  --name api-blog-comments \
  api-blog-comments:latest
```

Após subir o container:

- API: `http://localhost:5000/api/posts`
- Login (JWT): `http://localhost:5000/api/auth/login`

### Testes automatizados

O projeto inclui testes automatizados com **xUnit**:

- Projeto de testes: `api-blog-comments-dev.Tests`
- Exemplos de testes cobrindo geração de token JWT e comportamento básico do `PostsController`.

Para executar os testes:

```bash
dotnet test
```

### Documentação (OpenAPI)

A especificação OpenAPI da API está disponível no arquivo `OpenAPI.yaml` na raiz do projeto.
Ela pode ser importada em ferramentas como Postman, Insomnia, etc
para geração de clientes ou visualização interativa.

TODO: Implementação de Swagger UI para melhor visualização e implementação do CRUD completo com POST, GET, PUT e DELETE de comentários e posts do blog.
---

## API Blog Comments (English)

An ASP.NET Core Web API for managing blog posts and their comments.
The focus is to showcase good practices with **DataAnnotations**, DTOs,
OpenAPI documentation and a codebase that is ready to be upgraded
to a real database provider in the future.

### Stack

- ASP.NET Core Web API (`net10.0`)
- Entity Framework Core (in‑memory database in this project)
- OpenAPI (spec file `OpenAPI.yaml`)
- Authentication and authorization with JWT

### Main endpoints

- `GET /api/posts` – list posts with the number of comments.
- `POST /api/posts` – create a new post (**requires authentication**).
- `GET /api/posts/{id}` – get a specific post with its comments.
- `POST /api/posts/{id}/comments` – add a comment to a post (**requires authentication**).
- `POST /api/auth/login` – authenticate a user and return a JWT token.

### Validation and DataAnnotations

Entities and DTOs use **DataAnnotations** to validate input and encode business rules:

- `BlogPost` and `Comment` use attributes like `[Required]` and `[StringLength]` to guarantee valid titles, content and comment text.
- DTOs (`CreateBlogPostDto`, `CreateCommentDto`) also use DataAnnotations, so invalid requests automatically return `400 Bad Request` with detailed errors.

This means that, even with an in‑memory database, the domain model and validation are ready
to be reused when switching to a real data provider.

### Persistence and readiness for a real database

Currently the API uses an **in‑memory database** configured in `Program.cs`, which is fine for demos and development.

To move to a persistent database (for example **Azure SQL** or **SQL Server**), a typical flow would be:

1. Add the corresponding EF Core provider package (e.g. `Microsoft.EntityFrameworkCore.SqlServer`).
2. Change the `DbContext` configuration to use an appropriate connection string.
3. Create and apply migrations with `dotnet ef migrations add` / `dotnet ef database update`.
4. Manage the connection string via `appsettings.json` or environment variables (ideal for Azure).

The current entities, relationships and DataAnnotations were designed with this migration in mind.

### Authentication and authorization (JWT)

The API exposes a simple **JWT (JSON Web Token)** authentication flow:

- `POST /api/auth/login` accepts `username` and `password` and, when valid, returns a JWT token.
- The token must be sent on authenticated requests using the `Authorization: Bearer {token}` header.
- Read endpoints (`GET /api/posts`, `GET /api/posts/{id}`) are public.
- Write endpoints (`POST /api/posts`, `POST /api/posts/{id}/comments`) require a valid JWT token.

In the current development setup there is a fixed sample user:

- **Username:** `admin`
- **Password:** `admin123`


Note: The credentials and JWT key are exposed to better demonstrate the use case of authorization and authentication. In production, it's better to use User Secrets or Azure Key Vault. 

To obtain a token:

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'
```

Then use the `token` value in:

```http
Authorization: Bearer {token}
```

Token emission and validation settings (Issuer, Audience, Key, ExpirationMinutes) live in
`appsettings*.json` under the `Jwt` section. In a real environment the secret key must be
stored securely (environment variables, Azure Key Vault, etc.).

### CORS

The API is configured with **CORS** so a front‑end app (for example a SPA on `localhost:4200`)
can consume it. Allowed origins can be adjusted in `Program.cs` depending on the client and environment.

### Running locally

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

Commands:

```bash
cd api-blog-comments-dev
dotnet run
```

By default the application runs on an HTTPS port provided by ASP.NET (for example `https://localhost:5001`).

### Running in a Docker container

The repository includes a multi-stage `Dockerfile` (SDK + ASP.NET runtime).
To build and run the API inside a container:

```bash
# from the project root
docker build -t api-blog-comments:latest .

docker run -d \
  -p 5000:80 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  --name api-blog-comments \
  api-blog-comments:latest
```

Once the container is running:

- API: `http://localhost:5000/api/posts`
- Login (JWT): `http://localhost:5000/api/auth/login`

### Automated tests

The solution includes automated tests using **xUnit**:

- Test project: `api-blog-comments-dev.Tests`
- Sample tests covering JWT token generation and basic `PostsController` behavior.

To run the tests:

```bash
dotnet test
```

### Documentation (OpenAPI)

The OpenAPI specification for this API is available as `OpenAPI.yaml` at
the project root. You can import this file into tools such as Postman,
Insomnia, etc

TODO: Implementation of Swagger UI for better visualization and complete CRUD with POST, GET, PUT and DELETE of the blog posts and comments...