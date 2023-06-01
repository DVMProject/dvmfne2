/**
* Digital Voice Modem - Fixed Network Equipment
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment
*
*/
/*
*   Copyright (C) 2022 by Bryan Biedenkapp N2PLL
*
*   This program is free software: you can redistribute it and/or modify
*   it under the terms of the GNU Affero General Public License as published by
*   the Free Software Foundation, either version 3 of the License, or
*   (at your option) any later version.
*
*   This program is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU Affero General Public License for more details.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Serilog;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace fnerouter
{
    /// <summary>
    /// Implements the FNE router.
    /// </summary>
    public class RouterService : BackgroundService
    {
        private const int RID_LIST_UPDATE = 5; // minutes
        private const int MAX_ACT_LOG_LINES = 2048;

        private List<FneSystemBase> systems = new List<FneSystemBase>();
     
        private List<RoutingRule> rules = new List<RoutingRule>();
        private DateTime lastRuleUpdate = DateTime.Now;

        private List<RadioID> whitelist = new List<RadioID>();
        private List<RadioID> blacklist = new List<RadioID>();
        private DateTime lastRIDListUpdate = DateTime.Now;

        private DateTime activityLogSplitUpdate = DateTime.Now;
        private FileStream activityLog = null;
        private Dictionary<uint, FileStream> peerDiagLog = new Dictionary<uint, FileStream>();

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the list of registered running systems.
        /// </summary>
        public List<FneSystemBase> Systems => systems;

        /// <summary>
        /// Gets the list of routing rules.
        /// </summary>
        public List<RoutingRule> Rules => rules;

        /// <summary>
        /// Get the list of whitelisted RIDs.
        /// </summary>
        public List<RadioID> Whitelist => whitelist;

        /// <summary>
        /// Get the list of blacklisted RIDs.
        /// </summary>
        public List<RadioID> Blacklist => blacklist;

        /*
        ** Methods
        */

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        /// <returns><see cref="Task"/></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Logger.Information("[Router Service] SYSTEM STARTING...");

            List<ConfigMasterObject> masterConfigs = Program.Configuration.Masters;
            List<ConfigPeerObject> peerConfigs = Program.Configuration.Peers;

            // setup activity log
            if (Program.Configuration.AllowActTrans)
            {
                string actLogPath = Path.Combine(new string[] { Program.Configuration.Log.FilePath, Program.Configuration.ActivityLogFile });
                activityLog = new FileStream(actLogPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                activityLog.Seek(0, SeekOrigin.End);
                Log.Logger.Information($"[Router Service] ACTIVITY LOG {actLogPath} CONFIGURED");
            }

            // load the routing rules
            LoadRoutingRules();

            // load the white and blacklist RID files
            if (Program.Configuration.Rids != null)
            {
                // load whitelist
                if (Program.Configuration.Rids.WhitelistRIDFile != null)
                {
                    Log.Logger.Information($"[Router Service] LOADING WHITELIST RIDS {Program.Configuration.Rids.WhitelistRIDFile}");
                    whitelist = LoadRadioIDFile(Program.Configuration.Rids.WhitelistRIDFile);
                }

                // load blacklist
                if (Program.Configuration.Rids.BlacklistRIDFile != null)
                {
                    Log.Logger.Information($"[Router Service] LOADING WHITELIST RIDS {Program.Configuration.Rids.BlacklistRIDFile}");
                    blacklist = LoadRadioIDFile(Program.Configuration.Rids.BlacklistRIDFile);
                }

                lastRIDListUpdate = DateTime.Now;
            }

            // initialize master systems
            foreach (ConfigMasterObject masterConfig in masterConfigs)
            {
                if (masterConfig.Enabled)
                {
                    Log.Logger.Information($"[Router Service] MASTER: REGISTER SYSTEM {masterConfig.Name}");
                    RouterMasterSystem system = new RouterMasterSystem(this, masterConfig);
                    systems.Add(system);
                    system.Start();
                }
                else
                    Log.Logger.Information($"[Router Service] MASTER: SYSTEM {masterConfig.Name} NOT REGISTERED (DISABLED)");
            }

            // initialize peer systems
            foreach (ConfigPeerObject peerConfig in peerConfigs)
            {
                if (peerConfig.Enabled)
                {
                    Log.Logger.Information($"[Router Service] PEER: REGISTER SYSTEM {peerConfig.Name}");
                    RouterPeerSystem system = new RouterPeerSystem(this, peerConfig);
                    systems.Add(system);
                    system.Start();
                }
                else
                    Log.Logger.Information($"[Router Service] PEER: SYSTEM {peerConfig.Name} NOT REGISTERED (DISABLED)");
            }

            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        /// <returns><see cref="Task"/></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Logger.Information("[Router Service] SYSTEM STOPPING...");

            // deregister systems
            foreach (FneSystemBase system in systems)
            {
                Log.Logger.Information($"[Router Service] DE-REGISTER SYSTEM {system.SystemName}");
                if (system.IsStarted)
                    system.Stop();
            }
            systems = null;

            // close any peer diagnostic logs
            foreach (FileStream stream in peerDiagLog.Values)
            {
                if (stream != null)
                {
                    stream.Flush();
                    stream.Close();
                }
            }
            peerDiagLog = null;

            // flush activity log
            activityLog.Flush();
            activityLog.Close();
            activityLog = null;

            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that 
        /// represents the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="token">Triggered when <see cref="StopAsync(CancellationToken)"/> is called.</param>
        /// <returns>A <see cref="Task"/> Task that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            Log.Logger.Information("[Router Service] SYSTEM RUNNING...");

            // idle loop (used to update rules, and other various datasets that need to update on a cycle)
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // update routing rules
                    DateTime dt = lastRuleUpdate.AddMinutes(Program.Configuration.RoutingRuleUpdateTime);
                    if (dt < DateTime.Now)
                    {
                        LoadRoutingRules();

                        // update system rules
                        foreach (FneSystemBase system in Systems)
                            system.UpdateRoutingRules();

                        // send lists to connected peers
                        RouterMasterSystem.SendTGIDs(Systems);
                        RouterMasterSystem.SendDeactiveTGIDs(Systems);
                    }

                    // update white and blacklists
                    if (Program.Configuration.Rids != null)
                    {
                        dt = lastRIDListUpdate.AddMinutes(RID_LIST_UPDATE);
                        if (dt < DateTime.Now)
                        {
                            // load whitelist
                            if (Program.Configuration.Rids.WhitelistRIDFile != null)
                            {
                                Log.Logger.Information($"[Router Service] LOADING WHITELIST RIDS {Program.Configuration.Rids.WhitelistRIDFile}");
                                whitelist = LoadRadioIDFile(Program.Configuration.Rids.WhitelistRIDFile);
                            }

                            // load blacklist
                            if (Program.Configuration.Rids.BlacklistRIDFile != null)
                            {
                                Log.Logger.Information($"[Router Service] LOADING WHITELIST RIDS {Program.Configuration.Rids.BlacklistRIDFile}");
                                blacklist = LoadRadioIDFile(Program.Configuration.Rids.BlacklistRIDFile);
                            }

                            // send lists to connected peers
                            RouterMasterSystem.SendWhitelistRIDs(Systems);
                            RouterMasterSystem.SendBlacklistRIDs(Systems);

                            lastRIDListUpdate = DateTime.Now;
                        }
                    }

                    // check if the activity log needs to be split
                    dt = activityLogSplitUpdate.AddSeconds(3600);
                    if (dt < DateTime.Now)
                    {
                        lock (activityLog)
                        {
                            string actLogPath = Path.Combine(new string[] { Program.Configuration.Log.FilePath, Program.Configuration.ActivityLogFile });
                            activityLog.Seek(0, SeekOrigin.Begin);

                            // count number of lines in the file
                            int nLines = 0;
                            using (StreamReader reader = new StreamReader(actLogPath))
                            {
                                while (reader.ReadLine() != null)
                                    nLines++;
                            }

                            if (nLines < MAX_ACT_LOG_LINES)
                                activityLog.Seek(0, SeekOrigin.End);
                            else
                            {
                                Log.Logger.Information($"[Router Service] SPLITTING ACTIVITY LOG {actLogPath}");
                                SplitFile(actLogPath, ref activityLog);
                                activityLog.Seek(0, SeekOrigin.End);
                            }
                        }

                        activityLogSplitUpdate = DateTime.Now;
                    }

                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException) { /* stub */ }
            }
        }

        /// <summary>
        /// Helper to get the rules for the given instance of <see cref="FneSystemBase"/>.
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public RoutingRule GetRules(FneSystemBase system)
        {
            if (system.SystemName == null)
                return null;
            if (Rules.Count == 0)
                return null;

            return Rules.Find((x) => x.Name.ToUpperInvariant() == system.SystemName.ToUpperInvariant());
        }

        /// <summary>
        /// Internal helper to load the routing rules from disk.
        /// </summary>
        private void LoadRoutingRules()
        {
            if (Program.Configuration.RoutingRulesFile == null)
            {
                Log.Logger.Error($"No routing rules file defined in configuration?");
                return;
            }

            if (Program.Configuration.RoutingRulesFile == string.Empty)
            {
                Log.Logger.Error($"No routing rules file defined in configuration?");
                return;
            }

            Log.Logger.Information($"[Router Service] LOADING ROUTING RULES {Program.Configuration.RoutingRulesFile}");

            // load routing rules
            try
            {
                // get the routing rules file path from the config
                string path = Program.Configuration.RoutingRulesFile;
                if (Path.GetDirectoryName(path) == string.Empty)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    string executingPath = Path.GetDirectoryName(assembly.Location);
                    path = Path.Combine(new string[] { executingPath, path });
                }

                // does the rules file exist?
                if (!File.Exists(path))
                {
                    Log.Logger.Error($"Cannot find the routing rules file, {Program.Configuration.RoutingRulesFile}");
                    return;
                }

                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (TextReader reader = new StreamReader(stream))
                    {
                        string yml = reader.ReadToEnd();

                        // setup the YAML deseralizer for the configuration
                        IDeserializer ymlDeserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                        // we're gonna do this this way so we can catch an exception and not blow away the running
                        // rules
                        List<RoutingRule> newRules = ymlDeserializer.Deserialize<List<RoutingRule>>(yml);

                        // overwrite rules
                        rules.Clear();
                        rules = new List<RoutingRule>(newRules);
                        lastRuleUpdate = DateTime.Now;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, $"Cannot read the routing rules file, {Program.Configuration.RoutingRulesFile}");
            }

            // dump loaded rule information
            foreach (RoutingRule rule in rules)
                foreach (RoutingRuleGroupVoice gv in rule.GroupVoice)
                {
                    // generate ignored string
                    string ignored = "None";
                    if (gv.Config.Ignored != null)
                    {
                        if (gv.Config.Ignored.Count > 0)
                        {
                            ignored = string.Empty;
                            foreach (int peerId in gv.Config.Ignored)
                                ignored += $"{peerId}, ";
                        }
                    }
                    ignored = ignored.TrimEnd(new char[] { ',', ' ' });

                    // generate destinations string
                    string destinations = "None";
                    if (gv.Destination != null)
                    {
                        if (gv.Destination.Count > 0)
                        {
                            destinations = string.Empty;
                            foreach (RoutingRuleGroupVoiceDestination dst in gv.Destination)
                                destinations += $"{dst.Network} (DST_TGID: {dst.Tgid} DST_TS: {dst.Slot}), ";
                        }
                    }
                    destinations = destinations.TrimEnd(new char[] { ',', ' ' });

                    Log.Logger.Information($"Rule ({rule.Name}) NAME: {gv.Name} SRC_TGID: {gv.Source.Tgid} SRC_TS: {gv.Source.Slot} ACTIVE: {gv.Config.Active} ROUTABLE: {gv.Config.Routable} AFFILIATED: {gv.Config.Affiliated} IGNORED: {ignored} DST: {destinations}");
                }
        }

        /// <summary>
        /// Internal helper to save the routing rules to disk.
        /// </summary>
        private void SaveRoutingRules()
        {
            if (Program.Configuration.RoutingRulesFile == null)
            {
                Log.Logger.Error($"No routing rules file defined in configuration?");
                return;
            }

            if (Program.Configuration.RoutingRulesFile == string.Empty)
            {
                Log.Logger.Error($"No routing rules file defined in configuration?");
                return;
            }

            if (rules.Count == 0)
            {
                Log.Logger.Error($"No routing rules defined to save?");
                return;
            }

            Log.Logger.Information($"[Router Service] SAVING ROUTING RULES {Program.Configuration.RoutingRulesFile}");

            // save routing rules
            try
            {
                string path = Program.Configuration.RoutingRulesFile;
                if (Path.GetDirectoryName(path) == string.Empty)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    string executingPath = Path.GetDirectoryName(assembly.Location);
                    path = Path.Combine(new string[] { executingPath, path });
                }

                using (FileStream stream = new FileStream(Program.Configuration.RoutingRulesFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (TextWriter writer = new StreamWriter(stream))
                    {
                        ISerializer ymlSerializer = new SerializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                        ymlSerializer.Serialize(writer, rules);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, $"Cannot write the routing rules file, {Program.Configuration.RoutingRulesFile}");
            }
        }

        /// <summary>
        /// Internal helper to load a radio ID file from disk.
        /// </summary>
        private List<RadioID> LoadRadioIDFile(string radioIdFile)
        {
            // load rado ID file
            try
            {
                // get the routing rules file path from the config
                string path = radioIdFile;
                if (Path.GetDirectoryName(path) == string.Empty)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    string executingPath = Path.GetDirectoryName(assembly.Location);
                    path = Path.Combine(new string[] { executingPath, path });
                }

                // does the radio ID file exist?
                if (!File.Exists(path))
                {
                    Log.Logger.Error($"Cannot find the radio ID file, {radioIdFile}");
                    return new List<RadioID>();
                }

                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (TextReader reader = new StreamReader(stream))
                    {
                        string yml = reader.ReadToEnd();

                        // setup the YAML deseralizer for the configuration
                        IDeserializer ymlDeserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                        return ymlDeserializer.Deserialize<List<RadioID>>(yml);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, $"Cannot read the radio ID file, {radioIdFile}");
            }

            return new List<RadioID>();
        }

        /// <summary>
        /// Helper to write an entry to the activity log.
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="message"></param>
        public void WriteActivityLog(uint peerId, string message)
        {
            if (!Program.Configuration.AllowActTrans)
                return;

            lock (activityLog)
            {
                TextWriter writer = new StreamWriter(activityLog);
                writer.WriteLine($"{peerId} {message}");
            }
        }

        /// <summary>
        /// Helper to return to setup a peer diagnostics log.
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        public void SetupPeerDiagLog(uint peerId)
        {
            if (!Program.Configuration.AllowDiagTrans)
                return;
            if (peerDiagLog.ContainsKey(peerId))
            {
                Log.Logger.Error($"Tried to setup peer diagnostics log for PEER {peerId} when already setup?");
                return;
            }

            string filePath = Path.Combine(new string[] { Program.Configuration.DiagLogPath, $"{peerId}.log" });
            FileStream stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            peerDiagLog.Add(peerId, stream);
        }

        /// <summary>
        /// Helper to teardown a setup peer diagnostics log.
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        public void TearDownPeerDiagLog(uint peerId)
        {
            if (!Program.Configuration.AllowDiagTrans)
                return;
            if (!peerDiagLog.ContainsKey(peerId))
            {
                Log.Logger.Error($"Tried to teardown peer diagnostics log for PEER {peerId} when not setup?");
                return;
            }

            peerDiagLog[peerId].Flush();
            peerDiagLog[peerId].Close();
            peerDiagLog.Remove(peerId);
        }

        /// <summary>
        /// Helper to write an entry to a peer diagnostics log.
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="message"></param>
        public void WritePeerDiagLog(uint peerId, string message)
        {
            if (!Program.Configuration.AllowDiagTrans)
                return;
            if (!peerDiagLog.ContainsKey(peerId))
            {
                Log.Logger.Error($"Tried to write log entry for PEER {peerId} when logging not setup?");
                return;
            }

            TextWriter writer = new StreamWriter(peerDiagLog[peerId]);
            writer.WriteLine($"{peerId} {message}");
        }

        /// <summary>
        /// Helper to split a file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="_in"></param>
        /// <param name="percentage"></param>
        private void SplitFile(string filePath, ref FileStream _in, float percentage = 0.50f)
        {
            string filenameOut = Path.GetFileName(filePath) + ".1"; // yes this will be overridden...
            using (FileStream stream = new FileStream(Path.Combine(new string[] { Path.GetDirectoryName(filePath), filenameOut }), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                _in.Flush();

                // get the number of lines and current content of the input stream
                int nLines = 0;
                string content = string.Empty;
                _in.Seek(0, SeekOrigin.Begin);
                using (TextReader reader = new StreamReader(_in))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        content += line;
                        nLines++;
                    }
                }

                // rewind stream
                _in.Seek(0, SeekOrigin.Begin);

                int nTrain = (int)(nLines * percentage);
                int nValid = nLines - nTrain;

                // close and truncate input stream
                _in.Close();
                _in = new FileStream(filePath, FileMode.Truncate, FileAccess.ReadWrite, FileShare.Read);

                // write out split content to the output file
                TextWriter _inWriter = new StreamWriter(_in);
                using (TextWriter _outWriter = new StreamWriter(stream))
                {
                    string[] split = content.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    int i = 0;
                    foreach (string line in split)
                    {
                        if ((i < nTrain) || (nLines - i > nValid))
                        {
                            _outWriter.WriteLine(line);
                            i++;
                        }
                        else
                            _inWriter.WriteLine(line);
                    }
                }

                stream.Flush();
                _in.Flush();
                _in.Seek(0, SeekOrigin.End);
            }
        }
    } // public class RouterService : BackgroundService
} // namespace fnerouter
