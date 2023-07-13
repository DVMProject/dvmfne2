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

namespace fnerouter
{
    /// <summary>
    /// Implements a peer FNE router system.
    /// </summary>
    public class RouterPeerSystem : FneSystemBase
    {
        protected FnePeer peer;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RouterMasterSystem"/> class.
        /// </summary>
        /// <param name="service">Instance of the <see cref="RouterService"/> that owns this system</param>
        /// <param name="peerConfig">Peer stanza configuration</param>
        public RouterPeerSystem(RouterService service, ConfigPeerObject peerConfig) : base(service, Create(peerConfig))
        {
            this.peer = (FnePeer)fne;
        }

        /// <summary>
        /// Internal helper to instantiate a new instance of <see cref="FnePeer"/> class.
        /// </summary>
        /// <param name="config">Peer stanza configuration</param>
        /// <returns><see cref="FnePeer"/></returns>
        private static FnePeer Create(ConfigPeerObject config)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, config.Port);

            if (config.MasterAddress == null)
                throw new NullReferenceException("address");
            if (config.MasterAddress == string.Empty)
                throw new ArgumentException("address");

            // handle using address as IP or resolving from hostname to IP
            try
            {
                endpoint = new IPEndPoint(IPAddress.Parse(config.MasterAddress), config.MasterPort);
            }
            catch (FormatException)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(config.MasterAddress);
                if (addresses.Length > 0)
                    endpoint = new IPEndPoint(addresses[0], config.MasterPort);
            }

            FnePeer peer = new FnePeer(config.Name, config.PeerId, endpoint);

            // set configuration parameters
            peer.RawPacketTrace = Program.Configuration.RawPacketTrace;

            peer.PingTime = Program.Configuration.PingTime;
            peer.Passphrase = config.Passphrase;
            peer.Information.Details = ConfigPeerObject.ConvertToDetails(config);

            return peer;
        }

        /// <summary>
        /// Helper to send a activity transfer message to the master.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendActivityTransfer(string message)
        {
            byte[] data = new byte[message.Length + 11];
            FneUtils.StringToBytes(message, data, 11, message.Length);

            peer.SendMaster(FneBase.CreateOpcode(Constants.NET_FUNC_TRANSFER, Constants.NET_TRANSFER_SUBFUNC_ACTIVITY), data);
        }

        /// <summary>
        /// Helper to send a diagnostics transfer message to the master.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendDiagnosticsTransfer(string message)
        {
            byte[] data = new byte[message.Length + 11];
            FneUtils.StringToBytes(message, data, 11, message.Length);

            peer.SendMaster(FneBase.CreateOpcode(Constants.NET_FUNC_TRANSFER, Constants.NET_TRANSFER_SUBFUNC_DIAG), data);
        }
    } // public class RouterPeerSystem
} // namespace fnerouter
