﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace CitizenMP.Server
{
    public class Configuration
    {
        public static Configuration Load(string filename)
        {
            var buffer = File.ReadAllText(filename);

            var deserializer = new Deserializer(ignoreUnmatched: true);
            return deserializer.Deserialize<Configuration>(new StringReader(buffer));
        }

        public bool ScriptDebug { get; set; }

        public List<string> AutoStartResources { get; set; }

        public List<string> PreParseResources { get; set; }

        public string RconPassword { get; set; }

        public int ListenPort { get; set; }

        public bool DisableAuth { get; set; }

        public string Hostname { get; set; }

        public string Game { get; set; }

        public List<ImportConfiguration> Imports { get; set; }

        public Dictionary<string, DownloadConfiguration> Downloads { get; set; }

        public DownloadConfiguration GetDownloadConfiguration(string resourceName)
        {
            DownloadConfiguration config;

            if (Downloads.TryGetValue(resourceName, out config))
            {
                return config;
            }

            if (Downloads.TryGetValue("all", out config))
            {
                return config;
            }

            return null;
        }

        public bool DisableWindowedLogger { get; set; }

        public bool DebugLog { get; set; }

        public int Players { get; set; }

        public string Mapname { get; set; }
    }

    public class DownloadConfiguration
    {
        public string BaseURL { get; set; }

        public string UploadURL { get; set; }
    }

    public class ImportConfiguration
    {
        public string ConfigURL { get; set; }
    }
}
