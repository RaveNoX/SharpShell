﻿#region Usings

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using ColoredConsole;
using ConsoleShell;
using NDesk.Options;
using Newtonsoft.Json;

#endregion

namespace DualShell
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var historyFile = "history.txt";
            var shell = new Shell();
            var interactive = !args.Any();

            RegisterCommands(shell, interactive);

            if (interactive)
            {
                shell.WritePrompt += (sender, eventArgs) =>
                    ColorConsole.Write("[ ".Green(), DateTime.Now.ToLongTimeString(), " ]-> ".Green());

                shell.ShellCommandNotFound += (sender, eventArgs) =>
                    ColorConsole.WriteLine($"Command not found: ".Red(), eventArgs.Input.White());

                shell.PrintAlternatives += (sender, eventArgs) =>
                {
                    ColorConsole.WriteLine("Possible commands: ".Cyan());
                    foreach (var alternative in eventArgs.Alternatives)
                    {
                        ColorConsole.WriteLine("- ", alternative.White());
                    }
                };


                if (File.Exists(historyFile))
                {
                    shell.History.Load(historyFile);
                }

                try
                {
                    shell.RunShell();
                }
                catch (ApplicationExitException)
                {
                }

                shell.History.Save(historyFile);
            }
            else
            {
                shell.ExecuteCommand(args);
            }
        }

        private static void RegisterCommands(Shell shell, bool interactive)
        {
            if (interactive)
            {
                shell.AddLambdaCommand("exit", "Exit from program", Exit);
                shell.AddLambdaCommand("quit", "Exit from program", Exit);
            }

            shell.AddLambdaCommand("help", "Prints this help", (command, args) => InvokeHelp(shell, command, args));
            shell.AddLambdaCommand("options", "Test options", InvokeTestOptions);

            shell.AddLambdaCommand("sip list", "list sip peers", InvokeFakeCommand);
            shell.AddLambdaCommand("sip add", "add sip peer", InvokeFakeCommand);
            shell.AddLambdaCommand("sip delete", "delete sip peer", InvokeFakeCommand);
            shell.AddLambdaCommand("sip acl list", InvokeFakeCommand);
            shell.AddLambdaCommand("sip acl add", InvokeFakeCommand);
            shell.AddLambdaCommand("sip acl delete", InvokeFakeCommand);
            shell.AddLambdaCommand("sip acl stick", InvokeFakeCommand);
            shell.AddLambdaCommand("sip acl flush", InvokeFakeCommand);

            shell.AddLambdaCommand("list", InvokeFakeCommand, (command, tokens) =>
            {
                var items = new[] {"users", "peers"};

                if (tokens.Length == 0)
                {
                    return items;
                }

                return tokens.Length == 1 ? items.Where(x => x.StartsWith(tokens[0])).ToArray() : null;
            });
        }

        private static void Exit(ShellCommand shellCommand, string[] strings)
        {
            throw new ApplicationExitException();
        }

        private static void InvokeTestOptions(ShellCommand shellCommand, string[] args)
        {
            var prefixes = new[] {"--", "-", "/"};

            var optionsArgs = args
                .TakeWhile(x => prefixes.Any(p => x.StartsWith(p) && x.Length > p.Length))
                .ToList();

            var parser = new OptionSet();

            var options = (dynamic) new ExpandoObject();
            options.Verbosity = 0;

            parser
                .Add("c|config=", "Set config path", s => { if (!string.IsNullOrWhiteSpace(s)) options.ConfigPath = s; })
                .Add("v|verbose", "increase verbosity level", s => { if (s != null) options.Verbosity++; });


            var arguments = new List<string>(parser.Parse(optionsArgs));
            arguments.AddRange(args.Skip(optionsArgs.Count));

            options.Arguments = arguments;

            Console.WriteLine("Options:");
            Console.WriteLine(JsonConvert.SerializeObject(options, Formatting.Indented));
        }

        private static void InvokeHelp(Shell shell, ShellCommand shellCommand, string[] args)
        {
            var items = shell.GetCommandsDescriptions(string.Join(" ", args));

            var padSize = items.Max(x => x.Key.Length) + 4;

            ColorConsole.WriteLine("Commands:".Cyan());
            foreach (var item in items)
            {
                ColorConsole.WriteLine("- ", item.Key.PadRight(padSize).White(), item.Value);
            }
        }

        private static void InvokeFakeCommand(ShellCommand shellCommand, string[] args)
        {
            Console.WriteLine("Ivoke command \"{0}\" arguments: [ {1} ]", shellCommand.Pattern, string.Join(", ", args));
        }
    }
}