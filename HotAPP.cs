using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using System.Reflection.PortableExecutable;
using Photino.NET;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace Hot;

public class HotAPP<TComponent> : HotAPIServer where TComponent : IComponent {
    [STAThread]

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
        if (Config["HotAPI:App:UseAuthentication"]!.ToBool()) {
            appBuilder.Services.AddAuthentication();
            appBuilder.Services.AddCascadingAuthenticationState();
        }
        if (Config["HotAPI:App:UseAuthorization"]!.ToBool())
            appBuilder.Services.AddAuthorization();


        appBuilder.RootComponents.Add<TComponent>("app");

        Config_Services(appBuilder.Services);

        var app = appBuilder.Build();

        // customize window
        app.MainWindow
            //            .SetIconFile("favicon.ico")
            .SetTitle(Title);

        app.MainWindow.FullScreen = Config["HOTAPP:FullScreen"]!.ToBool();

        AppDomain.CurrentDomain.UnhandledException += (sender, error) => {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        app.MainWindow.RegisterWebMessageReceivedHandler((sender,message)=> {
            var window = (PhotinoWindow)sender!;
            if (message == "close-window") {
                Log.LogInformation($"Closing \"{window.Title}\".");
                window.Close();
            }
        });

        app.Run();
    }


    public virtual Action<RazorPagesOptions> Config_RazorPageOptions() {
        return ((opt) => { });
    }


    public override void Config_Builder(WebApplicationBuilder builder) {
        base.Config_Builder(builder);

        //builder.Services.AddMvc().AddRazorPagesOptions(o => {
        //    o.Conventions.AddPageRoute("/App", "");
        //});
        
        builder.Services
            .AddRazorPages(Config_RazorPageOptions())
            .AddRazorRuntimeCompilation(opt => {
                opt.FileProviders.Add(new EmbeddedFileProvider(Config.GetAsmResource));
            });
        builder.Services.AddServerSideBlazor();
        //        builder.Services.AddRazorComponents();


        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<HttpContextAccessor>();
//        blazor.Shared
//        =============
//      public class HttpContextAccessor {
//        private readonly IHttpContextAccessor _httpContextAccessor;
//
//        public HttpContextAccessor(IHttpContextAccessor httpContextAccessor) {
//            _httpContextAccessor = httpContextAccessor;
//        }
//
//        public HttpContext Context => _httpContextAccessor.HttpContext;
//      }
//
//
//      blazor.Client to App.cshtml
//      ===========================
//    @inject blazor.Shared.HttpContextAccessor HttpContext
//    <Router AppAssembly=typeof(Program).Assembly />
//
//    @functions 
//    {      
//      protected override void OnInit() {
//        HttpContext.Context.Request.Cookies.* *
//
//        // Or data passed through middleware in blazor.Server
//        HttpContext.Context.Features.Get<T>()
//    }
//}



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
