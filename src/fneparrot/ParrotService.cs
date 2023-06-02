﻿/**
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Serilog;

using fnecore;

namespace fneparrot
{
    /// <summary>
    /// Implements the FNE parrot.
    /// </summary>
    public class ParrotService : BackgroundService
    {
        public static List<ConfigMasterObject> Masters = null;
        private List<FneSystemBase> systems = new List<FneSystemBase>();

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the list of registered running systems.
        /// </summary>
        public List<FneSystemBase> Systems => systems;

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
            Log.Logger.Information("[Parrot Service] SYSTEM STARTING...");

            // initialize master systems
            foreach (ConfigMasterObject masterConfig in Masters)
            {
                if (masterConfig.Enabled)
                {
                    Log.Logger.Information($"[Parrot Service] MASTER: REGISTER SYSTEM {masterConfig.Name}");
                    MasterSystem system = new MasterSystem(masterConfig);
                    systems.Add(system);
                    system.Start();
                }
                else
                    Log.Logger.Information($"[Parrot Service] MASTER: SYSTEM {masterConfig.Name} NOT REGISTERED (DISABLED)");
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
            Log.Logger.Information("[Parrot Service] SYSTEM STOPPING...");

            foreach (FneSystemBase system in systems)
            {
                Log.Logger.Information($"[Parrot Service] DE-REGISTER SYSTEM {system.SystemName}");
                if (system.IsStarted)
                    system.Stop();
            }
            systems = null;

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
            Log.Logger.Information("[Parrot Service] SYSTEM RUNNING...");

            // idle loop (used to update rules, and other various datasets that need to update on a cycle)
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException) { /* stub */ }
            }
        }
    } // public class ParrotService : BackgroundService
} // namespace fneparrot
