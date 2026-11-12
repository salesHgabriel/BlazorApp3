# Nexus Admin: aplicação whitelabel em Blazor

Blazor Web App (.NET 11), Interactive Server, EF Core + SQLite e ASP.NET Core Identity.

![Dashboard com o branding do time "Data & Analytics"](docs/screenshots/02-dashboard.png)

## Funcionalidades

> Prints capturados com os dados do seed, logado como `admin@demo.com`. Ficam em [`docs/screenshots/`](docs/screenshots).

### Login e contas de demonstração
Login em SSR estático com o whitelabel global. Ao clicar numa conta demo, os campos são preenchidos.

![Login](docs/screenshots/01-login.png)

### Usuários: listagem, busca, ordenação e paginação
Tabela paginada no servidor, com busca com debounce, colunas ordenáveis e skeleton durante o carregamento.
Papéis e times longos são resumidos em **"… +N"**; ao passar o mouse, aparece a lista completa.

![Usuários](docs/screenshots/03-usuarios.png)

### Multi filtros
Papéis, times e departamentos (multi-seleção), status e período de cadastro.
Cada filtro ativo vira um chip removível. A URL aceita `?role=` e `?team=`.

![Multi filtros](docs/screenshots/04-usuarios-multifiltros.png)

### Dialog lateral (criar/editar)
Editor em drawer, com upload de avatar, validação (DataAnnotations no cliente **e** no servidor via pipeline do CQRS), papéis e times.

![Editor lateral](docs/screenshots/05-editor-usuario-lateral.png)

### Dialog central (detalhes)
![Detalhes do usuário](docs/screenshots/06-detalhes-usuario-central.png)

### Confirmação de exclusão
Mostra o impacto da exclusão. Exclusões críticas, como a de um time, exigem digitar o nome.

| Simples | Crítica (digitar o nome) |
|---|---|
| ![Confirmação](docs/screenshots/07-confirmacao-exclusao.png) | ![Exclusão crítica](docs/screenshots/21-exclusao-critica.png) |

### Ações em lote
Selecione vários usuários para ativar, inativar ou excluir. A exclusão em lote pede confirmação digitada.

![Ações em lote](docs/screenshots/08-acoes-em-lote.png)

### Papéis (roles dinâmicas) e permissões (fixas)
Os papéis são criados pela tela. As permissões têm **Id e nome fixos no código** (`Perm`) e são sincronizadas no seed.
O editor mostra a matriz de permissões agrupada, com "marcar o grupo".

| Papéis | Matriz de permissões |
|---|---|
| ![Papéis](docs/screenshots/09-papeis.png) | ![Editor de papel](docs/screenshots/10-editor-papel-permissoes.png) |

Catálogo das permissões, com os papéis que concedem cada uma:

![Permissões](docs/screenshots/11-permissoes.png)

### Times personalizados
Cards com a identidade visual de cada time. O editor define cores, logo e o **branding próprio** do time: nome exibido e cor do menu.

| Times | Editor do time |
|---|---|
| ![Times](docs/screenshots/12-times.png) | ![Editor do time](docs/screenshots/13-editor-time-branding.png) |

### Membros do time e busca lazy
A busca de usuários para adicionar é paginada sob demanda ("carregar mais"), e o papel no time pode ser alterado na própria lista.

![Detalhe do time](docs/screenshots/14-detalhe-time-busca-lazy.png)

### Troca de time e branding aplicado
No menu **Meus times**, o time atual aparece no topo e com ✓. Ao escolher um time com branding próprio, a aplicação inteira assume o nome e as cores dele.

| Menu "Meus times" | Branding do time aplicado |
|---|---|
| ![Meus times](docs/screenshots/17-menu-meus-times.png) | ![Branding do time](docs/screenshots/18-branding-do-time-aplicado.png) |

### Menu do usuário: permissões efetivas
As permissões efetivas são resumidas com **"… +N"**; ao passar o mouse, aparecem todas.

