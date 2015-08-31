using SSAP.ScratchHelper.Diagnostics;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace SSAP.ScratchHelper
{
	public class Startup
	{
		// This code configures Web API. The Startup class is specified as a type
		// parameter in the WebApp.Start method.
		public void Configuration( IAppBuilder appBuilder )
		{
			// Configure Web API for self-host. 
			HttpConfiguration config = new HttpConfiguration();
			//config.Routes.MapHttpRoute(
			//	name: "DefaultApi",
			//	routeTemplate: "api/{controller}/{id}/{ix}",
			//	routeTemplate: "{id}",
			//	defaults: new { id = RouteParameter.Optional }
			//);
			config.MapHttpAttributeRoutes();
			
			// Include this line to log all http messages
		   // config.MessageHandlers.Add( new MessageLoggingHandler() );
			
			appBuilder.UseWebApi( config );
		}
	}
}
