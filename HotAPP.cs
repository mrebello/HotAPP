using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Hot;

public class HotAPP<TComponent> : HotAPIServer where TComponent : IComponent {




    public override Task StartAsync(CancellationToken cancellationToken) {
        //base.StartAsync(cancellationToken);

        // Pega nome da aplicação e garante que configurações foram inicializadas
        var appname = Config[ConfigConstants.ServiceName] ?? Config[ConfigConstants.AppName]!;

        // Se rodando como serviço ou com -d na linha de comando, roda como server
        if ((OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService()) ||
            (OperatingSystem.IsLinux() && Microsoft.Extensions.Hosting.Systemd.SystemdHelpers.IsSystemdService()) ||
            Config.IsDaemon) {







              return base.StartAsync(cancellationToken);
        } else {   // Senão, roda como app desktop
            StartPhotino(appname);
            base.StopAsync(cancellationToken);
            Environment.Exit(0);

            return Task.CompletedTask;
        }
    }


    public static void StartPhotino(string Title) {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault();

        appBuilder.Services.AddSingleton<IConfiguration>(HotConfiguration.configuration);
        appBuilder.Services.AddLogging(HotLog.LoggingCreate);

        appBuilder.RootComponents.Add<TComponent>("app");

        var app = appBuilder.Build();

        // customize window
        app.MainWindow
            //            .SetIconFile("favicon.ico")
            .SetTitle(Title);

        AppDomain.CurrentDomain.UnhandledException += (sender, error) => {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        app.Run();
    }

    //public override WebApplicationOptions? WebApplicationOptions() {
    //    return new WebApplicationOptions() {
    //        ContentRootPath = "C:\\"
    //    };
    //}


    public override void Config_Builder(WebApplicationBuilder builder) {
        base.Config_Builder(builder);

        //builder.Services.AddMvc().AddRazorPagesOptions(o => {
        //    o.Conventions.AddPageRoute("/App", "");
        //});

#if NET8_0_OR_GREATER
        builder.Services.AddRazorPages().AddRazorRuntimeCompilation(opt => {
            opt.FileProviders.Add(new EmbeddedFileProvider(Config.GetAsmResource));
        });
#endif

        builder.Services.AddServerSideBlazor();
    }

    public override void Config_App(WebApplication app) {
        base.Config_App(app);

        app.UseRouting();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

    }



}
