using App.Application;
using App.Parsers;
using App.Parsers.Sources;
using System.Net;
using Google.Cloud.Storage.V1;
using Google.Cloud.Tasks.V2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistenceInfrastructure(configuration);
        services.AddSingleton<IDomainConfigurationProvider, FileDomainConfigurationProvider>();
        services.AddSingleton<ISystemRulesProvider, FileSystemRulesProvider>();
        services.AddSingleton(CvWorkflowSettingsFactory.Create(configuration));
        services.AddSingleton(DemoSettingsFactory.Create(configuration));
        services.AddSingleton(CodexCliOptions.FromConfiguration(configuration));

        services.AddScoped<IDemoSessionService, DemoSessionService>();
        services.AddScoped<IDemoWorkspaceSeeder, DemoWorkspaceSeeder>();
        services.AddScoped<IJobPostingQueryService, JobPostingQueryService>();
        services.AddScoped<IManualJobPostingService, ManualJobPostingService>();
        services.AddScoped<IApplicationStatusService, ApplicationStatusService>();
        services.AddScoped<IApplicationStatusStore, ApplicationStatusStore>();
        services.AddScoped<IInterviewNoteService, InterviewNoteService>();
        services.AddScoped<ICvTemplateQueryService, CvTemplateQueryService>();
        services.AddScoped<ICvTemplateCommandService, CvTemplateCommandService>();
        services.AddScoped<ICandidateRuleQueryService, CandidateRuleQueryService>();
        services.AddScoped<ICvGenerationService, CvGenerationService>();
        services.AddScoped<ICvGenerationStore, CvGenerationStore>();
        services.AddScoped<IGeneratedCvQueryService, GeneratedCvQueryService>();
        services.AddScoped<ICvCompilationService, CvCompilationService>();
        services.AddScoped<ICvCompilationStore, CvCompilationStore>();
        services.AddScoped<IPdfArtifactService, PdfArtifactService>();
        services.AddScoped<ICvGenerationJobProcessor, CvGenerationJobProcessor>();
        services.AddScoped<ICvCompilationJobProcessor, CvCompilationJobProcessor>();
        services.AddScoped<IGmailImportStore, GmailImportStore>();
        services.AddScoped<IGmailMessageProcessor, GmailMessageProcessor>();

        var gmailSyncSettings = GmailConfiguration.CreateSyncSettings(configuration);
        services.AddSingleton(gmailSyncSettings);
        services.AddSingleton<IEmailProvider>(_ => new GoogleGmailEmailProvider(
            GmailConfiguration.CreateOAuthSettings(configuration)));

        services.AddSingleton(new JobPostingDetailsFetcherOptions(
            ConfigurationValues.GetPositiveInt(
                configuration,
                "JobPostingDetails:MaxResponseBytes",
                PracujPlJobPostingDetailsFetcher.DefaultMaxResponseBytes)));
        var detailsTimeout = TimeSpan.FromSeconds(ConfigurationValues.GetPositiveInt(
            configuration,
            "JobPostingDetails:TimeoutSeconds",
            PracujPlJobPostingDetailsFetcher.DefaultTimeoutSeconds));
        AddJobPostingDetailsFetcher<LinkedInJobPostingDetailsFetcher>(services, detailsTimeout);
        AddJobPostingDetailsFetcher<JustJoinItJobPostingDetailsFetcher>(services, detailsTimeout);
        AddJobPostingDetailsFetcher<NoFluffJobsJobPostingDetailsFetcher>(services, detailsTimeout);
        AddJobPostingDetailsFetcher<IndeedJobPostingDetailsFetcher>(services, detailsTimeout);
        AddJobPostingDetailsFetcher<PracujPlJobPostingDetailsFetcher>(services, detailsTimeout);
        AddJobPostingDetailsFetcher<TheProtocolJobPostingDetailsFetcher>(services, detailsTimeout);
        services.AddTransient<IJobPostingDetailsEnricher, JobPostingDetailsEnricher>();

        services.AddSingleton<ICvGenerationQueue, PostgresCvGenerationQueue>();
        AddCvCompilationQueue(services, configuration);
        services.AddSingleton<ISourceParser, LinkedInJobParser>();
        services.AddSingleton<ISourceParser, JustJoinItJobParser>();
        services.AddSingleton<ISourceParser, NoFluffJobsParser>();
        services.AddSingleton<ISourceParser, IndeedJobParser>();
        services.AddSingleton<ISourceParser, PracujPlJobParser>();
        services.AddSingleton<ISourceParser, TheProtocolJobParser>();
        services.AddSingleton<ISourceParserRegistry, SourceParserRegistry>();
        services.AddSingleton<ICodexCliClient, CodexCliClient>();
        services.AddSingleton<IAiCvGenerator, DemoCvGenerator>();
        services.AddSingleton<IAiCvGenerator, CodexCliCvGenerator>();
        services.AddSingleton<IAiCvGeneratorResolver, CvGeneratorResolver>();
        services.AddSingleton<ITexSafetyValidator>(_ => new TexSafetyValidator(
            ConfigurationValues.GetPositiveInt(
                configuration,
                "Compilation:MaxTexBytes",
                200_000)));
        services.AddSingleton<ITexCompiler>(_ => new TectonicCompiler(
            TimeSpan.FromSeconds(ConfigurationValues.GetPositiveInt(
                configuration,
                "Compilation:TimeoutSeconds",
                25)),
            ConfigurationValues.GetPositiveInt(
                configuration,
                "Compilation:MaxTexBytes",
                200_000),
            configuration["Compilation:Executable"] ?? "tectonic"));
        AddArtifactStorage(services, configuration);

        return services;
    }

    private static void AddCvCompilationQueue(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Queues:CvCompilation:Provider"] ?? "Postgres";
        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICvCompilationQueue, PostgresCvCompilationQueue>();
            return;
        }

        if (!provider.Equals("CloudTasks", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Queues:CvCompilation:Provider must be 'Postgres' or 'CloudTasks'.");
        }

        services.AddSingleton(
            CloudTasksCvCompilationQueueOptions.FromConfiguration(configuration));
        services.AddSingleton(_ => CloudTasksClient.Create());
        services.AddSingleton<ICvCompilationQueue, CloudTasksCvCompilationQueue>();
    }

    private static void AddArtifactStorage(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Artifacts:Provider"] ?? "Local";
        if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IArtifactStorage>(_ => new LocalArtifactStorage(
                configuration["Artifacts:Root"]));
            return;
        }

        if (!provider.Equals("GoogleCloud", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Artifacts:Provider must be 'Local' or 'GoogleCloud'.");
        }

        services.AddSingleton(GoogleCloudArtifactStorageOptions.FromConfiguration(configuration));
        services.AddSingleton(_ => StorageClient.Create());
        services.AddSingleton<IArtifactStorage, GoogleCloudArtifactStorage>();
    }

    public static IServiceCollection AddPersistenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = DatabaseConnectionString.Create(configuration);
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IApplicationDatabaseInitializer, ApplicationDatabaseInitializer>();
        return services;
    }

    private static void AddJobPostingDetailsFetcher<TFetcher>(
        IServiceCollection services,
        TimeSpan timeout)
        where TFetcher : class, IJobPostingDetailsFetcher
    {
        services.AddHttpClient<TFetcher>(client =>
            {
                client.Timeout = timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("JobParser/1.0");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pl-PL, pl;q=0.9, en;q=0.8");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All
            });
        services.AddTransient<IJobPostingDetailsFetcher>(provider =>
            provider.GetRequiredService<TFetcher>());
    }
}
