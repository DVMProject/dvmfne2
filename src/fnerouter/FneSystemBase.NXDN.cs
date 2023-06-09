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

using Serilog;

using fnecore;
using fnecore.NXDN;
using System.Collections.Generic;

namespace fnerouter
{
    /// <summary>
    /// Implements a FNE system base.
    /// </summary>
    public abstract partial class FneSystemBase
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Callback used to validate incoming NXDN data.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="messageType">NXDN Message Type</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected virtual bool NXDNDataValidate(uint peerId, uint srcId, uint dstId, CallType callType, NXDNMessageType messageType, FrameType frameType, uint streamId)
        {
            DateTime pktTime = DateTime.Now;

            if (service.Blacklist.Find((x) => x.Id == srcId) != null)
            {
                if (streamId == status[NXDN_FIXED_SLOT].RxStreamId)
                {
                    // mark status variables for use later
                    status[NXDN_FIXED_SLOT].RxStart = pktTime;
                    status[NXDN_FIXED_SLOT].RxPeerId = peerId;
                    status[NXDN_FIXED_SLOT].RxRFS = srcId;
                    status[NXDN_FIXED_SLOT].RxType = frameType;
                    status[NXDN_FIXED_SLOT].RxTGId = dstId;
                    status[NXDN_FIXED_SLOT].RxStreamId = streamId;
                    Log.Logger.Warning($"({SystemName}) NXDD: Traffic *REJECT ACL      * PEER {peerId} SRC_ID {srcId} DST_ID {dstId} [STREAM ID {streamId}] (Blacklisted RID)");
    
                    // TODO TODO TODO
                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",peerId.ToString()},{"SRC_ID",srcId.ToString()},{"DST_ID",dstId.ToString()},{"STREAM ID",streamId.ToString()},{"Value","BLACKLISTED_RID"}});
                }

                return false;
            }

            // always validate a terminator if the source is valid
            if (messageType == NXDNMessageType.MESSAGE_TYPE_TX_REL)
                return true;

            if (callType == CallType.GROUP)
            {
                if (rules.SendTgid && (activeTGIDs.Find((x) => x.Source.Tgid == dstId) == null))
                {
                    if (streamId == status[NXDN_FIXED_SLOT].RxStreamId)
                    {
                        // mark status variables for use later
                        status[NXDN_FIXED_SLOT].RxStart = pktTime;
                        status[NXDN_FIXED_SLOT].RxPeerId = peerId;
                        status[NXDN_FIXED_SLOT].RxRFS = srcId;
                        status[NXDN_FIXED_SLOT].RxType = frameType;
                        status[NXDN_FIXED_SLOT].RxTGId = dstId;
                        status[NXDN_FIXED_SLOT].RxStreamId = streamId;
                        Log.Logger.Warning($"({SystemName}) NXDD: Traffic *REJECT ACL      * PEER {peerId} SRC_ID {srcId} DST_ID {dstId} [STREAM ID {streamId}] (Illegal TGID)");
    
                        //Send report to reporter server.
                        FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",peerId.ToString()},{"SRC_ID",srcId.ToString()},{"DST_ID",dstId.ToString()},{"STREAM ID",streamId.ToString()},{"Value","ILLEGAL_TGID"}});
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Event handler used to process incoming NXDN data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void NXDNDataReceived(object sender, NXDNDataReceivedEvent e)
        {
            DateTime pktTime = DateTime.Now;

            // override call type if necessary
            if ((e.MessageType == NXDNMessageType.MESSAGE_TYPE_TX_REL) && (status[NXDN_FIXED_SLOT].RxType != FrameType.TERMINATOR))
            {
                if (status[NXDN_FIXED_SLOT].RxCallType != e.CallType)
                    status[NXDN_FIXED_SLOT].RxCallType = e.CallType;
            }

            if (e.CallType == CallType.GROUP)
            {
                // is this a new call stream?
                if (e.StreamId != status[NXDN_FIXED_SLOT].RxStreamId && (e.MessageType != NXDNMessageType.MESSAGE_TYPE_TX_REL))
                {
                    if ((status[NXDN_FIXED_SLOT].RxType != FrameType.TERMINATOR) && (pktTime < status[NXDN_FIXED_SLOT].RxTime.AddSeconds(Constants.STREAM_TO)) &&
                        (status[NXDN_FIXED_SLOT].RxRFS != e.SrcId))
                    {
                        Log.Logger.Warning($"({SystemName}) NXDD: Traffic *CALL COLLISION  * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} [STREAM ID {e.StreamId}] (Collided with existing call)");
                        //Send report to reporter server.
                        FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"TGID",e.DstId.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","COLLISION_EXISTING"}});
                        return;
                    }

                    // this is a new call stream
                    status[NXDN_FIXED_SLOT].RxStart = pktTime;
                    Log.Logger.Information($"({SystemName}) NXDD: Traffic *CALL START      * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} [STREAM ID {e.StreamId}]");
                    //Send report to reporter server.
                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"TGID",e.DstId.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","CALL_START"}});

                    status[NXDN_FIXED_SLOT].RxCallType = CallType.GROUP;
                }

                // find the group voice rule by e.DstId, slot and whether or not the rule is active and routable
                RoutingRuleGroupVoice groupVoice = rules.GroupVoice.Find((x) => x.Source.Tgid == e.DstId && x.Config.Active && x.Config.Routable);
                if (groupVoice != null)
                {
                    for (int i = 0; i < groupVoice.Destination.Count; i++)
                    {
                        RoutingRuleGroupVoiceDestination target = groupVoice.Destination[i];
                        FneSystemBase tgtSystem = service.Systems.Find((x) => x.SystemName.ToUpperInvariant() == target.Network.ToUpperInvariant());
                        if (tgtSystem != null)
                        {
                            /*
                            ** Contention Handling
                            */

                            // from a different group than last RX from this system, but it has been less than Group Hangtime
                            if ((target.Tgid != tgtSystem.status[NXDN_FIXED_SLOT].RxTGId) && (pktTime - tgtSystem.status[NXDN_FIXED_SLOT].RxTime < new TimeSpan(0, 0, rules.GroupHangTime)))
                            {
                                Log.Logger.Information($"({SystemName}) NXDD: Call not routed to TGID {target.Tgid}, target active or in group hangtime: PEER {tgtSystem.PeerId} TGID {tgtSystem.status[NXDN_FIXED_SLOT].RxTGId}");
                                //Send report to reporter server.
                                FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"TARGET_TGID",target.Tgid.ToString()},{"PEER",tgtSystem.PeerId.ToString()},{"TGID",tgtSystem.status[NXDN_FIXED_SLOT].RxTGId.ToString()},{"Value","CALL_NOT_ROUTED_HANGTIME"}});
                                continue;
                            }

                            // from a different group than last TX to this system, but it has been less than Group Hangtime
                            if ((target.Tgid != tgtSystem.status[NXDN_FIXED_SLOT].TxTGId) && (pktTime - tgtSystem.status[NXDN_FIXED_SLOT].TxTime < new TimeSpan(0, 0, rules.GroupHangTime)))
                            {
                                Log.Logger.Information($"({SystemName}) NXDD: Call not routed to TGID {target.Tgid}, target in group hangtime: PEER {tgtSystem.PeerId} TGID {tgtSystem.status[NXDN_FIXED_SLOT].TxTGId}");
                                //Send report to reporter server.
                                FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"TARGET_TGID",target.Tgid.ToString()},{"PEER",tgtSystem.PeerId.ToString()},{"TGID",tgtSystem.status[NXDN_FIXED_SLOT].TxTGId.ToString()},{"Value","CALL_NOT_ROUTED_HANGTIME"}});
                                continue;
                            }

                            // from the same group as the last RX from this system, but from a different subscriber, and it has been less than stream timeout
                            if ((target.Tgid != tgtSystem.status[NXDN_FIXED_SLOT].RxTGId) && (e.SrcId != tgtSystem.status[NXDN_FIXED_SLOT].RxRFS) && (pktTime - tgtSystem.status[target.Slot].RxTime < new TimeSpan(0, 0, 0, 0, (int)(Constants.STREAM_TO * 1000))))
                            {
                                Log.Logger.Information($"({SystemName}) NXDD: Call not routed to TGID {target.Tgid}, matching call already active on target: PEER {tgtSystem.PeerId} TGID {tgtSystem.status[NXDN_FIXED_SLOT].TxTGId} SRC_ID {tgtSystem.status[NXDN_FIXED_SLOT].TxRFS}");
                                //Send report to reporter server.
                                FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"TARGET_TGID",target.Tgid.ToString()},{"PEER",tgtSystem.PeerId.ToString()},{"TGID",tgtSystem.status[NXDN_FIXED_SLOT].TxTGId.ToString()},{"SRC_ID",tgtSystem.status[NXDN_FIXED_SLOT].TxRFS.ToString()},{"Value","CALL_NOT_ROUTED_HANGTIME"}});
                                continue;
                            }

