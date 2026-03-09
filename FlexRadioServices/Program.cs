using System.Reflection;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Models.Ports.Network;
using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using FlexRadioServices.Utils;

namespace FlexRadioServices
{
    public static class Program
    {
        //private static readonly AppSettings Settings = new AppSettings();
        static void Main(string[] args)
        {
            
            API.IsGUI = false;
            API.ProgramName = "FlexRadioService";
            API.Init();
            
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
            
            app.Run();
        }
        
        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            
            builder.Configuration.AddJsonFile("./appsettings/appsettings.user.json", optional:true, reloadOnChange: true);
            var services = builder.Services;
            builder.Services.Configure<RadioSettings>(builder.Configuration.GetSection("RadioSettings"));
            builder.Services.Configure<MqttBrokerSettings>(builder.Configuration.GetSection("MqttBrokerSettings"));
            builder.Services.Configure<CatPortSettings>(builder.Configuration.GetSection("CatPorts"));
            
            services.AddSingleton<IFlexRadioService, FlexRadioService>();
            services.AddTransient<ITcpServerClient, TcpServerClient>();
            services.AddTransient<ITcpServer, TcpServer>();
            
            var mqttBrokerSettings = builder.Configuration
                .GetSection("MqttBrokerSettings")
                .Get<MqttBrokerSettings>();
            
            if (mqttBrokerSettings != null && !string.IsNullOrEmpty(mqttBrokerSettings.BrokerHost))
            {
                services.AddMqttClientHostedService(mqttBrokerSettings);
                services.AddHostedService<MqttRadioInfoPublisher>();
            }
            services.AddHostedService<RadioManagerService>();

            var portSettings = builder.Configuration
                .GetSection("CatPorts")
                .Get<CatPortSettings>()?
                .PortSettings;
            if (portSettings != null)
            {
                foreach (var portSetting in portSettings)
                {
                    services.AddSingleton<IHostedService>(x => new FlexCatPortService(portSetting, 
                        x.GetRequiredService<ITcpServer>(),
                        x.GetRequiredService<ILogger<FlexCatPortService>>(), 
                        x.GetRequiredService<IFlexRadioService>()));
                }
            }
            
            services.AddProblemDetails();
            
            services.AddControllers(o =>
            {
                o.RespectBrowserAcceptHeader = true;
                o.ReturnHttpNotAcceptable = true;
            }).AddNewtonsoftJson();

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
