using FisherTournament.Application.Common.Persistence;
using FisherTournament.Application.LeaderBoard;
using FisherTournament.Domain.Common.Provider;
using FisherTournament.Infrastructure.LeaderBoard;
using FisherTournament.Infrastructure.Persistence.Common.Interceptors;
using FisherTournament.Infrastructure.Persistence.ReadModels.EntityFramework;
using FisherTournament.Infrastructure.Persistence.ReadModels.EntityFramework.Repositories;
using FisherTournament.Infrastructure.Persistence.Tournaments;
using FisherTournament.Infrastructure.Provider;
using FisherTournament.Infrastructure.Settings;
using FisherTournament.ReadModels.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FisherTournament.Infrastructure;

public static partial class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services)
	{
		services.AddDbContext<ITournamentFisherDbContext, TournamentFisherDbContext>((provider, options) =>
		{
			var dataBaseConnectionSettings = provider.GetRequiredService<DataBaseConnectionSettings>();
			ArgumentNullException.ThrowIfNull(dataBaseConnectionSettings.TournamentDbConnectionString);
			// options.UseSqlServer(dataBaseConnectionSettings.ConnectionString);
			options.UseSqlite(dataBaseConnectionSettings.TournamentDbConnectionString);
			// options.LogTo(System.Console.WriteLine);
			// options.EnableSensitiveDataLogging();

			options.AddInterceptors(new TraceDbConnectionInterceptor(provider.GetRequiredService<ILogger<TraceDbConnectionInterceptor>>()));

			options.AddInterceptors(new RelaxSqliteDbConnectionInterceptor());
		});

		services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

		AddReadModels(services);
		AddLeaderBoardUpdateServices(services);

		return services;
	}

	private static void AddLeaderBoardUpdateServices(IServiceCollection services)
	{
		services.AddSingleton<ILeaderBoardUpdateScheduler, BatchLeaderBoardUpdateScheduler>();

		services.AddScoped<ILeaderBoardUpdater, LeaderBoardUpdater>();

		services.AddQuartz(q =>
		{
			var jobKey = LeaderBoardUpdateJobExecuter.JobKey;

			q.AddJob<LeaderBoardUpdateJobExecuter>(opts => opts.WithIdentity(jobKey))
				.AddTrigger(opt =>
						opt.ForJob(jobKey)
							.WithSimpleSchedule(s =>
							s.WithInterval(BatchLeaderBoardUpdateScheduler.CallInterval)
							 .RepeatForever()));

			q.UseMicrosoftDependencyInjectionJobFactory();
		});

		services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
	}

	private static void AddReadModels(IServiceCollection services)
	{
		services.AddSingleton<RelaxSqliteDbConnectionInterceptor>();
		services.AddSingleton<TraceDbConnectionInterceptor>();

		services.AddDbContext<ReadModelsDbContext>((provider, builder) =>
		{
			var dataBaseConnectionSettings = provider.GetRequiredService<DataBaseConnectionSettings>();
			builder.UseSqlite(dataBaseConnectionSettings.ReadModelsDbConnectionString);

			builder.AddInterceptors(provider.GetRequiredService<TraceDbConnectionInterceptor>());
			builder.AddInterceptors(provider.GetRequiredService<RelaxSqliteDbConnectionInterceptor>());
		});

		services.AddScoped<IReadModelsUnitOfWork, ReadModelsUnitOfWork>();
		services.AddScoped<ILeaderBoardRepository, LeaderBoardRepository>();
	}

	public static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
	{
		{ // DB
			DataBaseConnectionSettings dataBaseConnectionSettings = new();

			configuration.Bind(
				nameof(DataBaseConnectionSettings),
				dataBaseConnectionSettings);

			services.AddSingleton(dataBaseConnectionSettings);
		}


		return services;
	}
}