                            // from the same group as the last TX to this system, but from a different subscriber, and it has been less than stream timeout
                            if ((target.Tgid != tgtSystem.status[NXDN_FIXED_SLOT].TxTGId) && (e.SrcId != tgtSystem.status[NXDN_FIXED_SLOT].TxRFS) && (pktTime - tgtSystem.status[target.Slot].RxTime < new TimeSpan(0, 0, 0, 0, (int)(Constants.STREAM_TO * 1000))))
                            {
                                Log.Logger.Information($"({SystemName}) NXDD: Call not routed to TGID {target.Tgid}, call route in progress on target: PEER {tgtSystem.PeerId} TGID {tgtSystem.status[NXDN_FIXED_SLOT].TxTGId} SRC_ID {tgtSystem.status[NXDN_FIXED_SLOT].TxRFS}");
                                //Send report to reporter server.
                                FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"TARGET_TGID",target.Tgid.ToString()},{"PEER",tgtSystem.PeerId.ToString()},{"TGID",tgtSystem.status[NXDN_FIXED_SLOT].TxTGId.ToString()},{"SRC_ID",tgtSystem.status[NXDN_FIXED_SLOT].TxRFS.ToString()},{"Value","CALL_NOT_ROUTED_HANGTIME"}});
                                continue;
                            }

                            // set values for the contention handler to test next time
                            tgtSystem.status[NXDN_FIXED_SLOT].TxTime = pktTime;

                            if ((e.StreamId != status[NXDN_FIXED_SLOT].RxStreamId) || (tgtSystem.status[NXDN_FIXED_SLOT].TxRFS != e.SrcId) || (tgtSystem.status[NXDN_FIXED_SLOT].TxTGId != target.Tgid))
                            {
                                // record the destination TGID and stream ID
                                tgtSystem.status[NXDN_FIXED_SLOT].TxTGId = target.Tgid;
                                tgtSystem.status[NXDN_FIXED_SLOT].TxPITGId = 0;
                                tgtSystem.status[NXDN_FIXED_SLOT].TxStreamId = e.StreamId;
                                tgtSystem.status[NXDN_FIXED_SLOT].TxRFS = e.SrcId;

                                Log.Logger.Information($"({SystemName}) NXDD: Call routed to SYSTEM {target.Network} TGID {target.Tgid}");
                                //Send report to reporter server.
                                FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"TARGET_SYSTEM",target.Network.ToString()},{"TARGET_TGID",target.Tgid.ToString()},{"Value","CALL_ROUTED"}});
                            }

                            byte[] frame = new byte[e.Data.Length];
                            Buffer.BlockCopy(e.Data, 0, frame, 0, e.Data.Length);

                            // re-write destination TGID in the frame
                            frame[8] = (byte)((target.Tgid >> 16) & 0xFF);
                            frame[9] = (byte)((target.Tgid >> 8) & 0xFF);
                            frame[10] = (byte)((target.Tgid >> 0) & 0xFF);

                            // what type of FNE are we?
                            if (fne.FneType == FneType.MASTER)
                            {
                                FneMaster master = (FneMaster)fne;
                                master.SendPeer(tgtSystem.PeerId, FneBase.CreateOpcode(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_NXDN), frame, e.PacketSequence);
                            }
                            else if (fne.FneType == FneType.PEER)
                            {
                                FnePeer peer = (FnePeer)fne;
                                peer.SendMaster(FneBase.CreateOpcode(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_NXDN), frame, e.PacketSequence);
                            }

                            Log.Logger.Debug($"({SystemName}) NXDN Packet routed by rule {groupVoice.Name} to SYSTEM {tgtSystem.SystemName}");
                        }
                    }
                }

                // final actions - is this a voice terminator?
                if ((e.MessageType == NXDNMessageType.MESSAGE_TYPE_TX_REL) && (status[NXDN_FIXED_SLOT].RxType != FrameType.TERMINATOR))
                {
                    TimeSpan callDuration = pktTime - status[NXDN_FIXED_SLOT].RxStart;
                    Log.Logger.Information($"({SystemName}) NXDD: Traffic *CALL END        * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} DUR {callDuration.TotalSeconds} [STREAM ID: {e.StreamId}]");
                    //Send report to reporter server.
                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"TGID",e.DstId.ToString()},{"DUR",callDuration.TotalSeconds.ToString()},{"STREAM_ID",e.StreamId.ToString()},{"Value","CALL_END"}});
                }

                status[NXDN_FIXED_SLOT].RxPeerId = e.PeerId;
                status[NXDN_FIXED_SLOT].RxRFS = e.SrcId;
                status[NXDN_FIXED_SLOT].RxType = e.FrameType;
                status[NXDN_FIXED_SLOT].RxTGId = e.DstId;
                status[NXDN_FIXED_SLOT].RxTime = pktTime;
                status[NXDN_FIXED_SLOT].RxStreamId = e.StreamId;
            }
            else if (e.CallType == CallType.PRIVATE)
            {
                // is this a new call stream?
                if (e.StreamId != status[NXDN_FIXED_SLOT].RxStreamId)
                {
                    if ((status[NXDN_FIXED_SLOT].RxType != FrameType.TERMINATOR) && (pktTime < status[NXDN_FIXED_SLOT].RxTime.AddSeconds(Constants.STREAM_TO)) &&
                        (status[NXDN_FIXED_SLOT].RxRFS != e.SrcId))
                    {
                        Log.Logger.Warning($"({SystemName}) NXDD: Traffic *CALL COLLISION  * PEER {e.PeerId} SRC_ID {e.SrcId} DST_ID {e.DstId} [STREAM ID {e.StreamId}] (Collided with existing call)");
                        //Send report to reporter server.
                        FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"DST_ID",e.DstId.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","COLLISION"}});
                        return;
                    }

                    // this is a new call stream
                    status[NXDN_FIXED_SLOT].RxStart = pktTime;
                    Log.Logger.Information($"({SystemName}) NXDD: Traffic *PRV CALL START  * PEER {e.PeerId} SRC_ID {e.SrcId} DST_ID {e.DstId} [STREAM ID {e.StreamId}]");
                    //Send report to reporter server.

                    status[NXDN_FIXED_SLOT].RxCallType = e.CallType;
                }

                // final actions - is this a voice terminator?
                if ((e.MessageType == NXDNMessageType.MESSAGE_TYPE_TX_REL) && (status[NXDN_FIXED_SLOT].RxType != FrameType.TERMINATOR))
                {
                    TimeSpan callDuration = pktTime - status[NXDN_FIXED_SLOT].RxStart;
                    Log.Logger.Information($"({SystemName}) NXDD: Traffic *PRV CALL END    * PEER {e.PeerId} SRC_ID {e.SrcId} DST_ID {e.DstId} DUR {callDuration.TotalSeconds} [STREAM ID: {e.StreamId}]");
                    //Send report to reporter server.
                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"DST_ID",e.DstId.ToString()},{"DUR",callDuration.TotalSeconds.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","PRIVATE_CALL_END"}});
                }

                status[NXDN_FIXED_SLOT].RxPeerId = e.PeerId;
                status[NXDN_FIXED_SLOT].RxRFS = e.SrcId;
                status[NXDN_FIXED_SLOT].RxType = e.FrameType;
                status[NXDN_FIXED_SLOT].RxTGId = e.DstId;
                status[NXDN_FIXED_SLOT].RxTime = pktTime;
                status[NXDN_FIXED_SLOT].RxStreamId = e.StreamId;
            }
        }
    } // public abstract partial class FneSystemBase
} // namespace fnerouter
