using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using static Utili.Json;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class Rosyln
    {
    }

    public class RosylnCommands : ModuleBase<SocketCommandContext>
    {
        [Command("Execute"), Alias("Evaluate")]
        public async Task Execute([Remainder] string code)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                MetadataReference[] references = {
                    MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/Discord.Net.WebSocket.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/Discord.Net.Core.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/System.Linq.dll"))
                    };

                string[] imports = { "Discord", "Discord.Commands", "Discord.WebSocket", "System.Linq", "System.Threading.Tasks" };

                RosylnGlobals globals = new RosylnGlobals { Context = Context, Client = Program.Client };
                try
                {
                    object evaluation = await CSharpScript.EvaluateAsync<object>($"{code}", ScriptOptions.Default.WithReferences(references).WithImports(imports), globals: globals);
                    if (evaluation == null) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Executed"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Executed", evaluation.ToString()));
                }
                catch (CompilationErrorException e)
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Execution failed", e.Message));
                }
            }
        }

        // This command makes Utili run code using one of my private bots.

        [Command("oExecute"), Alias("oEvaluate")]
        public async Task ExecuteOther(string name, [Remainder] string code)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                string token = "";

                switch (name.ToLower())
                {
                    case "filebot":
                        token = Config.OtherBotTokens.FileBot;
                        break;

                    case "hubbot":
                        token = Config.OtherBotTokens.HubBot;
                        break;

                    case "thoriumcube":
                        token = Config.OtherBotTokens.ThoriumCube;
                        break;

                    case "unwyre":
                        token = Config.OtherBotTokens.Unwyre;
                        break;

                    case "pingplus":
                        token = Config.OtherBotTokens.PingPlus;
                        break;

                    case "imgonly":
                        token = Config.OtherBotTokens.ImgOnly;
                        break;

                    case "shards":
                        token = Config.OtherBotTokens.Shards;
                        break;

                    default:
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid bot"));
                        return;
                }

                DiscordSocketClient tempClient;

                tempClient = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Critical,
                    MessageCacheSize = 100
                });
                tempClient.Log += Rosyln_Log;

                await tempClient.LoginAsync(TokenType.Bot, token);
                await tempClient.StartAsync();

                await Task.Delay(2000);

                MetadataReference[] references = {
                    MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/Discord.Net.WebSocket.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/Discord.Net.Core.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/System.Linq.dll"))
                    };

                string[] imports = { "Discord", "Discord.Commands", "Discord.WebSocket", "System.Linq", "System.Threading.Tasks" };
                RosylnGlobals globals = new RosylnGlobals { Context = null, Client = tempClient };
                try
                {
                    object evaluation = await CSharpScript.EvaluateAsync<object>($"{code}", ScriptOptions.Default.WithReferences(references).WithImports(imports), globals: globals);
                    if (evaluation == null) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Executed"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Executed", evaluation.ToString()));
                }
                catch (CompilationErrorException e)
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Execution failed", e.Message));
                }

                await tempClient.LogoutAsync();
                tempClient.Dispose();
            }
        }

        public async Task Rosyln_Log(LogMessage message)
        {
            if (message.Source != "Rest")
            {
                Console.WriteLine($"[{DateTime.Now}] [Rosyln] [{message.Source}] {message.Message}");
            }
        }
    }

    public class RosylnGlobals
    {
        public SocketCommandContext Context;
        public DiscordSocketClient Client;
    }
}