using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using System.Reflection.PortableExecutable;

namespace Hot;

public class HotAPP<TComponent> : HotAPIServer where TComponent : IComponent {

    /// <summary>
    /// Used to configure services for then APP in both Server (kestrel) and Desktop (Photino).
    /// </summary>
    /// <param name="services"></param>
    public virtual void Config_Services(IServiceCollection services) {
    }

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


    public void StartPhotino(string Title) {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault();

        appBuilder.Services.AddSingleton<IConfiguration>(HotConfiguration.configuration);
        appBuilder.Services.AddLogging(HotLog.LoggingCreate);

        appBuilder.RootComponents.Add<TComponent>("app");

        Config_Services(appBuilder.Services);

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


    public override void Config_Builder(WebApplicationBuilder builder) {
        base.Config_Builder(builder);

        //builder.Services.AddMvc().AddRazorPagesOptions(o => {
        //    o.Conventions.AddPageRoute("/App", "");
        //});

        builder.Services.AddRazorPages().AddRazorRuntimeCompilation(opt => {
            opt.FileProviders.Add(new EmbeddedFileProvider(Config.GetAsmResource));
        });
        builder.Services.AddServerSideBlazor();
        //        builder.Services.AddRazorComponents();

        Config_Services(builder.Services);
    }

    public override void Config_App(WebApplication app) {
        base.Config_App(app);

        //        app.MapRazorComponents<TComponent>();
        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
    }

}
