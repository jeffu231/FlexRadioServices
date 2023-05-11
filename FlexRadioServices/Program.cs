using CoreServices;
using Flex.Smoothlake.FlexLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;

static void Main(string[] args)
{
    API.IsGUI = false;
    API.ProgramName = "FlexRadioService";
    API.Init();
    
    var builder = WebApplication.CreateBuilder(args);
    var app = builder.Build();

    builder.Services.AddControllers(o =>
    {
        o.RespectBrowserAcceptHeader = true;
        o.ReturnHttpNotAcceptable = true;
    }).AddNewtonsoftJson().AddXmlSerializerFormatters();

    builder.Services.AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("x-api-version"));
    });

// Add ApiExplorer to discover versions
    builder.Services.AddVersionedApiExplorer(setup =>
    {
        setup.GroupNameFormat = "'v'VVV";
        setup.SubstituteApiVersionInUrl = true;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen();

    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

    var app = builder.Build();

    var swaggerBasePath = "api/ars";

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

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
} 

