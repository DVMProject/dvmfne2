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
using System.Net;
using System.Collections.Generic;
using System.Text;

using Serilog;

using fnecore;

namespace fneparrot
{
    /// <summary>
    /// Implements a master FNE parrot system.
    /// </summary>
    public class MasterSystem : FneSystemBase
    {
        public static ConfigurationObject Configuration = null;
        protected FneMaster master;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MasterSystem"/> class.
        /// </summary>
        /// <param name="masterConfig">Master stanza configuration</param>
        public MasterSystem(ConfigMasterObject masterConfig) : base(Create(masterConfig))
        {
            this.master = (FneMaster)fne;
        }

        /// <summary>
        /// Internal helper to instantiate a new instance of <see cref="FneMaster"/> class.
        /// </summary>
        /// <param name="config">Master stanza configuration</param>
        /// <returns><see cref="FneMaster"/></returns>
        private static FneMaster Create(ConfigMasterObject config)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, config.Port);
            if (config.Address != null)
            {
                if (config.Address != string.Empty)
                    endpoint = new IPEndPoint(IPAddress.Parse(config.Address), config.Port);
            }

            FneMaster master = new FneMaster(config.Name, config.PeerId, endpoint);

            // set configuration parameters
            master.RawPacketTrace = Configuration.RawPacketTrace;

            master.PingTime = Configuration.PingTime;
            master.MaxMissed = Configuration.MaxMissedPings;
            master.Passphrase = config.Passphrase;
            master.Repeat = config.Repeat;

            return master;
        }
    } // public class RouterMasterSystem
} // namespace fneparrot
