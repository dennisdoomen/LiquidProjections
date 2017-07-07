using System.Collections.Generic;
using LiquidProjections.Statistics;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Swagger.Modules;
using Nancy.Swagger.Services;
using Nancy.TinyIoc;
using Swagger.ObjectModel;

namespace LiquidProjections.Owin
{
    internal class CustomNancyBootstrapper : DefaultNancyBootstrapper
    {
        private readonly ProjectionStats stats;

        public CustomNancyBootstrapper(ProjectionStats stats)
        {
            this.stats = stats;
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            // your customization goes here
            SwaggerMetadataProvider.SetInfo(
                "Nancy Swagger Example", //Name
                "v0", //Version
                "Our awesome service", //Description
                new Contact() {EmailAddress = "exampleEmail@example.com"}, //Contact Info
                "Tier1" //Service Level
            );

            container.Register(stats);

            base.ApplicationStartup(container, pipelines);
        }

        protected override IEnumerable<ModuleRegistration> Modules =>
            new[]
            {
                new ModuleRegistration(typeof(StatisticsModule)),
                new ModuleRegistration(typeof(SwaggerModule)), 
            };


        /// <summary>
        /// Initialise the request - can be used for adding pre/post hooks and
        ///             any other per-request initialisation tasks that aren't specifically container setup
        ///             related
        /// </summary>
        /// <param name="container">Container</param><param name="pipelines">Current pipelines</param><param name="context">Current context</param>
        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            pipelines.AfterRequest.AddItemToEndOfPipeline(
                x => x.Response.Headers.Add("Access-Control-Allow-Origin", "*"));
        }
    }
}