![Menu do usuário](docs/screenshots/16-menu-usuario-permissoes.png)

### Whitelabel
Nome, slogan, logo, favicon, cores (com temas prontos), fonte, arredondamento, efeito de transição de página e textos do login.
A pré-visualização atualiza enquanto você edita.

![Whitelabel](docs/screenshots/15-whitelabel.png)

### Componentes: toasts, loadings, dialogs, upload, busca lazy, paginação e efeitos
A página **Componentes** (`/components`) reúne todos os componentes reutilizáveis para teste.

| Toasts | Loading global |
|---|---|
| ![Toasts](docs/screenshots/19-componentes-toasts.png) | ![Loading](docs/screenshots/20-loading-global.png) |

### Idiomas (português, inglês e espanhol)
O seletor no topo (e no canto da tela de login) troca o idioma de toda a interface, incluindo mensagens de validação, erros de negócio, erros do Identity, toasts e o formato de datas e números.
A escolha fica gravada em cookie.

| Inglês | Espanhol |
|---|---|
| ![Inglês](docs/screenshots/22-idioma-ingles.png) | ![Espanhol](docs/screenshots/23-idioma-espanhol.png) |

#### Valores monetários por idioma
Em **/components**, o card *Valores e moedas por idioma* tem um pedido de exemplo com preço, quantidade e desconto:
- **Moeda padrão por idioma:** real (R$) em português, dólar ($) em inglês e euro (€) em espanhol. O seletor permite trocar a moeda.
- **Entrada:** o componente `<MoneyInput @bind-Value="..." Currency="BRL" />` aceita o valor digitado no formato do idioma (`2.500,75` ou `2,500.75`) e o reformata ao sair do campo. Valores inválidos são rejeitados.
- **Comparação:** uma tabela mostra o mesmo total em cada idioma, com o formato de número e de data correspondente.
- **Helper:** `Localization/Money.cs` (`Format`, `TryParse`, `Symbol`) pode ser usado em qualquer lugar.

Só a formatação muda, e não há conversão de câmbio.

![Valores por idioma](docs/screenshots/24-valores-por-idioma.png)

## Como rodar

```bash
cd BlazorApp3
dotnet run --launch-profile https
```

Na inicialização as migrations pendentes são aplicadas (`Database.MigrateAsync()`) e, se o banco estiver vazio, o seed é executado. Para recriar do zero, apague `app.db` e rode novamente.

### Migrations

O `dotnet-ef` 11 está registrado como ferramenta local em `dotnet-tools.json` (rode `dotnet tool restore` uma vez). As migrations ficam em `BlazorApp3/Data/Migrations`.

```bash
cd BlazorApp3
dotnet ef migrations add NomeDaMudanca -o Data/Migrations   # após alterar entidades/AppDbContext
dotnet ef database update                                    # opcional: a app já aplica ao iniciar
dotnet ef migrations remove                                  # desfaz a última (se ainda não aplicada)
```

### Contas de demonstração (também listadas na tela de login, clique para preencher)

| E-mail | Senha | Papel |
|---|---|---|
| admin@demo.com | Admin@123 | Administrador (sistema, todas as permissões) |
| gestor@demo.com | Gestor@123 | Gestor |
| analista@demo.com | Analista@123 | Analista (somente leitura) |
| suporte@demo.com | Suporte@123 | Suporte |
| designer@demo.com | Designer@123 | Designer (whitelabel + componentes) |
| viewer@demo.com | Viewer@123 | Visualizador (apenas dashboard) |
| inativo@demo.com | Inativo@123 | Usuário inativo (login bloqueado) |

Os 180 usuários gerados pelo seed usam a senha `Demo@123`.

## Arquitetura

Cada arquivo `.cs` contém **um único tipo** (classe, record, enum ou interface), organizado por pasta:

