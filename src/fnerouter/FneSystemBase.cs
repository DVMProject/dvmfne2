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

using Serilog;

using fnecore;
using fnecore.DMR;

namespace fnerouter
{
    /// <summary>
    /// Represents the individual timeslot data status.
    /// </summary>
    public class SlotStatus
    {
        /// <summary>
        /// Rx Start Time
        /// </summary>
        public DateTime RxStart = DateTime.Now;

        /// <summary>
        /// 
        /// </summary>
        public uint RxPeerId = 0;

        /// <summary>
        /// 
        /// </summary>
        public uint RxSeq = 0;

        /// <summary>
        /// Rx RF Source
        /// </summary>
        public uint RxRFS = 0;
        /// <summary>
        /// Tx RF Source
        /// </summary>
        public uint TxRFS = 0;

        /// <summary>
        /// Rx Stream ID
        /// </summary>
        public uint RxStreamId = 0;
        /// <summary>
        /// Tx Stream ID
        /// </summary>
        public uint TxStreamId = 0;

        /// <summary>
        /// Rx TG ID
        /// </summary>
        public uint RxTGId = 0;
        /// <summary>
        /// Tx TG ID
        /// </summary>
        public uint TxTGId = 0;
        /// <summary>
        /// Tx Privacy TG ID
        /// </summary>
        public uint TxPITGId = 0;

        /// <summary>
        /// Rx Call Type
        /// </summary>
        public CallType RxCallType = CallType.GROUP;

        /// <summary>
        /// Rx Time
        /// </summary>
        public DateTime RxTime = DateTime.Now;
        /// <summary>
        /// Tx Time
        /// </summary>
        public DateTime TxTime = DateTime.Now;

        /// <summary>
        /// Rx Type
        /// </summary>
        public FrameType RxType = FrameType.TERMINATOR;

        /** DMR Data */
        /// <summary>
        /// Rx Link Control Header
        /// </summary>
        public LC DMR_RxLC = null;
        /// <summary>
        /// Rx Privacy Indicator Link Control Header
        /// </summary>
        public PrivacyLC DMR_RxPILC = null;
        /// <summary>
        /// Tx Link Control Header
        /// </summary>
        public LC DMR_TxHLC = null;
        /// <summary>
        /// Tx Privacy Link Control Header
        /// </summary>
        public PrivacyLC DMR_TxPILC = null;
        /// <summary>
        /// Tx Terminator Link Control
        /// </summary>
        public LC DMR_TxTLC = null;
    } // public class SlotStatus

    /// <summary>
    /// Implements a FNE system.
    /// </summary>
    public abstract partial class FneSystemBase
    {
        private const int P25_FIXED_SLOT = 2;
        private const int NXDN_FIXED_SLOT = 3;

        protected RouterService service;
        protected FneBase fne;

        private Dictionary<uint, Dictionary<byte, SlotStatus>> dmrCalls;
        private Dictionary<uint, SlotStatus> p25Calls;
        private Dictionary<uint, SlotStatus> nxdnCalls;
        private uint lastStreamId;

        protected RoutingRule rules;
        
        protected List<RoutingRuleGroupVoice> activeTGIDs;
        protected List<RoutingRuleGroupVoice> deactiveTGIDs;
        protected List<RoutingRuleGroupVoice> allowAffTGIDs;

