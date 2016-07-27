﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CitizenMP.Server.Logging;

namespace CitizenMP.Server
{
    class Program
    {
        public static string RootDirectory { get; private set; }

        private async Task Start(string configFileName)
        {
            Configuration config;
            Console.Title = "MultiFive dedicated server";
            try
            {
                config = Configuration.Load(configFileName ?? "config.yml");

                if (config.AutoStartResources == null)
                {
                    this.Log().Fatal("No auto-started resources were configured.");
                    return;
                }

                if (config.ListenPort == 0)
                {
                    this.Log().Fatal("No port was configured.");
                    return;
                }

                if (config.Downloads == null)
                {
                    config.Downloads = new Dictionary<string, DownloadConfiguration>();
                }

                if(config.Players > 50 || config.Players < 0)
                {
                    this.Log().Fatal("Invalid count of players.");
                    return;
                }
            }
            catch (System.IO.IOException)
            {
                this.Log().Fatal("Could not open the configuration file {0}.", configFileName ?? "config.yml");
                return;
            }

            this.Log().Info("Creating initial server instance.");

            var commandManager = new Commands.CommandManager();
            var resManager = new Resources.ResourceManager(config);

            // create the game server (as resource scanning needs it now)
            var gameServer = new Game.GameServer(config, resManager, commandManager);

            // preparse resources
            if (config.PreParseResources != null)
            {
                this.Log().Info("Pre-parsing resources: {0}", string.Join(", ", config.PreParseResources));

                foreach (var resource in config.PreParseResources)
                {
                    resManager.ScanResources("resources/", resource);

                    var res = resManager.GetResource(resource);

                    if (res != null)
                    {
                        await res.Start();
                    }
                }
            }
            else
            {
                this.Log().Warn("No PreParseResources defined. This usually means you're using an outdated configuration file. Please consider this.");
            }

            // scan resources
            resManager.ScanResources("resources/");

            // start the game server
            gameServer.Start();

            // and initialize the HTTP server
            var httpServer = new HTTP.HttpServer(config, resManager);
            httpServer.Start();

            // start resources
            foreach (var resource in config.AutoStartResources)
            {
                var res = resManager.GetResource(resource);

                if (res == null)
                {
                    this.Log().Error("Could not find auto-started resource {0}.", resource);
                }
                else
                {
                    await res.Start();
                }
            }

            // start synchronizing the started resources
            resManager.StartSynchronization();

            // main loop
            int lastTickCount = Environment.TickCount;

            while (true)
            {
                Thread.Sleep(5);

                var tc = Environment.TickCount;

                gameServer.Tick(tc - lastTickCount);

                lastTickCount = tc;
            }
        }

        static void Main(string[] args)
        {
            BaseLog.SetStripSourceFilePath();

            Time.Initialize();

            try
            {
                RootDirectory = Environment.CurrentDirectory;

                // start the program
                new Program().Start((args.Length > 0) ? args[0] : null).Wait();

                Environment.Exit(0);
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e.InnerException.ToString());

                Environment.Exit(1);
            }
        }
    }
}
