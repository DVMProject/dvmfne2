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

using fnecore;

namespace fnerouter
{
    /// <summary>
    /// 
    /// </summary>
    public class ConfigLogObject
    {
        /// <summary>
        /// 
        /// </summary>
        public int DisplayLevel = 1;
        /// <summary>
        /// 
        /// </summary>
        public int FileLevel = 1;
        /// <summary>
        /// 
        /// </summary>
        public string FilePath = ".";
        /// <summary>
        /// 
        /// </summary>
        public string FileRoot = "fnerouter";
    } // public class ConfigLogObject

    /// <summary>
    /// 
    /// </summary>
    public class ConfigRIDObject
    {
        /// <summary>
        /// 
        /// </summary>
        public string Path = ".";
        /// <summary>
        /// 
        /// </summary>
        public string WhitelistRIDFile = "whitelist_ids.yml";
        /// <summary>
        /// 
        /// </summary>
        public string BlacklistRIDFile = "blacklist_ids.yml";
    } // public class ConfigRIDObject

    /// <summary>
    /// 
    /// </summary>
    public class ConfigMasterObject
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name;
        /// <summary>
        /// 
        /// </summary>
        public bool Enabled;
        /// <summary>
        /// 
        /// </summary>
        public bool Repeat;
        /// <summary>
        /// 
        /// </summary>
        public string Address;
        /// <summary>
        /// 
        /// </summary>
        public int Port;
        /// <summary>
        /// 
        /// </summary>
        public string Passphrase;
        /// <summary>
        /// 
        /// </summary>
        public int GroupHangtime;
        /// <summary>
        /// 
        /// </summary>
        public uint PeerId;
    } // public class ConfigMasterObject

    /// <summary>
    /// 
    /// </summary>
    public class ConfigPeerObject
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name;
        /// <summary>
        /// 
        /// </summary>
        public bool Enabled;
        /// <summary>
        /// 
        /// </summary>
        public string Address;
        /// <summary>
        /// 
        /// </summary>
        public int Port;
        /// <summary>
        /// 
        /// </summary>
        public string MasterAddress;
        /// <summary>
        /// 
        /// </summary>
        public int MasterPort;
        /// <summary>
        /// 
        /// </summary>
        public string Passphrase;
        /// <summary>
        /// 
        /// </summary>
        public string Identity;
        /// <summary>
        /// 
        /// </summary>
        public uint PeerId;
        /// <summary>
        /// 
        /// </summary>
        public uint RxFrequency;
        /// <summary>
        /// 
        /// </summary>
        public uint TxFrequency;
        /// <summary>
        /// 
        /// </summary>
        public double Latitude;
        /// <summary>
        /// 
        /// </summary>
        public double Longitude;
        /// <summary>
        /// 
        /// </summary>
        public string Location;

        /*
        ** Methods
        */

        /// <summary>
        /// Helper to convert the <see cref="ConfigPeerObject"/> to a <see cref="PeerDetails"/> object.
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public static PeerDetails ConvertToDetails(ConfigPeerObject peer)
        {
            PeerDetails details = new PeerDetails();

            // identity
            details.Identity = peer.Identity;
            details.RxFrequency = peer.RxFrequency;
            details.TxFrequency = peer.TxFrequency;

            // system info
            details.Latitude = peer.Latitude;
            details.Longitude = peer.Longitude;
            details.Height = 1;
            details.Location = peer.Location;

            // channel data
            details.TxPower = 0;
            details.TxOffsetMhz = 0.0f;
            details.ChBandwidthKhz = 0.0f;
            details.ChannelID = 0;
            details.ChannelNo = 0;

            // RCON
            details.Password = "ABCD123";
            details.Port = 9990;

            details.Software = AssemblyVersion._VERSION;

            return details;
        }
    } // public class ConfigPeerObject

    /// <summary>
    /// 
    /// </summary>
    public class ConfigurationObject
    {
        /// <summary>
        /// 
        /// </summary>
        public ConfigLogObject Log = new ConfigLogObject();

        /// <summary>
        /// 
        /// </summary>
        public int PingTime = 5;

        /// <summary>
        /// 
        /// </summary>
        public int MaxMissedPings = 5;

        /// <summary>
        /// 
        /// </summary>
        public bool RawPacketTrace = false;

        /// <summary>
        /// 
        /// </summary>
        public bool AllowActTrans = true;
        /// <summary>
        /// 
        /// </summary>
        public bool AllowDiagTrans = true;
        /// <summary>
        /// 
        /// </summary>
        public int MonitorServerPort = 5555;
        /// <summary>
        /// 
        /// </summary>
        public string ActivityLogFile = "activity_log.log";
        /// <summary>
        /// 
        /// </summary>
        public string DiagLogPath = ".";

        /// <summary>
        /// 
        /// </summary>
        public string RoutingRulesFile = "routing_rules.yml";

        /// <summary>
        /// 
        /// </summary>
        public int RoutingRuleUpdateTime = 30;

        /// <summary>
        /// 
        /// </summary>
        public ConfigRIDObject Rids = new ConfigRIDObject();

        /// <summary>
        /// 
        /// </summary>
        public List<ConfigMasterObject> Masters = new List<ConfigMasterObject>();

        /// <summary>
        /// 
        /// </summary>
        public List<ConfigPeerObject> Peers = new List<ConfigPeerObject>();
    } // public class ConfigurationObject
} // namespace fnerouter
