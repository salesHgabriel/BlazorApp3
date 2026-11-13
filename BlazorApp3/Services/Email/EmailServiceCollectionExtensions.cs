namespace BlazorApp3.Services.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>Registra o <see cref="IMailSender"/> e os provedores embutidos (Pickup, Smtp, SendGrid, Mailtrap).</summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.Section));
        services.AddTransient<IMailSender, MailSender>();
        services.AddEmailProvider<PickupDirectoryEmailProvider>();
        services.AddEmailProvider<SmtpEmailProvider>();
        services.AddHttpClient<SendGridEmailProvider>();
        services.AddTransient<IEmailProvider>(sp => sp.GetRequiredService<SendGridEmailProvider>());
        services.AddHttpClient<MailtrapEmailProvider>();
        services.AddTransient<IEmailProvider>(sp => sp.GetRequiredService<MailtrapEmailProvider>());
        return services;
    }

    /// <summary>Adiciona um provedor de e-mail customizado (selecionado pelo nome em <c>Email:Provider</c>).</summary>
    public static IServiceCollection AddEmailProvider<TProvider>(this IServiceCollection services) where TProvider : class, IEmailProvider =>
        services.AddTransient<IEmailProvider, TProvider>();
}
