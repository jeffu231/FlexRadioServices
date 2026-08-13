using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FlexRadioServices.Models.Ports.Network;
using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using FlexRadioServices.Services.FlexLib;
using FlexRadioServices.Utils;

namespace FlexRadioServices
{
    public partial class Program
    {
        //private static readonly AppSettings Settings = new AppSettings();
        static void Main(string[] args)
        {
            
            var builder = WebApplication.CreateBuilder(args);

            ConfigureServices(builder);

            ConfigureApiVersioning(builder);
            
            ConfigureSwagger(builder);

            var app = builder.Build();

            EnableSwagger(app);

            app.UseAuthorization();

            app.UseExceptionHandler();
            
            app.UseStatusCodePages();

            app.MapControllers();
            app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => false
            });
            app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready")
            });
            
            app.Run();
        }
        
        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            
            builder.Configuration.AddJsonFile("./appsettings/appsettings.user.json", optional: true, reloadOnChange: false);
            var services = builder.Services;
            services.AddRuntimeConfiguration(builder.Configuration);
            services.AddHostedService<RuntimeConfigurationDiagnosticsService>();
            services.AddHealthChecks()
                .AddCheck<FlexLibReadinessHealthCheck>("flexlib", tags: ["ready"]);
            
            services.AddSingleton<IFlexLibApi, FlexLibApiAdapter>();
            services.AddSingleton<FlexRadioService>();
            services.AddSingleton<IFlexRadioService>(serviceProvider =>
                serviceProvider.GetRequiredService<FlexRadioService>());
            services.AddSingleton<IConnectedRadioCoordinator>(serviceProvider =>
                serviceProvider.GetRequiredService<FlexRadioService>());
            services.AddSingleton<IReadinessState, ReadinessState>();
            services.AddHostedService<FlexLibLifecycleService>();
            services.AddSingleton<ISliceCommandService, SliceCommandService>();
            services.AddTransient<ITcpServerClient, TcpServerClient>();
            services.AddTransient<ITcpServer, TcpServer>();
            
            var mqttBrokerSettings = builder.Configuration
                .GetSection(MqttBrokerSettings.SectionName)
                .Get<MqttBrokerSettings>();
            
            if (mqttBrokerSettings != null && !string.IsNullOrWhiteSpace(mqttBrokerSettings.BrokerHost))
            {
                services.AddMqttClientHostedService(mqttBrokerSettings);
                services.AddHostedService<MqttRadioInfoPublisher>();
                services.AddHealthChecks().AddCheck<MqttHealthCheck>("mqtt", tags: ["ready"]);
            }
            services.AddHostedService<RadioManagerService>();

            var portSettings = builder.Configuration
                .GetSection(CatPortSettings.SectionName)
                .Get<CatPortSettings>()?
                .PortSettings;
            if (portSettings != null)
            {
                foreach (var portSetting in portSettings)
                {
                    services.AddSingleton<IHostedService>(x => new FlexCatPortService(portSetting, 
                        x.GetRequiredService<ITcpServer>(),
                        x.GetRequiredService<ILogger<FlexCatPortService>>(), 
                        x.GetRequiredService<IConnectedRadioCoordinator>()));
                }
            }
            
            services.AddProblemDetails();
            
            services.AddControllers(o =>
            {
                o.RespectBrowserAcceptHeader = true;
                o.ReturnHttpNotAcceptable = true;
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        }
        
        private static void ConfigureApiVersioning(WebApplicationBuilder builder)
        {
            // Add ApiExplorer to discover versions
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("x-api-version"));
            })
            .AddApiExplorer(options =>
            {
                // Configure options for the API explorer
                options.GroupNameFormat = "'v'VVV"; // Formats the group name for Swagger, e.g., "v1" or "v1.1"
                options.SubstituteApiVersionInUrl = true; // Automatically replaces {version} in routes
            });
        }

        private static void ConfigureSwagger(WebApplicationBuilder builder)
        {
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });


            builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
        }

        private static void EnableSwagger(WebApplication app)
        {
            var swaggerBasePath = "api/frs";

            app.UseSwagger(options =>
            {
                options.RouteTemplate = swaggerBasePath + "/swagger/{documentName}/swagger.{json|yaml}";
            });
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = $"{swaggerBasePath}/swagger";
                var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
                    options.SwaggerEndpoint($"{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
            });
        }
    }

}
