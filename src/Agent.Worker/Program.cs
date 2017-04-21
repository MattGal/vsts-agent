﻿using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task<int> MainAsync(
            string[] args)
        {
            //ITerminal registers a CTRL-C handler, which keeps the Agent.Worker process running
            //and lets the Agent.Listener handle gracefully the exit.
            using (var hc = new HostContext("Worker"))
            using (var term = hc.GetService<ITerminal>())
            {
                Tracing trace = hc.GetTrace(nameof(Program));
                try
                {
                    trace.Info($"Version: {Constants.Agent.Version}");
                    trace.Info($"Commit: {BuildConstants.Source.CommitHash}");
                    trace.Info($"Culture: {CultureInfo.CurrentCulture.Name}");
                    trace.Info($"UI Culture: {CultureInfo.CurrentUICulture.Name}");

                    // Validate args.
                    ArgUtil.NotNull(args, nameof(args));
                    ArgUtil.Equal(3, args.Length, nameof(args.Length));
                    ArgUtil.NotNullOrEmpty(args[0], $"{nameof(args)}[0]");
                    ArgUtil.Equal("spawnclient", args[0].ToLowerInvariant(), $"{nameof(args)}[0]");
                    ArgUtil.NotNullOrEmpty(args[1], $"{nameof(args)}[1]");
                    ArgUtil.NotNullOrEmpty(args[2], $"{nameof(args)}[2]");
                    var worker = hc.GetService<IWorker>();

                    // Run the worker.
                    return await worker.RunAsync(
                        pipeIn: args[1],
                        pipeOut: args[2]);
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException)
                    {
                        foreach (var inner in (ex as AggregateException).InnerExceptions)
                        {
                            // Populate any exception that cause worker failure back to agent.
                            Console.WriteLine(inner.ToString());
                            try
                            {
                                trace.Error(inner);
                            }
                            catch (Exception e)
                            {
                                // make sure we don't crash the app on trace error.
                                // since IOException will throw when we run out of disk space.
                                Console.WriteLine(e.ToString());
                            }
                        }
                    }
                    else
                    {
                        // Populate any exception that cause worker failure back to agent.
                        Console.WriteLine(ex.ToString());
                        try
                        {
                            trace.Error(ex);
                        }
                        catch (Exception e)
                        {
                            // make sure we don't crash the app on trace error.
                            // since IOException will throw when we run out of disk space.
                            Console.WriteLine(e.ToString());
                        }
                    }
                }
                finally
                {
                    hc.Dispose();
                }

                return 1;
            }
        }
    }
}
