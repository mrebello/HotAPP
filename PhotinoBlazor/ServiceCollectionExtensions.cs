using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using PhotinoNET;

namespace Photino.Blazor
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorDesktop(this IServiceCollection services)
        {
            services
                .AddOptions<PhotinoBlazorAppConfiguration>()
                .Configure(opts =>
                {
                    opts.AppBaseUri = new Uri(PhotinoWebViewManager.AppBaseUri);
                    opts.HostPage = "index.html";
                });

            return services
                .AddScoped(sp =>
                {
                    var handler = sp.GetService<PhotinoHttpHandler>();
                    return new HttpClient(handler) { BaseAddress = new Uri(PhotinoWebViewManager.AppBaseUri) };
                })
                .AddSingleton(sp =>
                {
                    var manager = sp.GetService<PhotinoWebViewManager>();
                    var store = sp.GetService<JSComponentConfigurationStore>();

                    return new BlazorWindowRootComponents(manager, store);
                })
                .AddSingleton<Dispatcher, PhotinoDispatcher>()
                .AddSingleton<IFileProvider>(_ =>
                {
                    var stackFrame = new System.Diagnostics.StackTrace(1);
                    var asm_resource = stackFrame.GetFrame(stackFrame.FrameCount - 1)?.GetMethod()?.ReflectedType?.Assembly
                    ?? System.Reflection.Assembly.GetExecutingAssembly();

                    var root = asm_resource.GetName().Name + ".wwwroot";
                    return new EmbeddedFileProvider(asm_resource,root);

                    //return new PhysicalFileProvider(root);
                })
                .AddSingleton<JSComponentConfigurationStore>()
                .AddSingleton<PhotinoBlazorApp>()
                .AddSingleton<PhotinoHttpHandler>()
                .AddSingleton<PhotinoSynchronizationContext>()
                .AddSingleton<PhotinoWebViewManager>()
                .AddSingleton(new PhotinoWindow())
                .AddBlazorWebView();
        }
    }
}
