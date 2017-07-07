using System.Threading.Tasks;
using LiquidProjections.Owin.Support;
using LiquidProjections.Statistics;
using Nancy.Owin;
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>,
    System.Threading.Tasks.Task>;
using MidFunc = System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>,
    System.Threading.Tasks.Task>, System.Func<System.Collections.Generic.IDictionary<string, object>,
    System.Threading.Tasks.Task>>;

namespace LiquidProjections.Owin
{
    public static class Middleware
    {
        public static MidFunc UseLiquidProjections(ProjectionStats stats)
        {
            return next => async env =>
            {
                MidFunc nancyMidFunc = NancyMiddleware.UseNancy(new NancyOptions
                {
                    Bootstrapper = new CustomNancyBootstrapper(stats)
                });

                var map = new MapMiddleware(next, new MapOptions
                {
                    PathMatch = new PathString("/projectionStats"),
                    Branch = nancyMidFunc(_ => Task.FromResult(0))
                });

                await map.Invoke(env);
            };
        }
    }
}