using LiquidProjections.Statistics;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Modules;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace LiquidProjections.Owin
{
    public class StatisticsModule : NancyModule
    {
        private readonly ProjectionStats stats;

        public StatisticsModule(ProjectionStats stats)
        {
            this.stats = stats;
        }

        public StatisticsModule()
        {
            Get("/hello", args => "Hello World", null, "HelloWorld");
        }
    }

    public class StatisticsMetadataModule : SwaggerMetadataModule
    {
        public StatisticsMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
            RouteDescriber.AddBaseTag(new Tag()
            {
                Description = "Operations for handling the service",
                Name = "Service"
            });

            RouteDescriber.DescribeRoute<string>("HelloWorld", "", "Say Hello", new[]
            {
                new HttpResponseMetadata {Code = 200, Message = "OK"}
            });
        }
    }

}