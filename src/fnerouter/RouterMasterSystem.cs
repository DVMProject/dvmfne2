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
    /// Implements a master FNE router system.
    /// </summary>
    public class RouterMasterSystem : FneSystemBase
    {
        protected FneMaster master;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RouterMasterSystem"/> class.
        /// </summary>
        /// <param name="service">Instance of the <see cref="RouterService"/> that owns this system</param>
        /// <param name="config">Server configuration</param>
        /// <param name="masterConfig">Master stanza configuration</param>
        public RouterMasterSystem(RouterService service, ConfigMasterObject masterConfig) : base(service, Create(masterConfig))
        {
            this.master = (FneMaster)fne;

            // hook callbacks
            master.PeerConnected += PeerConnected;
            master.PeerDisconnected = PeerDisconnected;

            master.ActivityTransfer = ActivityTransferLog;
            master.DiagnosticTransfer = DiagnosticTransferLog;
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
            master.RawPacketTrace = Program.Configuration.RawPacketTrace;
            master.AllowActivityTransfer = Program.Configuration.AllowActTrans;
            master.AllowDiagnosticTransfer = Program.Configuration.AllowDiagTrans;

            master.PingTime = Program.Configuration.PingTime;
            master.MaxMissed = Program.Configuration.MaxMissedPings;
            master.Passphrase = config.Passphrase;
            master.Repeat = config.Repeat;

            return master;
        }

        /// <summary>
        /// Helper to send the whitelisted RIDs for all configured masters.
        /// </summary>
        /// <param name="systems"></param>
        public static void SendWhitelistRIDs(List<FneSystemBase> systems)
        {
            if (systems.Count == 0)
                return;

            List<FneSystemBase> masters = systems.FindAll((x) => x.FneType == FneType.MASTER);
            if (masters != null)
            {
                if (masters.Count > 0)
                {
                    foreach (FneSystemBase system in masters)
                    {
                        RouterMasterSystem master = (RouterMasterSystem)system;
                        master.SendWhitelistRIDs();
                    }
                }
            }
        }

        /// <summary>
        /// Helper to send the whitelisted RIDs if this is a master.
        /// </summary>
        public void SendWhitelistRIDs()
        {
            if (service.Whitelist.Count == 0)
                return;
            if (fne.FneType != FneType.MASTER)
                return;

            foreach (uint peerId in master.Peers.Keys)
                SendWhitelistRIDs(peerId);
        }

        /// <summary>
        /// Helper to send the whitelisted RIDs if this is a master.
        /// </summary>
        /// <param name="peerId"></param>
        public void SendWhitelistRIDs(uint peerId)
        {
            if (service.Whitelist.Count == 0)
                return;
            if (fne.FneType != FneType.MASTER)
                return;

            // build data set to send to peer
            int dataLen = 4 + (service.Whitelist.Count * 4);
            byte[] data = new byte[dataLen];
            FneUtils.WriteBytes(service.Whitelist.Count, ref data, 0);

            int offs = 4;
            foreach (RadioID rid in service.Whitelist)
            {
                FneUtils.WriteBytes(rid.Id, ref data, offs);
                offs += 4;
            }

            master.SendPeerCommand(peerId, FneBase.CreateOpcode(Constants.NET_FUNC_MASTER, Constants.NET_MASTER_SUBFUNC_WL_RID), data, true);
        }

        /// <summary>
        /// Helper to send the blacklisted RIDs for all configured masters.
        /// </summary>
        /// <param name="systems"></param>
        public static void SendBlacklistRIDs(List<FneSystemBase> systems)
        {
            if (systems.Count == 0)
                return;

            List<FneSystemBase> masters = systems.FindAll((x) => x.FneType == FneType.MASTER);
            if (masters != null)
            {
                if (masters.Count > 0)
                {
                    foreach (FneSystemBase system in masters)
                    {
                        RouterMasterSystem master = (RouterMasterSystem)system;
                        master.SendBlacklistRIDs();
                    }
                }
            }
        }

        /// <summary>
        /// Helper to send the blacklisted RIDs if this is a master.
        /// </summary>
        public void SendBlacklistRIDs()
        {
            if (service.Blacklist.Count == 0)
                return;
            if (fne.FneType != FneType.MASTER)
                return;

            foreach (uint peerId in master.Peers.Keys)
                SendBlacklistRIDs(peerId);
        }

        /// <summary>
        /// Helper to send the blacklisted RIDs if this is a master.
        /// </summary>
        /// <param name="peerId"></param>
        public void SendBlacklistRIDs(uint peerId)
        {
            if (service.Blacklist.Count == 0)
                return;
            if (fne.FneType != FneType.MASTER)
                return;

            // build data set to send to peer
            int dataLen = 4 + (service.Blacklist.Count * 4);
            byte[] data = new byte[dataLen];
            FneUtils.WriteBytes(service.Blacklist.Count, ref data, 0);

            int offs = 4;
            foreach (RadioID rid in service.Blacklist)
            {
                FneUtils.WriteBytes(rid.Id, ref data, offs);
                offs += 4;
            }

            master.SendPeerCommand(peerId, FneBase.CreateOpcode(Constants.NET_FUNC_MASTER, Constants.NET_MASTER_SUBFUNC_BL_RID), data, true);
        }

        /// <summary>
        /// Helper to send the active TGIDs if this is a master.
        /// </summary>
        /// <param name="systems"></param>
        public static void SendTGIDs(List<FneSystemBase> systems)
        {
            if (systems.Count == 0)
                return;

            List<FneSystemBase> masters = systems.FindAll((x) => x.FneType == FneType.MASTER);
            if (masters != null)
            {
                if (masters.Count > 0)
                {
                    foreach (FneSystemBase system in masters)
                    {
                        RouterMasterSystem master = (RouterMasterSystem)system;
                        master.SendTGIDs();
                    }
                }
            }
        }

        /// <summary>
        /// Helper to send the active TGIDs if this is a master.
        /// </summary>
        public void SendTGIDs()
        {
            if (service.Rules.Count == 0)
                return;
            if (fne.FneType != FneType.MASTER)
                return;

            foreach (uint peerId in master.Peers.Keys)
                SendTGIDs(peerId);
        }

        /// <summary>
        /// Helper to send the active TGIDs if this is a master.
        /// </summary>
        /// <param name="peerId"></param>
        public void SendTGIDs(uint peerId)
        {
            if (service.Rules.Count == 0)
                return;
            if (fne.FneType != FneType.MASTER)
                return;

            RoutingRule routingRules = service.Rules.Find((x) => x.Name.ToUpperInvariant() == master.SystemName.ToUpperInvariant() && x.Master && x.SendTgid);
            if (routingRules != null)
            {
                List<RoutingRuleGroupVoice> groupVoice = routingRules.GroupVoice.FindAll((x) => x.Config.Active);

                // build data set to send to peer
                int dataLen = 4 + (groupVoice.Count * 5);
                byte[] data = new byte[dataLen];
                FneUtils.WriteBytes(groupVoice.Count, ref data, 0);

                int offs = 4;
                foreach (RoutingRuleGroupVoice gv in groupVoice)
                {
                    FneUtils.WriteBytes(gv.Source.Tgid, ref data, offs);
                    data[offs + 4] = gv.Source.Slot;
                    offs += 5;
                }

                master.SendPeerCommand(peerId, FneBase.CreateOpcode(Constants.NET_FUNC_MASTER, Constants.NET_MASTER_SUBFUNC_ACTIVE_TGS), data, true);
            }
        }

        /// <summary>
        /// Helper to send the deactivated TGIDs if this is a master.
        /// </summary>
        /// <param name="systems"></param>
        public static void SendDeactiveTGIDs(List<FneSystemBase> systems)
        {
            if (systems.Count == 0)
                return;

            List<FneSystemBase> masters = systems.FindAll((x) => x.FneType == FneType.MASTER);
            if (masters != null)
            {
                if (masters.Count > 0)
                {
                    foreach (FneSystemBase system in masters)
                    {
                        RouterMasterSystem master = (RouterMasterSystem)system;
                        master.SendDeactiveTGIDs();
                    }
                }
            }
        }

        /// <summary>
        /// Helper to send the deactivated TGIDs if this is a master.
        /// </summary>
        public void SendDeactiveTGIDs()
        {
            if (service.Rules.Count == 0)
                return;
            if (fne.FneType != FneType.MASTER)
                return;

            foreach (uint peerId in master.Peers.Keys)
                SendDeactiveTGIDs(peerId);
        }

        /// <summary>
        /// Helper to send the deactivated TGIDs if this is a master.
        /// </summary>
        /// <param name="peerId"></param>
        public void SendDeactiveTGIDs(uint peerId)
        {
            if (service.Rules.Count == 0)
                return;
            if (fne.FneType != FneType.MASTER)
                return;

            RoutingRule routingRules = service.Rules.Find((x) => x.Name.ToUpperInvariant() == master.SystemName.ToUpperInvariant() && x.Master && x.SendTgid);
            if (routingRules != null)
            {
                List<RoutingRuleGroupVoice> groupVoice = routingRules.GroupVoice.FindAll((x) => !x.Config.Active);

                // build data set to send to peer
                int dataLen = 4 + (groupVoice.Count * 5);
                byte[] data = new byte[dataLen];
                FneUtils.WriteBytes(groupVoice.Count, ref data, 0);

                int offs = 4;
                foreach (RoutingRuleGroupVoice gv in groupVoice)
                {
                    FneUtils.WriteBytes(gv.Source.Tgid, ref data, offs);
                    data[offs + 4] = gv.Source.Slot;
                    offs += 5;
                }

                master.SendPeerCommand(peerId, FneBase.CreateOpcode(Constants.NET_FUNC_MASTER, Constants.NET_MASTER_SUBFUNC_DEACTIVE_TGS), data, true);
            }
        }

        /// <summary>
        /// Event handler called when a peer connects.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="peer">Peer Information</param>
        private void PeerConnected(object sender, PeerConnectedEvent e)
        {
            if (service.Whitelist.Count > 0)
                SendWhitelistRIDs(e.PeerId);
            if (service.Blacklist.Count > 0)
                SendBlacklistRIDs(e.PeerId);

            SendTGIDs(e.PeerId);
            SendDeactiveTGIDs(e.PeerId);

            service.SetupPeerDiagLog(e.PeerId);
        }

        /// <summary>
        /// Callback called when a peer disconnects.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        private void PeerDisconnected(uint peerId)
        {
            service.TearDownPeerDiagLog(peerId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="message"></param>
        public void ActivityTransferLog(uint peerId, string message)
        {
            service.WriteActivityLog(peerId, message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="message"></param>
        public void DiagnosticTransferLog(uint peerId, string message)
        {
            service.WritePeerDiagLog(peerId, message);
        }
    } // public class RouterMasterSystem
} // namespace fnerouter