| Pasta | Conteúdo |
|---|---|
| `Data/Entities/` | Uma entidade por arquivo (`AppUser`, `AppRole`, `Team`, `TeamMember`, `BrandingSettings`...) |
| `Data/` | `AppDbContext` (Identity), `DbSeeder`, `Migrations/` |
| `Security/` | `Perm` (catálogo fixo) e `PermissionDefinition` |
| `Security/Authorization/` | `HasPermissionAttribute`, `PermissionRequirement`, `PermissionPolicyProvider`, `PermissionHandler`, `PermissionService` (cache) |
| `Security/Identity/` | Revalidação do circuito, `AppClaimsPrincipalFactory`, `LocalizedIdentityErrorDescriber` |
| `Application/Cqrs/` | CQRS próprio (sem pacotes, só DI): `Abstractions/`, `Behaviors/`, `Dispatching/` |
| `Features/<Feature>/` | Fatias verticais: `Models/`, `Shared/`, `Queries/<Caso>/` e `Commands/<Caso>/` |
| `Services/` | `Common/` (`AppException`, `PagedResult`...), `Ui/` (`UiDispatcher`, `PagedQueryState`), `Toasts/`, `Loading/`, `Confirm/`, `Branding/`, `Files/` |
| `Localization/` | `SharedResource`, `AppCultures`, `AppText`, `ValidationMessages` |
| `Resources/` | `SharedResource.resx` (pt-BR), `.en.resx`, `.es.resx` |
| `Components/UI/` | Componentes reutilizáveis (incl. `EditorBase<T>` e `PagedPager`) |
| `Components/Pages/` | Dashboard, Usuários, Papéis, Permissões, Times, Whitelabel, Vitrine |
| `Components/Account/` | Login/Logout (SSR estático) |

Exemplo de caso de uso:

```
Features/Users/
├── Models/UserEditModel.cs, UserListItem.cs, ...
├── Shared/UserAssignments.cs
├── Queries/SearchUsers/SearchUsersQuery.cs + SearchUsersHandler.cs
└── Commands/CreateUser/CreateUserCommand.cs + CreateUserHandler.cs
```

### CQRS (commands e queries só com DI)

- **Mensagens**: `IQuery<TResult>`, `ICommand<TResult>` e `ICommand` (que retorna `Unit`). Listagens herdam de `PagedQuery<TItem>` (Page/PageSize).
- **Handlers**: `IQueryHandler<,>` / `ICommandHandler<,>`, um handler por mensagem, no arquivo ao lado dela (ex.: `CreateUserCommand.cs` + `CreateUserHandler.cs`).
  `AddCqrs(assembly)` varre o assembly e registra tudo como *scoped*.
- **`IDispatcher`** (singleton): cria **um escopo de DI por mensagem**, o que dá a cada query/command o seu próprio `DbContext`.
  Isso permite queries em paralelo no mesmo circuito Blazor. Os invokers tipados ficam em cache, e não há reflection por chamada.
- **Pipeline** (`IPipelineBehavior`, executados na ordem de registro):
  `LoggingBehavior` (tempo/erros), `ValidationBehavior` (DataAnnotations no servidor, inclusive em modelos aninhados),
  `PermissionCacheBehavior` (commands marcados com `IInvalidatesPermissions` limpam o cache de permissões).
- **Na UI**, o código que se repetia virou helpers:
  - `UiDispatcher.SendAsync`: loading + toast de sucesso/erro, retorna `CommandResult<T>`.
  - `UiDispatcher.DeleteAsync` / `ConfirmAndSendAsync`: confirmação + envio.
  - `Ui.Paged(query, StateHasChanged)` → `PagedQueryState<T>`: loading inicial/refresh, descarte de respostas obsoletas, `ApplyAsync` (filtro → página 1), paginação; `<PagedPager State="..."/>` se liga direto a ele.
  - `EditorBase<TModel>`: ciclo abrir → carregar → salvar → fechar → `OnSaved` dos editores (usuário, papel, time).

Para criar uma funcionalidade nova: crie a pasta do caso de uso, declare o `record` da query/command e o handler em arquivos separados, e use `Ui.QueryAsync` / `Ui.SendAsync` na página. Não é preciso registrar nada.