        protected Dictionary<uint, Dictionary<uint, List<uint>>> groupAffiliatons;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the system name for this <see cref="FneSystemBase"/>.
        /// </summary>
        public string SystemName
        {
            get
            {
                if (fne != null)
                    return fne.SystemName;
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the peer ID for this <see cref="FneSystemBase"/>.
        /// </summary>
        public uint PeerId
        {
            get
            {
                if (fne != null)
                    return fne.PeerId;
                return uint.MaxValue;
            }
        }

        /// <summary>
        /// Flag indicating whether this <see cref="FneSystemBase"/> is running.
        /// </summary>
        public bool IsStarted
        { 
            get
            {
                if (fne != null)
                    return fne.IsStarted;
                return false;
            }
        }

        /// <summary>
        /// Gets the <see cref="FneType"/> this <see cref="FneBase"/> is.
        /// </summary>
        public FneType FneType
        {
            get
            {
                if (fne != null)
                    return fne.FneType;
                return FneType.UNKNOWN;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="FneSystemBase"/> class.
        /// </summary>
        /// <param name="service">Instance of the <see cref="RouterService"/> that owns this system</param>
        /// <param name="fne">Instance of <see cref="FneMaster"/> or <see cref="FnePeer"/></param>
        public FneSystemBase(RouterService service, FneBase fne)
        {
            this.service = service;
            this.fne = fne;

            this.rules = null;

            this.activeTGIDs = new List<RoutingRuleGroupVoice>();
            this.deactiveTGIDs = new List<RoutingRuleGroupVoice>();
            this.allowAffTGIDs = new List<RoutingRuleGroupVoice>();

            this.groupAffiliatons = new Dictionary<uint, Dictionary<uint, List<uint>>>();

            this.UpdateRoutingRules();

            // initialize call statuses
            this.dmrCalls = new Dictionary<uint, Dictionary<byte, SlotStatus>>();
            this.p25Calls = new Dictionary<uint, SlotStatus>();
            this.nxdnCalls = new Dictionary<uint, SlotStatus>();

            // hook various FNE network callbacks
            this.fne.DMRDataValidate = DMRDataValidate;
            this.fne.DMRDataReceived += DMRDataReceived;

            this.fne.P25DataValidate = P25DataValidate;
            this.fne.P25DataPreprocess += P25DataPreprocess;
            this.fne.P25DataReceived += P25DataReceived;

            this.fne.NXDNDataValidate = NXDNDataValidate;
            this.fne.NXDNDataReceived += NXDNDataReceived;

            this.fne.PeerIgnored = PeerIgnored;

            // hook logger callback
            this.fne.LogLevel = Program.FneLogLevel;
            this.fne.Logger = (LogLevel level, string message) =>
            {
                switch (level)
                {
                    case LogLevel.WARNING:
                        Log.Logger.Warning(message);
                        break;
                    case LogLevel.ERROR:
                        Log.Logger.Error(message);
                        break;
                    case LogLevel.DEBUG:
                        Log.Logger.Debug(message);
                        break;
                    case LogLevel.FATAL:
                        Log.Logger.Fatal(message);
                        break;
                    case LogLevel.INFO:
                    default:
                        Log.Logger.Information(message);
                        break;
                }
            };
        }

        /// <summary>
        /// Helper to update the routing rules for this system.
        /// </summary>
        public void UpdateRoutingRules()
        {
            this.rules = service.GetRules(this);
            if (this.rules != null)
            {
                if (this.rules.GroupVoice != null)
                {
                    this.activeTGIDs = this.rules.GroupVoice.FindAll((x) => x.Config.Active);
                    this.deactiveTGIDs = this.rules.GroupVoice.FindAll((x) => !x.Config.Active);
                    this.allowAffTGIDs = this.rules.GroupVoice.FindAll((x) => x.Config.Affiliated);
                }
            }
        }

        /// <summary>
        /// Starts the main execution loop for this <see cref="FneSystemBase"/>.
        /// </summary>
        public void Start()
        {
            if (!fne.IsStarted)
                fne.Start();
        }

        /// <summary>
        /// Stops the main execution loop for this <see cref="FneSystemBase"/>.
        /// </summary>
        public void Stop()
        {
            if (fne.IsStarted)
                fne.Stop();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        /// <param name="streamId"></param>
        public void UpdateGroupAff(uint peerId, uint srcId, uint dstId, uint streamId)
        {
            // make sure the peer exists in the affiliations table
            if (!groupAffiliatons.ContainsKey(peerId))
                groupAffiliatons.Add(peerId, new Dictionary<uint, List<uint>>());

            // remove the source RID from any other affiliated TGs
            RemoveGroupAff(peerId, srcId, streamId);

            // make sure the TGID exists in the affiliations table
            if (!groupAffiliatons[peerId].ContainsKey(dstId))
            {
                groupAffiliatons[peerId].Add(dstId, new List<uint>());
                Log.Logger.Information($"({SystemName}) PEER {peerId} Added TGID {dstId} from affiliations table [STREAM ID {streamId}]");
            }

            // add source RID to the affiliated TGs
            groupAffiliatons[peerId][dstId].Add(srcId);
            Log.Logger.Information($"({SystemName}) PEER {peerId} Added SRC_ID {srcId} affiliation from TGID {dstId} [STREAM ID {streamId}]");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="srcId"></param>
        /// <param name="streamId"></param>
        public void RemoveGroupAff(uint peerId, uint srcId, uint streamId)
        {
            // make sure the peer exists in the affiliations table
            if (!groupAffiliatons.ContainsKey(peerId))
                groupAffiliatons.Add(peerId, new Dictionary<uint, List<uint>>());

            // iterate through affiliations and perform affiliation clean up
            List<uint> tgsToRemove = new List<uint>();
            foreach (uint dstId in groupAffiliatons[peerId].Keys)
            {
                if (groupAffiliatons[peerId][dstId].Contains(srcId))
                {
                    int idx = groupAffiliatons[peerId][dstId].IndexOf(srcId);
                    groupAffiliatons[peerId][dstId].RemoveAt(idx);
                    Log.Logger.Information($"({SystemName}) PEER {peerId} Removed SRC_ID {srcId} affiliation from TGID {dstId} [STREAM ID {streamId}]");

                    // if there are no more affiliations delete the TG from the affiliations table
                    if (groupAffiliatons[peerId][dstId].Count == 0)
                        tgsToRemove.Add(dstId);
                }
            }

            // remove TGs with no affiliations
            if (tgsToRemove.Count > 0)
            {
                foreach (uint dstId in tgsToRemove)
                {
                    groupAffiliatons[peerId].Remove(dstId);
                    Log.Logger.Information($"({SystemName}) PEER {peerId} Removed TGID {dstId} from affiliations table [STREAM ID {streamId}]");
                }
            }
        }

        /// <summary>
        /// Callback used to process whether or not a peer is being ignored for traffic.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="slot">Slot Number</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="dataType">DMR Data Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <returns>True, if peer is ignored, otherwise false.</returns>
        protected virtual bool PeerIgnored(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId)
        {
            // U2U call is always passed...
            if (callType == CallType.PRIVATE)
                return false;

            RoutingRuleGroupVoice groupVoice = rules?.GroupVoice.Find((x) => x.Source.Tgid == dstId);
            if (groupVoice != null)
            {
                if (groupVoice.Config.Ignored.Count > 0)
                {
                    // if the group voice ignored list contains a peer ID of 0; we treat this talkgroup
                    // as requiring affiliations to route traffic
                    if (groupVoice.Config.Affiliated && groupVoice.Config.Ignored.Contains(0))
                    {
                        // check affiliations against this group
                        RoutingRuleGroupVoice aff = allowAffTGIDs.Find((x) => x.Source.Tgid == dstId);
                        if (aff != null)
                        {
                            if (groupAffiliatons.ContainsKey(peerId))
                                if (groupAffiliatons[peerId].ContainsKey(dstId))
                                    if (groupAffiliatons[peerId][dstId].Count > 0)
                                        return false;
                        }
                    }
                    else
                    {
                        if (!groupVoice.Config.Ignored.Contains((int)peerId))
                            return false;
                    }

                    // is this a new stream?
                    if (streamId != lastStreamId)
                    {
                        lastStreamId = streamId;
                        Log.Logger.Warning($"({SystemName}) Traffic *REJECT ACL      * PEER {peerId} SRC_ID {srcId} DST_ID {dstId} [STREAM ID {streamId}] (Ignored Peer)");
                        // TODO TODO TODO: Implement reporting API
                    }

                    return true;
                }
            }

            return false;
        }
    } // public abstract partial class FneSystemBase
} // namespace fnerouter
