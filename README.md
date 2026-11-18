# Nexus Admin: aplicação whitelabel em Blazor

Blazor Web App (.NET 11), Interactive Server, EF Core + SQLite, ASP.NET Core Identity, SignalR, Hangfire e API REST (JWT + OpenAPI/Scalar).

![Dashboard com o branding do time "Data & Analytics"](docs/screenshots/02-dashboard.png)

**Sumário:** [Produto](#produto) · [Funcionalidades](#funcionalidades) · [Segurança: sessões e MFA](#segurança-sessões-dispositivos-e-mfa) · [Relatórios](#relatórios-excel-e-pdf) · [Armazenamento](#armazenamento-de-arquivos-local-azure-blob-s3) · [API REST](#api-rest) · [Como rodar](#como-rodar) · [Configuração](#configuração-appsettings) · [Arquitetura](#arquitetura) · [Fluxos de código](#fluxos-de-código) · [Pacotes](#pacotes-usados) · [Prints](#atualizando-os-prints)

## Produto

Painel administrativo **white label** para gerir pessoas, times e acessos de uma empresa:

| Para quem | O que resolve |
|---|---|
| **Administradores** | Cadastram usuários, papéis (roles) e times; bloqueiam contas; definem as regras de cadastro, sessão e notificação; exportam relatórios. |
| **Gestores de time** | Mantêm os membros do time e o papel de cada um; o time pode ter nome, logo e cores próprios. |
| **Usuários** | Entram com senha e, se quiserem, um 2º fator (app autenticador); veem e encerram suas sessões; recebem notificações em tempo real; baixam em **Meus relatórios** os relatórios grandes gerados em segundo plano. |
| **Quem revende / implanta** | Troca nome, logo, cores, fonte e textos pela tela (whitelabel), sem recompilar. Interface em português, inglês e espanhol. Escolhe onde ficam os arquivos (disco local ou nuvem, como Azure Blob ou S3). |
| **Sistemas externos / apps** | Usam a **API REST** (`/api`) com JWT + refresh token, sob as mesmas permissões e regras de login da tela. |

Princípios: permissões **fixas no código** e papéis **dinâmicos** na tela; telas e API usam **o mesmo CQRS**; tudo que é lento ou pode falhar (e-mails, entregas, expirações, relatórios grandes) roda em **jobs com nova tentativa**; eventos de segurança chegam **na hora** pelo SignalR.

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

O seletor de times (`MultiSelect`) e o autocomplete (`LazyAutocomplete`) **rolam com a roda do mouse e com toque** mesmo dentro do drawer:
não há mais uma camada invisível por cima da página para detectar o clique fora. Quem fecha o painel é um observador em JS
(`OutsideClickWatcher` + `appDropdown.watch`), e o painel rola sozinho para ficar visível quando abre perto do rodapé.

| Editor | Times com rolagem no drawer |
|---|---|
| ![Editor lateral](docs/screenshots/05-editor-usuario-lateral.png) | ![Dropdown de times](docs/screenshots/27-dropdown-times-no-drawer.png) |

### Dialog central (detalhes)
Mostra também se o usuário tem MFA. Quem tem `Users.Edit` pode **remover o MFA** de outra pessoa (celular perdido); as sessões dela são encerradas.

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
As permissões efetivas são resumidas com **"… +N"**; ao passar o mouse, aparecem todas. O menu também leva à página **Segurança da conta**.

![Menu do usuário](docs/screenshots/16-menu-usuario-permissoes.png)

### Whitelabel
Nome, slogan, logo, favicon, cores (com temas prontos), fonte, arredondamento, efeito de transição de página e textos do login.
A pré-visualização atualiza enquanto você edita. O nome e a logo também vão para os relatórios e para o emissor do MFA no app autenticador.

![Whitelabel](docs/screenshots/15-whitelabel.png)

### Componentes: toasts, loadings, dialogs, upload, busca lazy, paginação e efeitos
A página **Componentes** (`/components`) reúne todos os componentes reutilizáveis para teste.

| Toasts | Loading global |
|---|---|
| ![Toasts](docs/screenshots/19-componentes-toasts.png) | ![Loading](docs/screenshots/20-loading-global.png) |

### Idiomas (português, inglês e espanhol)
O seletor no topo (e no canto da tela de login) troca o idioma de toda a interface, incluindo mensagens de validação, erros de negócio, erros do Identity, toasts, relatórios e o formato de datas e números.
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

### Cadastro e confirmação de e-mail por código
- `/Account/Register`: cadastro público (nome, e-mail, senha com medidor de força, aceite de termos). A conta recebe o papel de `Account:DefaultRole`.
- `/Account/ConfirmEmail`: tela com 6 caixas para o código (colar e autopreenchimento funcionam), reenvio com contagem regressiva e tela de sucesso. Ao confirmar, o usuário já entra.
- Código numérico de 6 dígitos, válido por 15 minutos, 5 tentativas, 60 s entre reenvios (tudo configurável em `Account`). Só o hash fica no banco; um código novo invalida os anteriores.
- Quem tenta entrar sem ter confirmado (e com a senha certa) recebe um código novo e vai para a tela de confirmação.
- **Atalho de desenvolvimento:** em `Development` **e** sem provedor de e-mail configurado (`Pickup`, ou SMTP/SendGrid/Mailtrap sem credenciais), a tela mostra um quadro tracejado "Modo desenvolvimento" com o botão *"Confirmar sem código"*. Em qualquer outro ambiente o botão não aparece e o serviço (`EmailConfirmationService.DevAutoConfirmAsync`) recusa a chamada.

| Cadastro | Confirmação por código |
|---|---|
| ![Cadastro](docs/screenshots/25-cadastro.png) | ![Confirmar e-mail](docs/screenshots/26-confirmar-email.png) |

### Bloqueio de usuários
- Permissão `Users.Block`. Na listagem: ação por linha, em lote e no detalhe. Pede duração (1 h, 24 h, 7 dias, 30 dias ou indeterminado) e motivo.
- Usa o lockout do Identity e **encerra todas as sessões** do usuário na hora (SignalR + revogação no banco). O usuário recebe um e-mail com o motivo. No login, o motivo só aparece se a senha estiver correta.
- Bloqueio temporário: o fim é agendado no Hangfire, que avisa o usuário quando o acesso volta.
- Status "Bloqueado" e "Pendente" (e-mail não confirmado) na listagem, com filtros próprios. O bloqueio por excesso de tentativas de senha também aparece e pode ser desfeito.
- Inativar o usuário ou trocar a senha dele pelo editor também encerra as sessões abertas.

![Bloquear usuário](docs/screenshots/28-bloquear-usuario.png)

### Notificações (sistema + e-mail) em tempo real
- Sino no topo com badge de não lidas. A notificação nova chega pelo SignalR (`/hubs/notifications`): o sino balança, o badge pulsa e aparece um toast. Dá para marcar como lida, marcar todas e limpar as lidas.
- Eventos: novo cadastro (para quem tem `Users.Edit`), conta confirmada, entrada em um time, bloqueio/desbloqueio, alerta de excesso de tentativas de senha, **novo acesso à conta** e **relatório pronto / falha ao gerar relatório** (com link para *Meus relatórios*).
- Publicação via `INotifier.Publish(new NotificationRequest { ... })`. A entrega (banco, SignalR e um e-mail por destinatário) roda no Hangfire.

![Notificações](docs/screenshots/31-notificacoes.png)

### Configurações do sistema (`/settings/system`, permissão `Settings.System`)
Opções que podem ser ligadas e desligadas: cadastro público, exigir confirmação de e-mail, avisar administradores sobre novos cadastros,
**sessão única**, **avisar sobre novos acessos**, alertas de segurança, notificações no sistema e notificações por e-mail.
A tela também mostra o provedor de e-mail ativo e tem "Enviar e-mail de teste" e "Testar notificação".

![Configurações do sistema](docs/screenshots/30-configuracoes-sistema.png)

### Jobs em segundo plano (Hangfire)
Painel em `/jobs` (permissão `Jobs.View`). O armazenamento é SQLite, em `hangfire.db` separado do `app.db`. A fila `emails` tem prioridade sobre a `default`.

| Job | Tipo |
|---|---|
| Código de confirmação, e-mails de notificação, e-mail de teste | fire-and-forget, até 5 novas tentativas |
| Entrega de notificações (banco + SignalR + e-mails) | fire-and-forget |
| Fim de bloqueio temporário | agendado |
| Gerar relatório em segundo plano (`ReportJobs.GenerateAsync`) | fire-and-forget, sem nova tentativa (falha vira notificação) |
| E-mail do relatório pronto (com anexo ou link) | fire-and-forget, fila `emails`, até 5 novas tentativas |
| Remover cadastros não confirmados (após 7 dias) | recorrente, diário às 3h UTC |
| Remover relatórios expirados (arquivo + registro) | recorrente, diário às 4h UTC |
| Limpar códigos expirados | recorrente, de hora em hora |

### E-mail com qualquer provedor
`IMailSender` (usado pela aplicação) delega ao `IEmailProvider` escolhido em `Email:Provider`:
- `Pickup` (padrão): grava arquivos `.eml` em `App_Data/emails`, útil em desenvolvimento.
- `Smtp`: qualquer servidor SMTP, como Mailtrap Sandbox, Gmail, Office 365 ou Amazon SES SMTP.
- `SendGrid`: Web API v3 (`Email:SendGrid:ApiKey`).
- `Mailtrap`: Email API (`Email:Mailtrap:ApiToken`; o `Endpoint` aceita a URL de envio ou a do sandbox).

Para outro provedor (Postmark, Mailgun, Resend...), implemente `IEmailProvider` (`Name` + `SendAsync`), registre com `services.AddEmailProvider<MeuProvider>()` e configure `Email:Provider` com o nome dele. Guarde as credenciais em user-secrets ou em variáveis de ambiente (ex.: `Email__SendGrid__ApiKey`). Para os links dos e-mails, defina `App:PublicUrl` em produção.

**Anexos:** `EmailMessage.Attachments` (`EmailAttachment(FileName, ContentType, Content)`) e a sobrecarga `IMailSender.SendAsync(..., attachments, ...)` funcionam nos quatro provedores embutidos (SMTP/Pickup via `System.Net.Mail`; SendGrid e Mailtrap em base64). É assim que o relatório pronto vai anexado. Provedores customizados devem ler `message.Attachments`.

`EmailOptions.IsDeliveryConfigured()` diz se há entrega real: `Pickup` ou um provedor sem credenciais conta como "não configurado" (é o que libera o atalho de confirmação em desenvolvimento).

## Segurança: sessões, dispositivos e MFA

### Sessões por dispositivo
Cada login cria uma linha em **`UserSessions`** (id da sessão, navegador, sistema, tipo de dispositivo, IP, criação, último acesso, se usou MFA e, quando encerrada, quando, por quê e por qual sessão).
O id vai no cookie como o claim `session_id`. Cada aba conectada ao SignalR grava o seu **connection id** em **`UserConnections`** e entra no grupo `session:{id}`,
o que permite derrubar exatamente aquele navegador. As conexões são removidas ao desconectar e zeradas quando o app inicia.

A cada requisição, `OnValidatePrincipal` confere a sessão (com cache de 2 min, limpo na revogação): sessão encerrada ou ausente → cookie rejeitado e redirecionamento ao login.

Logins pela **API** também criam uma linha em `UserSessions`, com o hash do refresh token (`RefreshTokenHash`, `RefreshTokenExpiresAt`). Eles aparecem em *Segurança da conta* como qualquer dispositivo e seguem as mesmas regras de sessão única e aviso de novo acesso. Encerrar a sessão invalida na hora o access token e o refresh token (veja [API REST](#api-rest)).

### Novo acesso: aviso em tempo real e "encerrar a outra sessão"
Com **várias sessões permitidas** (padrão) e *Avisar sobre novos acessos* ligado, quando a mesma conta entra em outro navegador:
1. as abas abertas recebem o evento `newSession` e mostram o aviso **"Novo acesso à sua conta"**, com navegador, sistema, IP e horário (vários avisos ficam em fila);
2. uma notificação de segurança também vai para o sino;
3. **"Fui eu"** fecha o aviso; **"Encerrar esta sessão"** pede **a senha de novo (e o código do MFA, se ativo)** antes de derrubar o outro navegador.

A sessão encerrada recebe `sessionRevoked` pelo SignalR e vai na hora para `/Account/SessionEnded`, com o motivo (encerrada por outro dispositivo, bloqueio, troca de senha...). O cookie dela não vale mais.

| Aviso na sessão original | Confirmação com senha (+ MFA) |
|---|---|
| ![Novo acesso](docs/screenshots/33-alerta-novo-acesso.png) | ![Reautenticação](docs/screenshots/35-reautenticacao.png) |

| Sessão derrubada (o outro dispositivo) |
|---|
| <img src="docs/screenshots/36-sessao-encerrada.png" alt="Sessão encerrada" width="300"> |

Com **Sessão única** ligada, não há aviso: o login novo encerra as outras sessões automaticamente (motivo *replaced*).

### Página "Segurança da conta" (`/account/security`)
Acessível pelo menu do usuário:
- **Sessões ativas**: ícone por tipo de dispositivo, "Esta sessão", ponto verde para quem está online (tem conexão SignalR), selo MFA, último acesso.
  Encerrar uma sessão ou **todas as outras** sempre pede a senha (e o código do MFA). A lista se atualiza sozinha.
- **Verificação em duas etapas (MFA)**: configurar, ver/regenerar códigos de recuperação e desativar.

![Segurança da conta](docs/screenshots/32-seguranca-conta.png)

### MFA (TOTP com app autenticador)
- **Configuração:** QR code (gerado no servidor com QRCoder, sem chamadas externas) e a chave para digitar à mão. O emissor exibido no app é o nome do whitelabel.
  Para ativar, digita-se um código de 6 dígitos do app. Ao ativar, são gerados **10 códigos de recuperação** (`XXXXX-XXXXX`, uso único), que podem ser copiados ou baixados em `.txt`.
- **Ao ativar ou desativar**, as **outras** sessões são encerradas; a atual continua (o security stamp não é alterado, para não derrubar o circuito atual).
- **Login:** depois da senha, quem tem MFA vai para `/Account/LoginWith2fa` (6 caixas, ou "usar código de recuperação"). Erros contam para o lockout.
  A etapa intermediária fica num cookie próprio de curta duração; a sessão só é criada depois do código, marcada com MFA.
- **Desativar e regenerar códigos** pedem senha + código. Um administrador (`Users.Edit`) pode remover o MFA de um usuário pelo detalhe dele.

| Configuração (QR + chave) | 2ª etapa do login |
|---|---|
| ![Configurar MFA](docs/screenshots/34-mfa-configuracao.png) | ![Login MFA](docs/screenshots/37-login-mfa.png) |

## Relatórios Excel e PDF

Botão **Relatório** na listagem de usuários (permissão **`Users.Export`**). O relatório usa **os mesmos filtros e a mesma ordenação da tela**.

O diálogo permite escolher:
- formato (**Excel `.xlsx`** ou **PDF**), título e subtítulo;
- **agrupar por** departamento, cargo ou status (ou sem agrupamento);
- incluir filtros aplicados e total de registros, linhas zebradas, logo e cores da marca;
- no PDF: orientação (retrato/paisagem) e papel (A4, Letter, Legal);
- **colunas**: marcar/desmarcar e **reordenar** (12 disponíveis: nome, e-mail, cargo, departamento, papéis, times, status, cadastro, último acesso, MFA, e-mail confirmado, motivo do bloqueio).

| Diálogo | PDF gerado (agrupado por departamento) |
|---|---|
| ![Diálogo do relatório](docs/screenshots/29-relatorio-dialog.png) | ![PDF](docs/screenshots/38-relatorio-pdf.png) |

Detalhes:
- **Excel (ClosedXML):** datas e números com tipo de verdade (não texto), cabeçalho na cor da marca, linhas de grupo, zebra, autofiltro, cabeçalho congelado, largura automática e configuração de impressão.
- **PDF (QuestPDF):** cabeçalho com logo, título, filtros e "gerado por", tabela com cabeçalho repetido em cada página, grupos, zebra e "Página X / Y". A fonte se ajusta ao número de colunas.
- **Multiplataforma:** ambas as bibliotecas são .NET puro + SkiaSharp/OpenXML, sem Office, sem navegador headless e sem GDI. Funcionam em Windows, Linux (inclusive containers) e macOS.
- Até o limite de segundo plano (1000 registros): geração fora da thread do circuito (`Task.Run`) e download imediato por `DotNetStreamReference` (`appFiles.download`), sem arquivo temporário no servidor. Limite absoluto de 50.000 linhas.
- Para criar o relatório de outra tela: declare as colunas com `ReportColumn<T>` (veja `UsersReport.Columns()`), faça uma query que devolva os itens e chame `ReportService.RenderAsync(items, columns, options)`; o `ReportDialog` é genérico.
- **Licença do QuestPDF:** configurada como *Community* em `Program.cs` (gratuita para empresas com receita bruta anual abaixo de US$ 1 milhão). Acima disso é preciso uma licença paga.

### Relatórios grandes em segundo plano (notificação + e-mail)

Acima de `Reports:BackgroundThreshold` registros (padrão **1000**), o diálogo mostra um aviso e o botão vira **"Gerar Excel/PDF em segundo plano"**. Ao confirmar:

1. É criada uma linha em **`ReportExports`** (status *Na fila*) e um job do Hangfire (`ReportJobs.GenerateAsync`) é enfileirado. O pedido leva os filtros da tela, as opções do diálogo e os rótulos dos filtros. A **cultura** de quem pediu é capturada pelo Hangfire, então cabeçalhos e datas saem no mesmo idioma.
2. O job refaz a consulta (`ExportUsersQuery`, mesmos filtros e ordenação), renderiza e grava o arquivo no **armazenamento configurado**, na chave privada `reports/{id}.xlsx|pdf` (no disco local, `App_Data/reports`).
3. O usuário recebe a **notificação** "Relatório pronto" no sino, em tempo real, com link para **Meus relatórios**.
4. Se o **e-mail estiver configurado** (cópia por e-mail ligada em *Sistema* e um provedor que entrega de verdade; em `Development` o Pickup também conta), um segundo job envia o e-mail. O arquivo vai **anexado** até `Reports:MaxEmailAttachmentMb` (10 MB); acima disso, o e-mail leva só o botão **Baixar relatório**.
5. Em caso de erro (ex.: mais de 50.000 linhas), o status vira *Falhou* e a notificação "Falha ao gerar relatório" mostra o motivo.

**Meus relatórios** (`/reports`, no menu do usuário): lista os pedidos com status (*Na fila*, *Gerando...*, *Pronto*, *Falhou*, *Expirado*), registros, tamanho, data do pedido e validade. A lista se atualiza sozinha enquanto há relatório em andamento. Dá para baixar e excluir.

**Download:** `GET /reports/{id}/download` exige login (cookie) e só atende o **dono** do relatório. Sem login, vai para a tela de entrar e volta. Com armazenamento em nuvem, o endpoint redireciona para uma URL assinada de 5 minutos. Os arquivos expiram em `Reports:RetentionDays` (7 dias), e um job diário apaga os vencidos e as falhas antigas.

**Outra tela em segundo plano:** implemente `IBackgroundReportSource` (`Key` + `RenderAsync`; veja `UsersReportSource`), registre em `AddReports` (`Features/Reports/ReportsServiceCollectionExtensions.cs`) e, na página, use `BackgroundReports.ShouldRunInBackground(total)` e `BackgroundReports.EnqueueAsync(chave, filtros, opções, ...)`. No `ReportDialog`, passe `Background` e `BackgroundThreshold`.

## Armazenamento de arquivos (local, Azure Blob, S3)

Uploads (logos, avatares, anexos) e relatórios gerados em segundo plano passam pelo `IFileStorage` (`Services/Files/Storage`). O padrão é o **disco local** (`LocalFileStorageProvider`, em `App_Data`). Para usar nuvem, basta implementar `IFileStorageProvider` e registrar; **nenhum pacote de nuvem vem instalado**.

| Chave | Conteúdo | Visibilidade | Como sai |
|---|---|---|---|
| `uploads/{pasta}/{guid}.{ext}` | logos, avatares, anexos | `Public` | `/uploads/...` (sem login) |
| `reports/{guid}.{ext}` | relatórios em segundo plano | `Private` | `/reports/{id}/download` (só o dono) |

- **Escolha do provedor:** `Storage:Provider` com o `Name` do provedor. Vazio: se houver **exatamente um** provedor de nuvem registrado, ele é usado; se não houver nenhum, usa o `Local`. O provedor ativo aparece no log ao iniciar (`Armazenamento de arquivos: ...`).
- **URLs estáveis:** o banco guarda sempre `/uploads/...`. No disco local, a URL é servida como arquivo estático. Na nuvem, o app redireciona para a URL que o provedor devolver em `GetDownloadUrlAsync` (blob público, SAS, URL pré-assinada) ou, se ele devolver `null`, repassa o conteúdo lido por `OpenReadAsync`.
- **Downloads privados:** o endpoint confere o dono antes de pedir a URL ao provedor, com validade de 5 minutos.
- **Trocar de provedor não copia os arquivos já existentes:** migre `App_Data/uploads` e `App_Data/reports` para o container/bucket mantendo as mesmas chaves.

Exemplo com **Azure Blob Storage** (`dotnet add package Azure.Storage.Blobs`):

```csharp
public sealed class AzureBlobStorageProvider(IConfiguration config) : IFileStorageProvider
{
    private readonly BlobContainerClient _container = new(config["Storage:Azure:ConnectionString"], config["Storage:Azure:Container"]);
    public string Name => "AzureBlob";

    public async Task<long> SaveAsync(string key, Stream content, string contentType, StorageVisibility visibility, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(key);
        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, ct);
        return (await blob.GetPropertiesAsync(cancellationToken: ct)).Value.ContentLength;
    }

    public async Task<StoredFileStream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(key);
        if (!await blob.ExistsAsync(ct)) return null;
        var r = (await blob.DownloadStreamingAsync(cancellationToken: ct)).Value;
        return new StoredFileStream(r.Content, r.Details.ContentType, r.Details.ContentLength);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default) => await _container.GetBlobClient(key).ExistsAsync(ct);
    public Task DeleteAsync(string key, CancellationToken ct = default) => _container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: ct);

    public Task<string?> GetDownloadUrlAsync(string key, TimeSpan expiresIn, string? downloadFileName = null, CancellationToken ct = default)
    {
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(expiresIn))
        {
            BlobContainerName = _container.Name, BlobName = key,
            ContentDisposition = downloadFileName is null ? null : $"attachment; filename=\"{downloadFileName}\""
        };
        return Task.FromResult<string?>(_container.GetBlobClient(key).GenerateSasUri(sas).ToString());
    }
}
```

Exemplo com **Amazon S3** (`dotnet add package AWSSDK.S3`); também serve para MinIO, Cloudflare R2 e similares:

```csharp
public sealed class S3StorageProvider(IAmazonS3 s3, IConfiguration config) : IFileStorageProvider
{
    private readonly string _bucket = config["Storage:S3:Bucket"]!;
    public string Name => "S3";

    public async Task<long> SaveAsync(string key, Stream content, string contentType, StorageVisibility visibility, CancellationToken ct = default)
    {
        using var ms = new MemoryStream(); // o SDK precisa do tamanho; para arquivos grandes use TransferUtility
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;
        await s3.PutObjectAsync(new PutObjectRequest { BucketName = _bucket, Key = key, InputStream = ms, ContentType = contentType }, ct);
        return ms.Length;
    }

    public async Task<StoredFileStream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var r = await s3.GetObjectAsync(_bucket, key, ct);
            return new StoredFileStream(r.ResponseStream, r.Headers.ContentType, r.ContentLength);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound) { return null; }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try { await s3.GetObjectMetadataAsync(_bucket, key, ct); return true; }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound) { return false; }
    }
    public Task DeleteAsync(string key, CancellationToken ct = default) => s3.DeleteObjectAsync(_bucket, key, ct);

    public async Task<string?> GetDownloadUrlAsync(string key, TimeSpan expiresIn, string? downloadFileName = null, CancellationToken ct = default) =>
        await s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _bucket, Key = key, Verb = HttpVerb.GET, Expires = DateTime.UtcNow.Add(expiresIn),
            ResponseHeaderOverrides = { ContentDisposition = downloadFileName is null ? null : $"attachment; filename=\"{downloadFileName}\"" }
        });
}
```

Registro em `Program.cs` (depois de `AddFileStorage`):

```csharp
builder.Services.AddFileStorageProvider<AzureBlobStorageProvider>();   // ou S3StorageProvider (+ AddAWSService<IAmazonS3>())
```

## API REST

API em **`/api`** (`Api/`) sobre **o mesmo CQRS das telas**: cada endpoint só traduz HTTP ⇄ query/command, então validação, regras de negócio e permissões são as mesmas.

- **Autenticação:** JWT no header `Authorization: Bearer <accessToken>`. O cookie do app também é aceito, o que serve para chamadas feitas do próprio navegador logado. O esquema `CookieOrBearer` usa o Bearer só quando a rota é `/api` **e** o header vem preenchido. Nas rotas `/api`, falta de login ou de permissão devolve **401/403** em vez de redirecionar para a tela de login.
- **Autorização:** as mesmas permissões fixas (`Perm.*`) via `.RequirePermission(...)` (basta **uma** das permissões listadas).
- **Erros:** `ProblemDetails`. Regra de negócio ou validação (`AppException`) → **400**; 404 e 401 vêm com o status puro (sem a página "não encontrado").
- **JSON:** enums como texto (ex.: `"role": "Manager"`). Paginação com `page` e `pageSize` (máx. **100**).
- **Documentação (só em Development):** OpenAPI em `/openapi/v1.json` e **Scalar** em `/scalar`. O perfil `api` do `launchSettings.json` já abre o Scalar. Para testar, chame `POST /api/auth/token`, copie o `accessToken` e cole em *Authentication* (Bearer).

### Login, refresh e logout (`/api/auth`)

```bash
# 1) login (com MFA ativo, envie também "code": código do app autenticador ou de recuperação)
curl -X POST https://localhost:7146/api/auth/token -H "Content-Type: application/json" \
     -d '{"email":"admin@demo.com","password":"Admin@123"}'
# → { "accessToken": "...", "accessTokenExpiresAt": "...", "refreshToken": "{sessionId}.{segredo}", "refreshTokenExpiresAt": "...", "tokenType": "Bearer" }

# 2) chamadas
curl https://localhost:7146/api/users?status=active&pageSize=50 -H "Authorization: Bearer <accessToken>"

# 3) access token expirou (15 min): troque o refresh token por um par novo
curl -X POST https://localhost:7146/api/auth/refresh -H "Content-Type: application/json" -d '{"refreshToken":"<refreshToken>"}'

# 4) logout: encerra a sessão (access e refresh token deixam de valer na hora)
curl -X POST https://localhost:7146/api/auth/logout -H "Authorization: Bearer <accessToken>"
```

O login segue **as mesmas regras da tela** (conta inativa, bloqueio com motivo, e-mail não confirmado, lockout por tentativas, MFA). Os erros trazem a extensão `code` para o cliente decidir o que fazer:

| `code` | Status | Quando |
|---|---|---|
| `invalid_credentials` | 401 | E-mail ou senha inválidos |
| `mfa_required` | 401 | A conta tem MFA e o `code` não foi enviado |
| `invalid_mfa_code` | 401 | Código do MFA inválido (conta para o lockout) |
| `invalid_refresh_token` | 401 | Refresh token inválido, expirado, reutilizado ou sessão encerrada |
| `account_inactive` | 403 | Conta inativa |
| `account_blocked` | 403 | Bloqueio administrativo (o motivo só aparece com a senha correta) |
| `account_locked` | 403 | Bloqueio temporário por excesso de tentativas (dispara o alerta de segurança) |
| `email_not_confirmed` | 403 | Confirmação de e-mail exigida e pendente |

**Tokens e sessões:**
- **Access token:** JWT HMAC-SHA256 com os mesmos claims do cookie (sem o security stamp), mais `session_id` e `amr=mfa`. Validade `Jwt:AccessTokenMinutes` (15).
- **Refresh token:** opaco (`{sessionId}.{segredo}`); só o hash SHA-256 fica no banco. Validade `Jwt:RefreshTokenDays` (14). É **rotacionado a cada uso** com uma atualização atômica: se duas renovações chegam juntas, só uma vence.
- **Reuso detectado:** apresentar um refresh token já trocado encerra a sessão (motivo *security*), porque é sinal de que o token vazou.
- **Validação a cada requisição:** `OnTokenValidated` confere a sessão. Logout, sessão única, bloqueio, inativação ou "encerrar sessão" na tela invalidam o token na hora, sem esperar ele expirar.

### Endpoints

| Recurso | Rotas | Permissão |
|---|---|---|
| **Auth** | `POST /auth/token`, `POST /auth/refresh` (anônimos), `POST /auth/logout` | login |
| **Me** | `GET /me` (perfil, time ativo, permissões efetivas), `GET /me/teams`, `PUT /me/active-team`, `GET /me/notifications?take=`, `POST /me/notifications/read`, `POST /me/notifications/{id}/read`, `DELETE /me/notifications/read` | login |
| **Dashboard** | `GET /dashboard` | `Dashboard.View` |
| **Users** | `GET /users` (filtros: `search`, `roleIds`, `teamIds`, `departments`, `status`, `createdFrom`, `createdTo`, `sortBy`, `sortDesc`, `page`, `pageSize`), `GET /users/departments`, `GET /users/{id}` | `Users.View` |
| | `GET /users/lookup?term=&excludeTeamId=` | `Users.View` ou `Teams.ManageMembers` |
| | `POST /users` (senha obrigatória) · `PUT /users/{id}` · `POST /users/{id}/resend-confirmation` | `Users.Create` · `Users.Edit` · `Users.Edit` |
| | `DELETE /users/{id}` · `POST /users/batch/delete` | `Users.Delete` |
| | `POST /users/batch/activate` · `POST /users/batch/deactivate` | `Users.Edit` |
| | `POST /users/batch/block` (`ids`, `reason`, `hours`; sem `hours` = indeterminado) · `POST /users/batch/unblock` | `Users.Block` |
| **Roles** | `GET /roles`, `GET /roles/{id}` · `GET /roles/options` | `Roles.View` · `Roles.View` ou `Users.View` |
| | `POST /roles` · `PUT /roles/{id}` · `DELETE /roles/{id}` | `Roles.Create` · `Roles.Edit` · `Roles.Delete` |
| **Permissions** | `GET /permissions` (catálogo + papéis que concedem cada uma) | `Permissions.View` ou `Roles.View` |
| **Teams** | `GET /teams`, `GET /teams/{id}`, `GET /teams/{id}/summary` · `GET /teams/options` | `Teams.View` · `Teams.View` ou `Users.View` |
| | `POST /teams` · `PUT /teams/{id}` · `DELETE /teams/{id}` | `Teams.Create` · `Teams.Edit` · `Teams.Delete` |
| | `GET /teams/{id}/members`, `GET /teams/{id}/members/role-counts` | `Teams.View` |
| | `POST /teams/{id}/members` · `PUT /teams/{id}/members/{userId}` · `DELETE /teams/{id}/members/{userId}` | `Teams.ManageMembers` |
| **Branding** | `GET /branding` · `PUT /branding`, `POST /branding/reset` | `Branding.View` · `Branding.Edit` |
| **Settings** | `GET /settings`, `PUT /settings`, `POST /settings/test-email` | `Settings.System` |

Respostas: criação → **201** com `Location`; alteração, exclusão e ações em lote → **204**; envio de e-mail (`resend-confirmation`, `test-email`) → **202** (vai para a fila). A senha nunca volta na resposta. Como na tela, ninguém pode **excluir, inativar ou bloquear a própria conta** (`DELETE /users/{id}` e rotas de lote devolvem 400).

Os relatórios e o download `/reports/{id}/download` ainda **não** têm rota na API: o download usa o cookie do navegador (é o link enviado por e-mail).

**Endpoint novo:** crie o arquivo em `Api/Platform` (recursos globais) ou `Api/Tenant` (recursos do usuário/time) com um `Map<Recurso>Endpoints(this RouteGroupBuilder api)`, use `IDispatcher` com as queries/commands existentes, proteja com `.RequirePermission(Perm.X)` e registre em `ApiEndpoints.MapRestApi`.

## Como rodar

```bash
cd BlazorApp3
dotnet run --launch-profile https   # app em https://localhost:7146
dotnet run --launch-profile api     # mesmo app, abrindo a documentação da API (Scalar) em /scalar
```

A API exige `Jwt:SigningKey` com **pelo menos 32 bytes**: o app não inicia sem ela (`ValidateOnStart`). Em `Development` ela vem de `appsettings.Development.json`, uma chave só para desenvolvimento. Em produção, defina por user-secrets ou pela variável `Jwt__SigningKey`, nunca no repositório.

Na inicialização as migrations pendentes são aplicadas (`Database.MigrateAsync()`) e, se o banco estiver vazio, o seed é executado. Para recriar do zero, apague `app.db` e rode novamente.

### Migrations

O `dotnet-ef` 11 está registrado como ferramenta local em `dotnet-tools.json` (rode `dotnet tool restore` uma vez). As migrations ficam em `BlazorApp3/Data/Migrations`.

```bash
cd BlazorApp3
dotnet ef migrations add NomeDaMudanca -o Data/Migrations   # após alterar entidades/AppDbContext
dotnet ef database update                                    # opcional: a app já aplica ao iniciar
dotnet ef migrations remove                                  # desfaz a última (se ainda não aplicada)
```

| Migration | O que cria |
|---|---|
| `InitialCreate` | Identity, `Permissions`/`RolePermissions`, `Teams`/`TeamMembers`, `BrandingSettings` |
| `Start` | Vazia (marco inicial) |
| `AccountSecurityNotifications` | `EmailConfirmationCodes`, `SystemSettings`, `Notifications` e os campos de bloqueio do usuário (motivo, quando, por quem) |
| `SessionsAndMfa` | `UserSessions`, `UserConnections`, `MfaEnabledAt` e a opção "avisar sobre novos acessos" |
| `ApiRefreshTokens` | Hash e validade do refresh token da API em `UserSessions` |
| `BackgroundReports` | `ReportExports` (relatórios gerados em segundo plano) |

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

Os 180 usuários gerados pelo seed usam a senha `Demo@123`. Nenhuma conta vem com MFA ativo.

Para ver o relatório em segundo plano com os dados do seed (menos de 1000 usuários), baixe o limite: `Reports__BackgroundThreshold=10 dotnet run --launch-profile https`. Depois gere o relatório em **Usuários** e acompanhe o sino, **Meus relatórios** e o `.eml` em `App_Data/emails`.

## Configuração (appsettings)

| Seção | Chaves (padrão) | Para quê |
|---|---|---|
| `ConnectionStrings` | `DefaultConnection` (`app.db`), `Hangfire` (`hangfire.db`) | Bancos SQLite do app e dos jobs |
| `App` | `PublicUrl` (vazio = primeira requisição) | Links absolutos nos e-mails enviados por jobs |
| `Jwt` | `Issuer`, `Audience`, `SigningKey` (≥ 32 bytes, obrigatória), `AccessTokenMinutes` (15), `RefreshTokenDays` (14) | Tokens da API |
| `Account` | `DefaultRole`, `CodeLength` (6), `CodeLifetimeMinutes` (15), `MaxCodeAttempts` (5), `ResendCooldownSeconds` (60), `UnconfirmedAccountRetentionDays` (7) | Cadastro e confirmação de e-mail |
| `Reports` | `BackgroundThreshold` (1000), `RetentionDays` (7), `MaxEmailAttachmentMb` (10) | Relatórios em segundo plano |
| `Storage` | `Provider` (vazio = automático), `Local:RootPath` (`App_Data`) | Onde ficam uploads e relatórios |
| `Email` | `Provider` (`Pickup`), `FromAddress`, `FromName`, `Pickup:Directory`, `Smtp:*`, `SendGrid:*`, `Mailtrap:*` | Envio de e-mails |

Qualquer chave pode vir de variável de ambiente com `__` no lugar de `:` (ex.: `Storage__Provider=AzureBlob`, `Email__Smtp__Password=...`). Segredos ficam em user-secrets ou variáveis, não no `appsettings.json`.

## Arquitetura

### Visão geral

```mermaid
flowchart LR
    subgraph Navegador
        SSR["Páginas de conta<br/>(SSR estático: login, MFA, cadastro)"]
        UI["Páginas interativas<br/>(Blazor Server via circuito)"]
        JS["app.js<br/>appNotify · appDropdown · appFiles"]
    end
    EXT["Cliente da API<br/>(apps, integrações)"]

    subgraph Servidor["ASP.NET Core (.NET 11)"]
        ID["Identity + cookie<br/>OnValidatePrincipal"]
        JWT["JWT Bearer<br/>OnTokenValidated"]
        API["Minimal APIs /api<br/>RequirePermission"]
        COMP["Componentes Razor<br/>UiDispatcher"]
        CQRS["IDispatcher<br/>Logging → Validation → PermissionCache"]
        H["Handlers<br/>(Features/*)"]
        SVC["Serviços<br/>UserSessions · ReportService · Branding · Notifier"]
        FS["IFileStorage<br/>uploads · reports"]
        HUB["NotificationsHub<br/>/hubs/notifications"]
        HF["Hangfire<br/>e-mails · entregas · expirações · relatórios"]
    end

    DB[("app.db<br/>SQLite / EF Core")]
    HDB[("hangfire.db")]
    MAIL["Provedor de e-mail<br/>Pickup · SMTP · SendGrid · Mailtrap"]
    STO[("Disco local (App_Data)<br/>ou nuvem: Azure Blob · S3")]

    SSR -- "POST form" --> ID
    UI <-- "SignalR (circuito)" --> COMP
    JS <-- "SignalR (hub)" --> HUB
    EXT -- "HTTPS + Bearer" --> JWT --> API
    API --> CQRS
    COMP --> CQRS --> H --> DB
    H --> SVC
    SVC --> HUB
    SVC --> HF --> HDB
    HF --> MAIL
    HF --> FS
    COMP --> FS --> STO
    ID --> SVC
    JWT --> SVC
    HUB --> DB
```

- **Dois canais SignalR:** o circuito do Blazor (interface) e o hub `/hubs/notifications` (eventos: `notification`, `newSession`, `sessionRevoked`). O hub é que sabe qual aba pertence a qual sessão.
- **Páginas de conta em SSR estático** (`[ExcludeFromInteractiveRouting]`, `AuthLayout`): precisam do `HttpContext` para gravar cookies.
- **Uma linha de execução por mensagem**: o `IDispatcher` cria um escopo de DI (e um `DbContext`) para cada query/command.
- **Telas, API e jobs usam o mesmo CQRS**: o relatório em segundo plano roda `ExportUsersQuery` dentro do job, e a API chama as mesmas queries/commands das páginas.

### Pastas

Cada arquivo `.cs` contém, em regra, **um único tipo** (classe, record, enum ou interface). As fatias maiores e novas de segurança agrupam os records de mensagem de um mesmo recurso num arquivo (`MfaFeature.cs`, `SessionsFeature.cs`).

| Pasta | Conteúdo |
|---|---|
| `Api/Common/` | `ApiEndpoints` (grupo `/api`, `RequirePermission`, filtro de `AppException` → 400), `ApiDocumentation` (OpenAPI + Scalar) |
| `Api/Common/Auth/` | `AuthEndpoints` (token, refresh, logout), `JwtTokenService` (emissão e rotação), `JwtAuthentication` (esquema `CookieOrBearer`, `JwtOptions`) |
| `Api/Platform/`, `Api/Tenant/` | Endpoints por recurso: `Users`, `Roles`/`Permissions`, `System` (dashboard, branding, settings) · `Me`, `Teams` |
| `Data/Entities/` | Uma entidade por arquivo (`AppUser`, `AppRole`, `Team`, `TeamMember`, `BrandingSettings`, `SystemSettings`, `UserSession` + `UserConnection`, `ReportExport`...) |
| `Data/` | `AppDbContext` (Identity), `DbSeeder`, `Migrations/` |
| `Security/` | `Perm` (catálogo fixo) e `PermissionDefinition` |
| `Security/Authorization/` | `HasPermissionAttribute`, `PermissionRequirement`, `PermissionPolicyProvider`, `PermissionHandler`, `PermissionService` (cache) |
| `Security/Identity/` | `UserSessions` (sessões, 2FA pendente, revogação), `DeviceInfo` (User-Agent), `SessionEvents`, revalidação do circuito, `AppClaimsPrincipalFactory`, `LocalizedIdentityErrorDescriber` |
| `Application/Cqrs/` | CQRS próprio (sem pacotes, só DI): `Abstractions/`, `Behaviors/`, `Dispatching/` |
| `Features/<Feature>/` | Fatias verticais: `Models/`, `Shared/`, `Queries/<Caso>/` e `Commands/<Caso>/` |
| `Features/Account/Security/` | `MfaFeature` (status, configurar, ativar, desativar, regenerar, resetar), `SessionsFeature` (listar, encerrar), `ReauthVerifier` |
| `Features/Reports/` | Relatórios em segundo plano: `BackgroundReports` (decide e enfileira), `IBackgroundReportSource`, `ReportStorage`, `Jobs/ReportJobs` (gerar, e-mail, limpeza), `MyReportsQuery`/`DeleteReportCommand`, endpoint de download |
| `Services/` | `Common/`, `Ui/` (`UiDispatcher`, `PagedQueryState`), `Toasts/`, `Loading/`, `Confirm/`, `Branding/`, `Files/`, `Email/`, `Reports/` |
| `Services/Files/` | `FileStorageService` (uploads públicos), `JsFileExtensions` (download/cópia) e `Storage/` (`IFileStorageProvider`, `IFileStorage`, `LocalFileStorageProvider`, `StorageKey`, `UseUploads`) |
| `Services/Reports/` | `ReportModels` (colunas, opções, `ReportBuilder`), `ExcelReportWriter`, `PdfReportWriter`, `ReportService`, `ReportsOptions` |
| `Localization/` | `SharedResource`, `AppCultures`, `AppText`, `ValidationMessages`, `Money` |
| `Resources/` | `SharedResource.resx` (pt-BR), `.en.resx`, `.es.resx` |
| `Components/UI/` | Componentes reutilizáveis (`EditorBase<T>`, `PagedPager`, `MultiSelect`, `LazyAutocomplete`, `OutsideClickWatcher`, `ReauthDialog`, `ReportDialog`...) |
| `Components/Layout/` | `MainLayout`, `NavMenu`, `UserMenu`, `NotificationBell`, `SessionAlertHost` |
| `Components/Pages/` | Dashboard, Usuários, Papéis, Permissões, Times, Whitelabel, Sistema, Segurança da conta, Meus relatórios, Componentes |
| `Components/Account/` | Login, LoginWith2fa, Register, ConfirmEmail, SessionEnded (SSR estático) |
| `wwwroot/js/app.js` | Ponte JS: hub de notificações, dropdowns, download/cópia, efeitos |

Exemplo de caso de uso:

```
Features/Users/
├── Models/UserEditModel.cs, UserListItem.cs, ...
├── Shared/UserAssignments.cs
├── Queries/SearchUsers/SearchUsersQuery.cs + SearchUsersHandler.cs
├── Queries/ExportUsers/ExportUsersQuery.cs        (reaproveita filtros/ordenação do SearchUsers)
└── Commands/CreateUser/CreateUserCommand.cs + CreateUserHandler.cs
```

### CQRS (commands e queries só com DI)

- **Mensagens**: `IQuery<TResult>`, `ICommand<TResult>` e `ICommand` (que retorna `Unit`). Listagens herdam de `PagedQuery<TItem>` (Page/PageSize).
- **Handlers**: `IQueryHandler<,>` / `ICommandHandler<,>`, um handler por mensagem.
  `AddCqrs(assembly)` varre o assembly e registra tudo como *scoped* (inclusive handlers `internal sealed`).
- **`IDispatcher`** (singleton): cria **um escopo de DI por mensagem**, o que dá a cada query/command o seu próprio `DbContext`.
  Isso permite queries em paralelo no mesmo circuito Blazor. Os invokers tipados ficam em cache, e não há reflection por chamada.
- **Pipeline** (`IPipelineBehavior`, executados na ordem de registro):
  `LoggingBehavior` (tempo/erros), `ValidationBehavior` (DataAnnotations no servidor, inclusive em modelos aninhados),
  `PermissionCacheBehavior` (commands marcados com `IInvalidatesPermissions` limpam o cache de permissões).
  Mensagens que carregam senha sobrescrevem `ToString()`, então a senha não aparece no log.
- **Na UI**, o código que se repetia virou helpers:
  - `UiDispatcher.SendAsync`: loading + toast de sucesso/erro, retorna `CommandResult<T>`.
  - `UiDispatcher.DeleteAsync` / `ConfirmAndSendAsync`: confirmação + envio.
  - `Ui.Paged(query, StateHasChanged)` → `PagedQueryState<T>`: loading inicial/refresh, descarte de respostas obsoletas, `ApplyAsync` (filtro → página 1), paginação; `<PagedPager State="..."/>` se liga direto a ele.
  - `EditorBase<TModel>`: ciclo abrir → carregar → salvar → fechar → `OnSaved` dos editores (usuário, papel, time).

Para criar uma funcionalidade nova: crie a pasta do caso de uso, declare o `record` da query/command e o handler, e use `Ui.QueryAsync` / `Ui.SendAsync` na página. Não é preciso registrar nada.

### Idiomas (localization com resource files)
- **Configuração** (`Program.cs`): `AddLocalization(ResourcesPath = "Resources")` + `UseRequestLocalization` com pt-BR (padrão), en-US e es-ES (lista em `Localization/AppCultures.cs`).
  A cultura vem do cookie `.AspNetCore.Culture`, depois do `Accept-Language` e, por fim, do padrão. O circuito Blazor herda a cultura da conexão.
- **Troca**: `GET /culture/set?culture=en-US&redirectUri=/users` grava o cookie e redireciona (só aceita caminhos locais).
  O `LanguageSelector` usa links com `target="_top"`, então a página recarrega e o novo circuito já nasce no idioma escolhido.
- **Textos**: as páginas usam `@L["texto"]` (`IStringLocalizer<SharedResource>`, injetado em `_Imports.razor`).
  **A chave é o próprio texto em português**, então se faltar tradução aparece o original. Textos com valores usam placeholders: `L["Olá, {0}!", nome]`.
- **Fora dos componentes** (handlers, serviços, relatórios, `IdentityErrorDescriber`) usa-se `AppText.Get/Format`, que lê os mesmos `.resx`.
  Nas DataAnnotations, `ErrorMessageResourceType = typeof(ValidationMessages)`.
- **Novo texto**: use `L["..."]` no componente e adicione a mesma chave em `Resources/SharedResource.resx`, `.en.resx` e `.es.resx`.
  **Novo idioma**: crie `SharedResource.<cultura>.resx` e inclua a cultura em `AppCultures.All`.
- Dados cadastrados (nomes de times, departamentos, cargos, textos do whitelabel) não são traduzidos.

### Permissões e roles
- **Permissões** têm `Id` e `Nome` fixos em `Security/Perm.cs` e são sincronizadas com a tabela `Permissions` a cada inicialização. Para adicionar uma, crie a constante e inclua-a em `Perm.All`. A mais recente é `Users.Export` (Id 15), para os relatórios.
- **Roles** são criadas dinamicamente na tela *Papéis*, que oferece uma matriz de permissões agrupadas. A role de sistema `Administrador` sempre recebe todas.
- Uso em páginas: `@attribute [HasPermission(Perm.UsersView)]`. Em componentes: `<Can Permission="@Perm.UsersCreate">…</Can>`.
- As permissões são resolvidas no banco com cache invalidado a cada alteração, então mudanças em roles valem na hora, sem novo login.

### Whitelabel e times
- Em *Whitelabel* você configura nome, slogan, logo, favicon, cores (com temas prontos), fonte, arredondamento, transição de página, textos do login e do rodapé, e-mail de suporte, além de permitir ou não o branding por time. Há pré-visualização ao vivo.
- Cada **time** pode ter branding próprio: nome exibido, logo e cores. Ao escolher o time no seletor do topo, a aplicação inteira assume a identidade dele.

### Componentes
Toasts, loading global, spinner, skeleton, botão com loading, Modal (central), Drawer (lateral), confirmação de exclusão (simples ou exigindo digitar o nome), confirmação com senha + MFA (`ReauthDialog`), diálogo de relatório (`ReportDialog`), upload (drag & drop, vários arquivos, progresso), upload de imagem com preview, paginação, busca com debounce, autocomplete lazy paginado, lista virtualizada, multi-select, ordenação por coluna, chips de filtros e transições de página. A maioria pode ser testada em **/components**.

Os uploads passam pelo `IFileStorage` (chave `uploads/...`; no disco local, `App_Data/uploads`) e são servidos em `/uploads`, seja qual for o provedor. Veja [Armazenamento](#armazenamento-de-arquivos-local-azure-blob-s3).

## Fluxos de código

### Uma ação na tela (CQRS)

```mermaid
sequenceDiagram
    participant P as Página (.razor)
    participant U as UiDispatcher
    participant D as IDispatcher
    participant B as Behaviors
    participant H as Handler
    participant DB as AppDbContext

    P->>U: SendAsync(new UpdateUserCommand(...))
    U->>U: mostra loading
    U->>D: Send(command)
    D->>D: cria escopo de DI (DbContext próprio)
    D->>B: Logging → Validation → PermissionCache
    B->>H: Handle(command)
    H->>DB: SaveChangesAsync
    H-->>U: resultado (ou AppException)
    U-->>P: CommandResult(Succeeded, Value) + toast
```

### Login com senha, MFA e criação da sessão

```mermaid
sequenceDiagram
    actor N as Navegador
    participant L as /Account/Login (SSR)
    participant M as /Account/LoginWith2fa
    participant S as UserSessions
    participant DB as UserSessions (tabela)
    participant HUB as NotificationsHub

    N->>L: POST e-mail + senha
    L->>L: CheckPasswordSignIn (lockout)
    alt MFA ativo
        L->>S: BeginTwoFactorAsync (cookie curto TwoFactorUserId)
        L-->>N: 302 /Account/LoginWith2fa
        N->>M: POST código TOTP ou de recuperação
        M->>S: TwoFactorUserIdAsync → valida código
        M->>S: SignInAsync(mfa: true)
    else sem MFA
        L->>S: SignInAsync(mfa: false)
    end
    S->>DB: insere sessão (navegador, SO, IP)
    S->>S: cookie com claim session_id
    alt Sessão única ligada
        S->>HUB: sessionRevoked → outras sessões (replaced)
    else Avisar novos acessos
        S->>HUB: newSession → outras sessões + notificação no sino
    end
    S-->>N: 302 para a página pedida
```

### Encerrar outra sessão (com reautenticação)

```mermaid
sequenceDiagram
    actor A as Aba original
    participant R as ReauthDialog
    participant C as RevokeSessionCommand
    participant V as ReauthVerifier
    participant S as UserSessions
    participant HUB as Hub (grupo session:{id})
    actor B as Outro navegador

    A->>R: "Encerrar esta sessão"
    R->>C: senha (+ código MFA)
    C->>V: VerifyAsync (lockout, senha, TOTP/recuperação)
    V-->>C: ok
    C->>S: RevokeAsync(alvo, motivo revoked, por esta sessão)
    S->>S: RevokedAt no banco + limpa cache
    S->>HUB: sessionRevoked
    HUB-->>B: redireciona para /Account/SessionEnded
    Note over B: próximas requisições: OnValidatePrincipal rejeita o cookie
```

### Gerar relatório

```mermaid
sequenceDiagram
    participant P as Users.razor
    participant RD as ReportDialog
    participant Q as ExportUsersQuery
    participant RS as ReportService
    participant W as Excel/PdfReportWriter
    participant JS as appFiles.download

    P->>RD: abre com colunas e agrupamentos (+ aviso se total > limite)
    RD-->>P: ReportOptions (formato, colunas, grupo...)
    P->>Q: filtros atuais da tela
    Q-->>P: itens (mesmos filtros/ordenação, máx. 50.000)
    P->>RS: RenderAsync(itens, colunas, opções)
    RS->>RS: branding + logo (lido do IFileStorage)
    RS->>W: ReportBuilder.Build → Render (Task.Run)
    W-->>RS: bytes .xlsx / .pdf
    RS-->>P: arquivo
    P->>JS: DotNetStreamReference → download no navegador
```

Acima do limite (`Reports:BackgroundThreshold`), o mesmo `ReportService` roda dentro de um job:

```mermaid
sequenceDiagram
    participant P as Users.razor
    participant BR as BackgroundReports
    participant DB as ReportExports
    participant J as ReportJobs (Hangfire)
    participant SRC as UsersReportSource
    participant FS as IFileStorage
    participant N as INotifier (sino)
    participant M as E-mail

    P->>BR: EnqueueAsync(filtros, opções, usuário)
    BR->>DB: insere (Na fila)
    BR->>J: Enqueue GenerateAsync (cultura capturada)
    P-->>P: toast "Relatório em processamento"
    J->>DB: Gerando...
    J->>SRC: ExportUsersQuery + ReportService.RenderAsync
    SRC-->>J: arquivo
    J->>FS: SaveAsync("reports/{id}.xlsx", Private)
    J->>DB: Pronto (tamanho, registros, expira em 7 dias)
    J->>N: "Relatório pronto" → link /reports
    opt e-mail configurado
        J->>M: SendEmailAsync (anexo até 10 MB ou só o link)
    end
    Note over J,N: em erro: status Falhou + notificação com o motivo
```

### Login da API e renovação do token

```mermaid
sequenceDiagram
    actor C as Cliente
    participant A as /api/auth
    participant T as JwtTokenService
    participant S as UserSessions
    participant API as /api/*

    C->>A: POST /token (e-mail, senha, code?)
    A->>A: mesmas regras da tela (inativo, bloqueio, e-mail, lockout, MFA)
    A->>T: IssueAsync
    T->>S: cria sessão (hash do refresh token) + política de sessão
    T-->>C: accessToken (15 min) + refreshToken (14 dias)
    C->>API: Authorization: Bearer
    API->>S: OnTokenValidated → sessão ainda vale?
    API-->>C: 200 / 401 / 403 / 400 ProblemDetails
    C->>A: POST /refresh
    A->>T: confere hash → rotação atômica
    T-->>C: novo par (o refresh antigo deixa de valer)
    Note over T,S: refresh antigo reapresentado → sessão encerrada (security)
```

### Implementação: decisões importantes
- **Sessão no banco, não só no cookie:** permite listar, encerrar uma específica e auditar quem encerrou qual. A validação usa cache curto para não consultar o banco a cada requisição.
- **Claims preservados na renovação:** `SecurityStampValidatorOptions.OnRefreshingPrincipal` copia `session_id` e `amr` quando o Identity recria o principal, senão a sessão "sumiria".
- **MFA sem trocar o security stamp:** ativar/desativar usa as APIs de store do Identity e encerra as outras sessões explicitamente; trocar o stamp derrubaria também o circuito atual.
- **Reautenticação centralizada:** `ReauthVerifier` é usado por encerrar sessão, desativar MFA e regenerar códigos; respeita o lockout do Identity.
- **Dropdowns sem camada invisível:** o "clique fora" é detectado em JS (pointerdown em captura), o que devolve a rolagem por roda/toque dentro de drawers e modais.
- **Relatório reaproveita a busca:** `ExportUsersQuery` chama os mesmos `Filter`/`Sort`/`LoadBadges` do `SearchUsersHandler`, então o arquivo bate com a tela.
- **Relatório grande fora do circuito:** acima do limite, gerar e baixar pelo circuito prenderia o usuário na tela e usaria muita memória por conexão. O job grava o arquivo e avisa depois. O job **não tem nova tentativa automática** (uma consulta grande repetida em loop pesaria no banco); a falha vira notificação para a pessoa pedir de novo.
- **Arquivos privados fora de `/uploads`:** relatórios ficam na chave `reports/...` com visibilidade privada e só saem pelo endpoint que confere o dono. Na nuvem, saem por URL assinada curta, gerada depois dessa conferência.
- **Armazenamento trocável sem mudar o banco:** o banco guarda `/uploads/...` e o nome do arquivo do relatório, nunca o endereço do provedor. Trocar de disco local para Azure/S3 é configuração (mais a cópia dos arquivos).
- **API sobre o CQRS existente:** os endpoints não têm regra de negócio própria. O login da API repete as checagens da tela de login, e o token é preso a uma `UserSession`, então encerrar a sessão derruba o token na hora, sem lista de revogação separada.
- **Refresh token com rotação e detecção de reuso:** só o hash fica no banco, e a troca é um `UPDATE ... WHERE hash = antigo` (atômico). Um token antigo reapresentado indica vazamento e encerra a sessão.

## Pacotes usados

### Back-end (NuGet)

| Pacote | Versão | Para quê |
|---|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 11.0.0 | Usuários, papéis, senha, lockout, tokens e TOTP |
| `Microsoft.EntityFrameworkCore.Sqlite` | 11.0.0 | ORM e banco SQLite (`app.db`) |
| `Microsoft.EntityFrameworkCore.Design` | 11.0.0 | Migrations (`dotnet ef`) |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | 11.0.0 | Hospedagem do projeto `BlazorApp3.Client` |
| `Hangfire.AspNetCore` | 1.8.25 | Jobs em segundo plano e painel `/jobs` |
| `Hangfire.Storage.SQLite` | 0.4.3 | Armazenamento dos jobs em `hangfire.db` |
| `SQLitePCLRaw.lib.e_sqlite3` | 3.53.3 | **Correção de segurança** (veja abaixo) |
| `ClosedXML` | 0.105.1 | Relatórios Excel (`.xlsx`) |
| `QuestPDF` | 2026.9.2 | Relatórios PDF (licença Community) |
| `QRCoder` | 1.8.1 | QR code do MFA, gerado no servidor |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 11.0.0 | Autenticação da API por JWT |
| `Microsoft.AspNetCore.OpenApi` | 11.0.0 | Documento OpenAPI da API (`/openapi/v1.json`) |
| `Scalar.AspNetCore` | 2.18.7 | Interface da documentação da API (`/scalar`, só em Development) |

Também do framework (sem pacote extra): ASP.NET Core SignalR, Minimal APIs, `ProblemDetails`, localização (`IStringLocalizer`), Data Protection e `System.Net.Mail` (provedor SMTP, com anexos).
**Armazenamento em nuvem:** nenhum SDK vem instalado. Os pacotes `Azure.Storage.Blobs` ou `AWSSDK.S3` só entram se você implementar o provedor (veja [Armazenamento](#armazenamento-de-arquivos-local-azure-blob-s3)).
Ferramenta local: `dotnet-ef` 11 (`dotnet-tools.json`).

**Correção do SQLite nativo:** o `Hangfire.Storage.SQLite` → `sqlite-net-pcl` → `SQLitePCLRaw.bundle_green` 2.1.11 trazia o `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (GHSA-2m69-gcr7-jv3q).
O `.csproj` fixa a versão 3.53.3 com `ExcludeAssets="all"`, então o processo usa um único `e_sqlite3` nativo, o corrigido que vem com o EF Core. `dotnet list package --vulnerable --include-transitive` fica limpo. Revise ao atualizar o Hangfire.Storage.SQLite.

### Front-end

| Biblioteca | Versão | Origem | Para quê |
|---|---|---|---|
| Blazor Web (`blazor.web.js`) | .NET 11 | framework | Circuito Interactive Server e navegação melhorada |
| Bootstrap | 5.3.8 | `wwwroot/lib` | Grid, utilitários e formulários (tema próprio em `app.css`) |
| Bootstrap Icons | 1.11.3 | CDN jsDelivr | Ícones |
| `@microsoft/signalr` | 8.0.7 | CDN jsDelivr | Cliente do hub de notificações/sessões |
| Google Fonts | — | fonts.googleapis.com | Fonte escolhida no whitelabel |
| `wwwroot/js/app.js` | próprio | — | `appNotify` (hub, sino, avisos de sessão), `appDropdown` (clique fora + rolagem), `appFiles` (download e cópia), efeitos e helpers |

Não há bundler nem `node_modules` na aplicação. O Node (20+) só é usado pelo script de prints.

## Atualizando os prints

O script `tools/screenshots.mjs` não tem dependências. Ele controla o Chrome pelo DevTools Protocol, usando o WebSocket nativo do Node 20+.
Ele força `Accept-Language: pt-BR` (os seletores procuram textos em português) e usa contextos isolados do Chrome para simular um segundo dispositivo
(um "iPhone" entra na mesma conta para gerar o aviso de novo acesso e depois é derrubado).

```bash
# 1) app rodando em http://localhost:5299   2) Chrome headless com depuração remota
dotnet run --project BlazorApp3 --urls http://localhost:5299
chrome --headless=new --remote-debugging-port=9222 --user-data-dir=%TEMP%\shots-profile
# 3) captura (opcional: lista de nomes para capturar só alguns, ex.: 03-usuarios,17-menu-meus-times)
node --experimental-websocket tools/screenshots.mjs docs/screenshots
```

Variáveis opcionais:

| Variável | Uso |
|---|---|
| `BASE` | URL do app (padrão `http://localhost:5299`) |
| `PENDING_EMAIL` | conta com e-mail não confirmado, para o print `26-confirmar-email` |
| `MFA_EMAIL` / `MFA_PASSWORD` | conta com MFA ativo, para o print `37-login-mfa` |

O print do PDF (`38-relatorio-pdf.png`) é gerado com `PdfReportWriter.Create(doc).GenerateImages(...)` do QuestPDF, a partir dos dados do seed.
O script cria sessões e notificações de teste para o admin; se quiser um ambiente limpo depois, encerre as sessões em **Segurança da conta**.
