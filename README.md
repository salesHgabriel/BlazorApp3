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

| Pasta | Conteúdo |
|---|---|
| `Data/` | Entidades, `AppDbContext` (Identity), `DbSeeder` |
| `Security/` | `Perm` (catálogo fixo), policy provider dinâmico, handler, `PermissionService` (cache), revalidação do Identity |
| `Application/Cqrs/` | CQRS próprio (sem pacotes, só DI): abstrações, `Dispatcher`, pipeline behaviors |
| `Features/<Feature>/` | Queries, commands, DTOs e handlers de Users, Roles, Teams, Branding, Dashboard |
| `Services/` | `UiDispatcher` + `PagedQueryState`, UI (Toast, Loading, Confirm, BrandingState), `EffectiveBranding`, FileStorage |
| `Components/UI/` | Componentes reutilizáveis (incl. `EditorBase<T>` e `PagedPager`) |
| `Components/Pages/` | Dashboard, Usuários, Papéis, Permissões, Times, Whitelabel, Vitrine |
| `Components/Account/` | Login/Logout (SSR estático) |

### CQRS (commands e queries só com DI)

- **Mensagens**: `IQuery<TResult>`, `ICommand<TResult>` e `ICommand` (que retorna `Unit`). Listagens herdam de `PagedQuery<TItem>` (Page/PageSize).
- **Handlers**: `IQueryHandler<,>` / `ICommandHandler<,>`. Uma classe pode tratar várias mensagens (ex.: `UserQueryHandlers`).
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

Para criar uma funcionalidade nova: declare o `record` da query/command, implemente o handler na mesma pasta e use `Ui.QueryAsync` / `Ui.SendAsync` na página. Não é preciso registrar nada.

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
