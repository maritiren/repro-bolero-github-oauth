namespace ReproGithubOAuth.Server

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Cors
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
        let configureCors (builder: Infrastructure.CorsPolicyBuilder) = 
            builder.WithOrigins("http://localhost:5000")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                |> ignore
        services.AddMvc().AddRazorRuntimeCompilation() |> ignore
        services.AddServerSideBlazor() |> ignore
        services.AddCors(fun options ->
            options.AddPolicy("_allowSpecificOrigins", configureCors)
        ) |> ignore
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
                    options.ClientId <- "GitHub ClientId should be here";
                    options.ClientSecret <- "GitHub Client Secret should be here"; 
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
        // Putting UseCors() after UseRouting() according to 
        // https://stackoverflow.com/a/65937838/12094643
        app
            .UseRouting()
            .UseCors("_allowSpecificOrigins")
            .UseAuthentication()
            .UseRemoting()
            .UseStaticFiles()
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
