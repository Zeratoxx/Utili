using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Text;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;
using static Utili.Json;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp;

namespace Utili
{
    class Rosyln
    {
        
    }

    public class RosylnCommands : ModuleBase<SocketCommandContext>
    {
        [Command("Execute"), Alias("Evaluate")]
        public async Task Execute([Remainder] string Code)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                Microsoft.CodeAnalysis.MetadataReference[] References = {
                    Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/Discord.Net.WebSocket.dll")),
                    Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/Discord.Net.Core.dll")),
                    Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/System.Linq.dll"))
                    };

                string[] Imports = { "Discord", "Discord.Commands", "Discord.WebSocket", "System.Linq", "System.Threading.Tasks" };

                var globals = new RosylnGlobals { Context = Context, Client = Program.GlobalClient };
                try
                {
                    var Evaluation = await CSharpScript.EvaluateAsync<object>($"{Code}", ScriptOptions.Default.WithReferences(References).WithImports(Imports), globals: globals);
                    if(Evaluation == null) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Executed"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Executed", Evaluation.ToString()));
                }
                catch (CompilationErrorException e)
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Execution failed", e.Message));
                }
            }
        }

        // This command makes Utili run code using one of my private bots.

        [Command("oExecute"), Alias("oEvaluate")]
        public async Task ExecuteOther(string Name, [Remainder] string Code)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                string Token = "";

                switch (Name.ToLower())
                {
                    case "filebot":
                        Token = Config.OtherBotTokens.FileBot;
                        break;
                    case "hubbot":
                        Token = Config.OtherBotTokens.HubBot;
                        break;
                    case "thoriumcube":
                        Token = Config.OtherBotTokens.ThoriumCube;
                        break;
                    case "unwyre":
                        Token = Config.OtherBotTokens.Unwyre;
                        break;
                    case "pingplus":
                        Token = Config.OtherBotTokens.PingPlus;
                        break;
                    case "imgonly":
                        Token = Config.OtherBotTokens.ImgOnly;
                        break;
                    case "shards":
                        Token = Config.OtherBotTokens.Shards;
                        break;
                    default:
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid bot"));
                        return;
                }

                DiscordSocketClient TempClient;

                TempClient = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Critical,
                    MessageCacheSize = 100
                });
                TempClient.Log += Rosyln_Log;

                await TempClient.LoginAsync(TokenType.Bot, Token);
                await TempClient.StartAsync();
                
                await Task.Delay(2000);

                Microsoft.CodeAnalysis.MetadataReference[] References = { 
                    Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/Discord.Net.WebSocket.dll")), 
                    Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/Discord.Net.Core.dll")), 
                    Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "Deps/System.Linq.dll"))
                    };

                string[] Imports = { "Discord", "Discord.Commands", "Discord.WebSocket", "System.Linq", "System.Threading.Tasks" };
                var globals = new RosylnGlobals { Context = null, Client = TempClient };
                try
                {
                    var Evaluation = await CSharpScript.EvaluateAsync<object>($"{Code}", ScriptOptions.Default.WithReferences(References).WithImports(Imports), globals: globals);
                    if (Evaluation == null) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Executed"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Executed", Evaluation.ToString()));
                }
                catch (CompilationErrorException e)
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Execution failed", e.Message));
                }

                await TempClient.LogoutAsync();
                TempClient.Dispose();
            }
        }

        public async Task Rosyln_Log(LogMessage Message)
        {
            if (Message.Source.ToString() != "Rest")
            {
                Console.WriteLine($"[{DateTime.Now}] [Rosyln] [{Message.Source}] {Message.Message}");
            }
        }
    }

    public class RosylnGlobals
    {
        public SocketCommandContext Context;
        public DiscordSocketClient Client;
    }
    
}
