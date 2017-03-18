using System;
using System.Threading;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using code.MissingDIExtensions;
using code.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MissingDIExtensions;
using StructureMap;
using Ninject;
using Ninject.Infrastructure.Disposal;

namespace code
{
	public class Startup
	{
		public IConfigurationRoot Configuration { get; }

		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		#region AspNet Core DI

		private IServiceProvider ConfigAspNetDI(IServiceCollection services)
		{
			services.AddSingleton<IRequestIdFactory, RequestIdFactory>();
			services.AddScoped<IRequestId, RequestId>();

			return services.BuildServiceProvider();
		}

		#endregion

		#region Autofac

		public Autofac.IContainer ApplicationContainer { get; private set; }

		private IServiceProvider ConfigAutofacDI(IServiceCollection services)
		{
			var builder = new Autofac.ContainerBuilder();

			builder.RegisterType<RequestIdFactory>().As<IRequestIdFactory>().SingleInstance();
			builder.RegisterType<RequestId>().As<IRequestId>();

			builder.Populate(services);
			this.ApplicationContainer = builder.Build();

			return new AutofacServiceProvider(this.ApplicationContainer);
		}

		#endregion

		#region StructureMap

		private IServiceProvider ConfigStructureMapDI(IServiceCollection services)
		{
			var container = new StructureMap.Container();

			container.Configure(config =>
			{
				config.For<IRequestIdFactory>().Use<RequestIdFactory>().Singleton();
				config.For<IRequestId>().Use<RequestId>().ContainerScoped();

				config.Populate(services);
			});

			return container.GetInstance<IServiceProvider>();
		}

		#endregion

		#region Ninject

		private sealed class Scope : DisposableObject { }

		private readonly AsyncLocal<Scope> scopeProvider = new AsyncLocal<Scope>();
		private IReadOnlyKernel kernel;
		private object Resolve(Type type) => kernel.Get(type);
		private Scope RequestScope(Ninject.Activation.IContext context) => scopeProvider.Value;

		private IReadOnlyKernel RegisterApplicationComponents(IApplicationBuilder app, ILoggerFactory loggerFactory)
		{
			IKernelConfiguration config = new KernelConfiguration();

			// Register application services
			config.Bind(app.GetControllerTypes()).ToSelf().InScope(RequestScope);

			config.Bind<IRequestIdFactory>().To<RequestIdFactory>().InSingletonScope();
			config.Bind<IRequestId>().To<RequestId>().InScope(RequestScope);

			// Cross-wire required framework services
			config.BindToMethod(app.GetRequestService<IViewBufferScope>);
			config.Bind<ILoggerFactory>().ToConstant(loggerFactory);

			return config.BuildReadonlyKernel();
		}

		public IServiceProvider ConfigNinjectDI(IServiceCollection services)
		{
			// Add framework services.
			services.AddMvc();

			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

			services.AddRequestScopingMiddleware(() => scopeProvider.Value = new Scope());
			services.AddCustomControllerActivation(Resolve);
			services.AddCustomViewComponentActivation(Resolve);

			return services.BuildServiceProvider();
		}

		#endregion

		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			// Add framework services.
			services.AddMvc();

			// setup up service mappings
			return ConfigAspNetDI(services);
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			// for ninject only
			kernel = RegisterApplicationComponents(app, loggerFactory);

			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseBrowserLink();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}

			app.UseStaticFiles();

			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
