using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Api
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override IEnumerable<Type> ViewEngines => new Type[0];

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            if (context.Request.Headers.Accept.All(x => x.Item1 != "application/json"))
            {
                context.Request.Headers.Accept = new[] { new Tuple<string, decimal>("application/json", 1m) }.Concat(context.Request.Headers.Accept);
            }
        }
    }
}