namespace ReproGithubOAuth.Server

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Hosting
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open ReproGithubOAuth

type BookService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Main.BookService>()

    let books =
        let json = Path.Combine(env.ContentRootPath, "data/books.json") |> File.ReadAllText
        JsonSerializer.Deserialize<Client.Main.Book[]>(json)
        |> ResizeArray

    override this.Handler =
        {
            getBooks = ctx.Authorize <| fun () -> async {
                return books.ToArray()
            }

            addBook = ctx.Authorize <| fun book -> async {
                books.Add(book)
            }

            removeBookByIsbn = ctx.Authorize <| fun isbn -> async {
                books.RemoveAll(fun b -> b.isbn = isbn) |> ignore
            }

            signIn = fun () -> async {
                let! res = Async.AwaitTask (ctx.HttpContext.ChallengeAsync "GitHub")
                printfn $"res: {res}"
                // Do some parsing of request
                // Never gets this far, so returning whatever
                return option.Some "myusername"
            }

            signOut = fun () -> async {
                return! ctx.HttpContext.AsyncSignOut()
            }

            getUsername = ctx.Authorize <| fun () -> async {
                return ctx.HttpContext.User.Identity.Name
            }
        }