### Idiomas (localization com resource files)
- **Configuração** (`Program.cs`): `AddLocalization(ResourcesPath = "Resources")` + `UseRequestLocalization` com pt-BR (padrão), en-US e es-ES (lista em `Localization/AppCultures.cs`).
  A cultura vem do cookie `.AspNetCore.Culture`, depois do `Accept-Language` e, por fim, do padrão. O circuito Blazor herda a cultura da conexão.
- **Troca**: `GET /culture/set?culture=en-US&redirectUri=/users` grava o cookie e redireciona (só aceita caminhos locais).
  O `LanguageSelector` usa links com `target="_top"`, então a página recarrega e o novo circuito já nasce no idioma escolhido.
- **Textos**: as páginas usam `@L["texto"]` (`IStringLocalizer<SharedResource>`, injetado em `_Imports.razor`).
  **A chave é o próprio texto em português**, então se faltar tradução aparece o original. Textos com valores usam placeholders: `L["Olá, {0}!", nome]`.
- **Fora dos componentes** (handlers, serviços, `IdentityErrorDescriber`) usa-se `AppText.Get/Format`, que lê os mesmos `.resx`.
  Nas DataAnnotations, `ErrorMessageResourceType = typeof(ValidationMessages)`.
- **Novo texto**: use `L["..."]` no componente e adicione a mesma chave em `Resources/SharedResource.resx`, `.en.resx` e `.es.resx`.
  **Novo idioma**: crie `SharedResource.<cultura>.resx` e inclua a cultura em `AppCultures.All`.
- Dados cadastrados (nomes de times, departamentos, cargos, textos do whitelabel) não são traduzidos.

### Permissões e roles
- **Permissões** têm `Id` e `Nome` fixos em `Security/Perm.cs` e são sincronizadas com a tabela `Permissions` a cada inicialização. Para adicionar uma, crie a constante e inclua-a em `Perm.All`.
- **Roles** são criadas dinamicamente na tela *Papéis*, que oferece uma matriz de permissões agrupadas. A role de sistema `Administrador` sempre recebe todas.
- Uso em páginas: `@attribute [HasPermission(Perm.UsersView)]`. Em componentes: `<Can Permission="@Perm.UsersCreate">…</Can>`.
- As permissões são resolvidas no banco com cache invalidado a cada alteração, então mudanças em roles valem na hora, sem novo login.

### Whitelabel e times
- Em *Whitelabel* você configura nome, slogan, logo, favicon, cores (com temas prontos), fonte, arredondamento, transição de página, textos do login e do rodapé, e-mail de suporte, além de permitir ou não o branding por time. Há pré-visualização ao vivo.
- Cada **time** pode ter branding próprio: nome exibido, logo e cores. Ao escolher o time no seletor do topo, a aplicação inteira assume a identidade dele.

### Componentes
Toasts, loading global, spinner, skeleton, botão com loading, Modal (central), Drawer (lateral), confirmação de exclusão (simples ou exigindo digitar o nome), upload (drag & drop, vários arquivos, progresso), upload de imagem com preview, paginação, busca com debounce, autocomplete lazy paginado, lista virtualizada, multi-select, ordenação por coluna, chips de filtros e transições de página. Todos podem ser testados em **/components**.

Os uploads ficam em `App_Data/uploads` e são servidos em `/uploads`.

### Atualizando os prints

O script `tools/screenshots.mjs` não tem dependências. Ele controla o Chrome pelo DevTools Protocol, usando o WebSocket nativo do Node 20+.

```bash
# 1) app rodando em http://localhost:5299   2) Chrome headless com depuração remota
dotnet run --project BlazorApp3 --urls http://localhost:5299
chrome --headless=new --remote-debugging-port=9222 --user-data-dir=%TEMP%\shots-profile
# 3) captura (opcional: lista de nomes para capturar só alguns, ex.: 03-usuarios,17-menu-meus-times)
node --experimental-websocket tools/screenshots.mjs docs/screenshots
```
