using LiquidProjections.Statistics;
using Nancy.Owin;
using Owin;

namespace LiquidProjections.Owin
{
    public static class MiddlewareExtensions
    {
        public static IAppBuilder UseStatsHttpApi(this IAppBuilder appBuilder, ProjectionStats stats)
        {
            appBuilder.Map("/projectionStats", a => a.UseNancy(new NancyOptions
            {
                Bootstrapper = new CustomNancyBootstrapper(stats)
            }));

            return appBuilder;
        }
    }
}