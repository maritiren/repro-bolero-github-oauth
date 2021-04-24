namespace ReproGithubOAuth.Server

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OAuth
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Bolero
open Bolero.Remoting.Server
open Bolero.Server.RazorHost
open ReproGithubOAuth
open Bolero.Templating.Server

type Startup() =

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddMvc().AddRazorRuntimeCompilation() |> ignore
        services.AddServerSideBlazor() |> ignore
        services
            .AddAuthorization()
            .AddAuthentication(fun options ->
                options.DefaultAuthenticateScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                options.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                options.DefaultChallengeScheme <- "GitHub"
            )
                .AddCookie(fun config ->
                    config.Cookie.SameSite <- SameSiteMode.None
                    config.Cookie.SecurePolicy <- CookieSecurePolicy.Always
                )
                .AddGitHub(fun options ->
                    options.ClientId <- "GitHub ClientId";
                    options.ClientSecret <- "GitHub Client Secret"; 
                    options.CallbackPath <- new PathString("/github-oauth");
                    options.AuthorizationEndpoint <- "https://github.com/login/oauth/authorize";
                    options.TokenEndpoint <- "https://github.com/login/oauth/access_token";
                    options.UserInformationEndpoint <- "https://api.github.com/user";
                )
                .Services
            .AddRemoting<BookService>()
            .AddBoleroHost()
#if DEBUG
            .AddHotReload(templateDir = __SOURCE_DIRECTORY__ + "/../ReproGithubOAuth.Client")
#endif
        |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        app
            .UseAuthentication()
            .UseRemoting()
            .UseStaticFiles()
            .UseRouting()
            .UseBlazorFrameworkFiles()
            .UseEndpoints(fun endpoints ->
#if DEBUG
                endpoints.UseHotReload()
#endif
                endpoints.MapBlazorHub() |> ignore
                endpoints.MapFallbackToPage("/_Host") |> ignore)
        |> ignore

module Program =

    [<EntryPoint>]
    let main args =
        WebHost
            .CreateDefaultBuilder(args)
            .UseStaticWebAssets()
            .UseStartup<Startup>()
            .Build()
            .Run()
        0
