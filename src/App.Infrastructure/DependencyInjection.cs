using App.Application;
using App.Parsers;
using App.Parsers.Sources;
using System.Net;
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
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=jobparser;Username=jobparser;Password=jobparser";

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDomainConfigurationProvider, FileDomainConfigurationProvider>();
        services.AddSingleton<ISystemRulesProvider, FileSystemRulesProvider>();
        services.AddSingleton(CvWorkflowSettingsFactory.Create(configuration));

        services.AddScoped<IDemoSessionService, DemoSessionService>();
        services.AddScoped<IApplicationDatabaseInitializer, ApplicationDatabaseInitializer>();
        services.AddScoped<IDemoWorkspaceSeeder, DemoWorkspaceSeeder>();
        services.AddScoped<IJobPostingQueryService, JobPostingQueryService>();
        services.AddScoped<IApplicationStatusService, ApplicationStatusService>();
        services.AddScoped<IApplicationStatusStore, ApplicationStatusStore>();
        services.AddScoped<ICvTemplateQueryService, CvTemplateQueryService>();
        services.AddScoped<ICandidateRuleQueryService, CandidateRuleQueryService>();
        services.AddScoped<ICvGenerationService, CvGenerationService>();
        services.AddScoped<ICvGenerationStore, CvGenerationStore>();
        services.AddScoped<IGeneratedCvQueryService, GeneratedCvQueryService>();
        services.AddScoped<ICvCompilationService, CvCompilationService>();
        services.AddScoped<ICvCompilationStore, CvCompilationStore>();
        services.AddScoped<IPdfArtifactService, PdfArtifactService>();
        services.AddScoped<ICvGenerationJobProcessor, CvGenerationJobProcessor>();
        services.AddScoped<ICvCompilationJobProcessor, CvCompilationJobProcessor>();

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
        services.AddTransient<IJobPostingDetailsEnricher, JobPostingDetailsEnricher>();

        services.AddSingleton<ICvGenerationQueue, PostgresCvGenerationQueue>();
        services.AddSingleton<ICvCompilationQueue, PostgresCvCompilationQueue>();
        services.AddSingleton<ISourceParser, LinkedInJobParser>();
        services.AddSingleton<ISourceParser, JustJoinItJobParser>();
        services.AddSingleton<ISourceParser, NoFluffJobsParser>();
        services.AddSingleton<ISourceParser, IndeedJobParser>();
        services.AddSingleton<ISourceParser, PracujPlJobParser>();
        services.AddSingleton<ISourceParserRegistry, SourceParserRegistry>();
        services.AddSingleton<IAiCvGenerator, DemoCvGenerator>();
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
                200_000)));
        services.AddSingleton<IArtifactStorage>(_ => new LocalArtifactStorage(
            configuration["Artifacts:Root"]));

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
