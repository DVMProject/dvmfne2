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
using fnecore.DMR;
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
        /// Callback used to validate incoming DMR data.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="slot">Slot Number</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="dataType">DMR Data Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected virtual bool DMRDataValidate(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId)
        {
            DateTime pktTime = DateTime.Now;

            SlotStatus status = new SlotStatus();
            if (dmrCalls.ContainsKey(dstId))                
                if (dmrCalls[dstId].ContainsKey(slot))
                    status = dmrCalls[dstId][slot];

            if (service.Blacklist.Find((x) => x.Id == srcId) != null)
            {
                if (streamId == status.RxStreamId)
                {
                    // mark status variables for use later
                    status.RxStart = pktTime;
                    status.RxPeerId = peerId;
                    status.RxRFS = srcId;
                    status.RxType = frameType;
                    status.RxTGId = dstId;
                    status.RxStreamId = streamId;

                    Log.Logger.Warning($"({SystemName}) DMRD: Traffic *REJECT ACL      * PEER {peerId} SRC_ID {srcId} DST_ID {dstId} [STREAM ID {streamId}] (Blacklisted RID)");

                    // send report to monitor server
                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",peerId.ToString()},{"SRC_ID",srcId.ToString()},{"DST_ID",dstId.ToString()},{"STREAM ID",streamId.ToString()},{"Value","BLACKLISTED_RID"}});
                }

                return false;
            }

            // always validate a terminator if the source is valid
            if ((frameType == FrameType.DATA_SYNC) && (dataType == DMRDataType.TERMINATOR_WITH_LC))
                return true;

            if (callType == CallType.GROUP)
            {
                if (rules.SendTgid && (activeTGIDs.Find((x) => x.Source.Tgid == dstId) == null))
                {
                    if (streamId == status.RxStreamId)
                    {
                        // mark status variables for use later
                        status.RxStart = pktTime;
                        status.RxPeerId = peerId;
                        status.RxRFS = srcId;
                        status.RxType = frameType;
                        status.RxTGId = dstId;
                        status.RxStreamId = streamId;

                        Log.Logger.Warning($"({SystemName}) DMRD: Traffic *REJECT ACL      * PEER {peerId} SRC_ID {srcId} DST_ID {dstId} [STREAM ID {streamId}] (Illegal TGID)");
    
                        // send report to monitor server
                        FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",peerId.ToString()},{"SRC_ID",srcId.ToString()},{"DST_ID",dstId.ToString()},{"STREAM ID",streamId.ToString()},{"Value","ILLEGAL_TGID"}});
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Event handler used to process incoming DMR data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void DMRDataReceived(object sender, DMRDataReceivedEvent e)
        {
            DateTime pktTime = DateTime.Now;

            byte[] dmrpkt = new byte[33];
            Buffer.BlockCopy(e.Data, 20, dmrpkt, 0, 33);
            byte bits = e.Data[15];

            SlotStatus status = new SlotStatus();
            if (dmrCalls.ContainsKey(e.DstId))
                if (dmrCalls[e.DstId].ContainsKey(e.Slot))
                    status = dmrCalls[e.DstId][e.Slot];

            if ((e.FrameType == FrameType.DATA_SYNC) && ((e.DataType == DMRDataType.DATA_HEADER) || (e.DataType == DMRDataType.RATE_12_DATA) ||
                (e.DataType == DMRDataType.RATE_34_DATA) || (e.DataType == DMRDataType.RATE_1_DATA)))
            {
                Log.Logger.Information($"({SystemName}) DMRD: Traffic *DATA            * PEER {e.PeerId} SRC_ID {e.SrcId} DST_ID {e.DstId} TS {e.Slot + 1} [STREAM ID {e.StreamId}]");
                // send report to monitor server
                FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"DST_ID",e.DstId.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","DMR_DATA"}});
                return;
            }

            if (e.CallType == CallType.GROUP)
            {
                // is this a new call stream?
                if (e.StreamId != status.RxStreamId)
                {
                    if ((status.RxType != FrameType.TERMINATOR) && (pktTime < status.RxTime.AddSeconds(Constants.STREAM_TO)) &&
                        (status.RxRFS != e.SrcId))
                    {
                        Log.Logger.Warning($"({SystemName}) DMRD: Traffic *CALL COLLISION  * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} TS {e.Slot + 1} [STREAM ID {e.StreamId}] (Collided with existing call)");
                        // send report to monitor server
                        FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"DST_ID",e.DstId.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","COLLISION_EXISTING"}});
                        return;
                    }

                    // this is a new call stream
                    status.RxStart = pktTime;
                    Log.Logger.Information($"({SystemName}) DMRD: Traffic *CALL START      * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} TS {e.Slot + 1} [STREAM ID {e.StreamId}]");
                    // send report to monitor server
                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"DST_ID",e.DstId.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","CALL_STARTED"}});

                    status.RxCallType = e.CallType;

                    // if we can, use the LC from the voice header as to keep all options intact
                    if ((e.FrameType == FrameType.DATA_SYNC) && (e.DataType == DMRDataType.VOICE_LC_HEADER))
                    {
                        LC lc = FullLC.Decode(dmrpkt, DMRDataType.VOICE_LC_HEADER);
                        status.DMR_RxLC = lc;
                    }
                    else // if we don't have a voice header; don't wait to decode it, just make a dummy header
                        status.DMR_RxLC = new LC()
                        {
                            SrcId = e.SrcId,
                            DstId = e.DstId
                        };

                    status.DMR_RxPILC = new PrivacyLC();
                    Log.Logger.Debug($"({SystemName}) TS {e.Slot + 1} [STREAM ID {e.StreamId}] RX_LC {FneUtils.HexDump(status.DMR_RxLC.GetBytes())}");
                }

                // if we can, use the PI LC from the PI voice header as to keep all options intact
                if ((e.FrameType == FrameType.DATA_SYNC) && (e.DataType == DMRDataType.VOICE_PI_HEADER))
                {
                    PrivacyLC lc = FullLC.DecodePI(dmrpkt);
                    status.DMR_RxPILC = lc;
                    Log.Logger.Information($"({SystemName}) DMRD: Traffic *CALL PI PARAMS  * PEER {e.PeerId} DST_ID {e.DstId} TS {e.Slot + 1} ALGID {lc.AlgId} KID {lc.KId} [STREAM ID {e.StreamId}]");
                    Log.Logger.Debug($"({SystemName}) TS {e.Slot + 1} [STREAM ID {e.StreamId}] RX_PI_LC {FneUtils.HexDump(status.DMR_RxPILC.GetBytes())}");
                }

                // find the group voice rule by e.DstId, e.Slot and whether or not the rule is active and routable
                RoutingRuleGroupVoice groupVoice = rules.GroupVoice.Find((x) => x.Source.Tgid == e.DstId && x.Source.Slot == e.Slot && x.Config.Active && x.Config.Routable);
                if (groupVoice != null)
                {
                    for (int i = 0; i < groupVoice.Destination.Count; i++)
                    {
                        RoutingRuleGroupVoiceDestination target = groupVoice.Destination[i];
                        FneSystemBase tgtSystem = service.Systems.Find((x) => x.SystemName.ToUpperInvariant() == target.Network.ToUpperInvariant());
                        if (tgtSystem != null)
                        {
                            if (tgtSystem.SystemName.ToUpperInvariant() == SystemName.ToUpperInvariant()) 
                            {
                                Log.Logger.Error($"({SystemName}) DMRD: Call not routed, cowardly refusing to route a call to ourselves.");
                                continue;
                            }

                            SlotStatus tgtStatus = null;
                            if (tgtSystem.dmrCalls.ContainsKey(target.Tgid)) 
                                if (tgtSystem.dmrCalls[target.Tgid].ContainsKey(target.Slot))
                                    tgtStatus = tgtSystem.nxdnCalls[target.Tgid];
                            if (tgtStatus == null) 
                                tgtStatus = new SlotStatus();

                            /*
                            ** Contention Handling
                            */

                            // from a different group than last RX from this system, but it has been less than Group Hangtime
                            if ((target.Tgid != tgtStatus.RxTGId) && (pktTime - tgtStatus.RxTime < new TimeSpan(0, 0, rules.GroupHangTime)))
                                if (e.FrameType == FrameType.DATA_SYNC && e.DataType == DMRDataType.VOICE_LC_HEADER)
                                {
                                    Log.Logger.Information($"({SystemName}) DMRD: Call not routed to TGID {target.Tgid}, target active or in group hangtime: PEER {tgtSystem.PeerId} TS {target.Slot + 1} TGID {tgtStatus.RxTGId}");
                                    // send report to monitor server
                                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"DST_ID",e.DstId.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","CALL_NOT_ROUTED_HANGTIME"}});
                                    continue;
                                }

                            // from a different group than last TX to this system, but it has been less than Group Hangtime
                            if ((target.Tgid != tgtStatus.TxTGId) && (pktTime - tgtStatus.TxTime < new TimeSpan(0, 0, rules.GroupHangTime)))
                                if (e.FrameType == FrameType.DATA_SYNC && e.DataType == DMRDataType.VOICE_LC_HEADER)
                                {
                                    Log.Logger.Information($"({SystemName}) DMRD: Call not routed to TGID {target.Tgid}, target in group hangtime: PEER {tgtSystem.PeerId} TS {target.Slot + 1} TGID {tgtStatus.TxTGId}");
                                    // send report to monitor server
                                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"TARGET_TGID",target.Tgid.ToString()},{"PEER",tgtSystem.PeerId.ToString()},{"TS",(target.Slot+1).ToString()},{"TGID",tgtStatus.TxTGId.ToString()},{"Value","CALL_NOT_ROUTED_HANGTIME"}});
                                    continue;
                                }

                            // from the same group as the last RX from this system, but from a different subscriber, and it has been less than stream timeout
                            if ((target.Tgid != tgtStatus.RxTGId) && (e.SrcId != tgtStatus.RxRFS) && (pktTime - tgtStatus.RxTime < new TimeSpan(0, 0, 0, 0, (int)(Constants.STREAM_TO * 1000))))
                                if (e.FrameType == FrameType.DATA_SYNC && e.DataType == DMRDataType.VOICE_LC_HEADER)
                                {
                                    Log.Logger.Information($"({SystemName}) DMRD: Call not routed to TGID {target.Tgid}, matching call already active on target: PEER {tgtSystem.PeerId} TS {target.Slot + 1} TGID {tgtStatus.TxTGId} SRC_ID {tgtStatus.TxRFS}");
                                    // send report to monitor server
                                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"TARGET_TGID",target.Tgid.ToString()},{"PEER",tgtSystem.PeerId.ToString()},{"TS",(target.Slot+1).ToString()},{"TGID",tgtStatus.TxTGId.ToString()},{"SRC_ID",tgtStatus.TxRFS.ToString()},{"Value","CALL_NOT_ROUTED_CALLONTARGET"}});
                                    continue;
                                }

                            // from the same group as the last TX to this system, but from a different subscriber, and it has been less than stream timeout
                            if ((target.Tgid != tgtStatus.TxTGId) && (e.SrcId != tgtStatus.TxRFS) && (pktTime - tgtStatus.RxTime < new TimeSpan(0, 0, 0, 0, (int)(Constants.STREAM_TO * 1000))))
                                if (e.FrameType == FrameType.DATA_SYNC && e.DataType == DMRDataType.VOICE_LC_HEADER)
                                {
                                    Log.Logger.Information($"({SystemName}) DMRD: Call not routed to TGID {target.Tgid}, call route in progress on target: PEER {tgtSystem.PeerId} TS {target.Slot + 1} TGID {tgtStatus.TxTGId} SRC_ID {tgtStatus.TxRFS}");
                                    // send report to monitor server
                                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"TARGET_TGID",target.Tgid.ToString()},{"PEER",tgtSystem.PeerId.ToString()},{"TS",(target.Slot+1).ToString()},{"TGID",tgtStatus.TxTGId.ToString()},{"SRC_ID",tgtStatus.TxRFS.ToString()},{"Value","CALL_NOT_ROUTED_CALLINPROGRESS"}});
                                    continue;
                                }

                            // set values for the contention handler to test next time
                            tgtStatus.TxTime = pktTime;

                            if ((e.StreamId != status.RxStreamId) || (tgtStatus.TxRFS != e.SrcId) || (tgtStatus.TxTGId != target.Tgid))
                            {
                                // record the destination TGID and stream ID
                                tgtStatus.TxTGId = target.Tgid;
                                tgtStatus.TxPITGId = 0;
                                tgtStatus.TxStreamId = e.StreamId;
                                tgtStatus.TxRFS = e.SrcId;

                                // generate full LCs (full and EMB) for Tx stream
                                LC dstLC = status.DMR_RxLC;
                                dstLC.DstId = target.Tgid;
                                dstLC.SrcId = e.SrcId;

                                tgtStatus.DMR_TxHLC = dstLC;
                                tgtStatus.DMR_TxTLC = dstLC;

                                PrivacyLC dstPILC = status.DMR_RxPILC;
                                dstPILC.DstId = target.Tgid;

                                tgtStatus.DMR_TxPILC = dstPILC;

                                if (tgtSystem.dmrCalls.ContainsKey(target.Tgid)) 
                                {
                                    if (tgtSystem.dmrCalls.ContainsKey(target.Slot))
                                        tgtSystem.dmrCalls[target.Tgid][target.Slot] = status;
                                    else
                                        tgtSystem.dmrCalls[target.Tgid].Add(target.Slot, status);
                                }
                                else
                                    tgtSystem.dmrCalls.Add(target.Tgid, new Dictionary<byte, SlotStatus>{ { target.Slot, status  } });


                                Log.Logger.Debug($"({SystemName}) TS {e.Slot + 1} [STREAM ID {e.StreamId}] TX_H_LC {FneUtils.HexDump(tgtStatus.DMR_TxHLC.GetBytes())}");
                                Log.Logger.Debug($"({SystemName}) TS {e.Slot + 1} [STREAM ID {e.StreamId}] TX_PI_LC {FneUtils.HexDump(tgtStatus.DMR_TxPILC.GetBytes())}");
                                Log.Logger.Debug($"({SystemName}) DMR Packet DST TGID {target.Tgid} does not match SRC TGID {e.DstId} - Generating FULL and EMB LCs");
                                Log.Logger.Information($"({SystemName}) DMRD: Call routed to SYSTEM {target.Network} TS {target.Slot} TGID {target.Tgid}");
                                // send report to monitor server
                                FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"TARGET_SYSTEM",target.Network.ToString()},{"TS",target.Slot.ToString()},{"TGID",target.Tgid.ToString()},{"Value","CALL_ROUTED"}});
                            }

                            uint piDstId = status.DMR_RxPILC.DstId;
                            if ((piDstId != 0) && (tgtStatus.TxPITGId != target.Tgid))
                            {
                                // record the destination TGID
                                tgtStatus.TxPITGId = target.Tgid;

                                // generate full LCs (full and EMB) for Tx stream
                                PrivacyLC dstPiLC = status.DMR_RxPILC;
                                dstPiLC.DstId = target.Tgid;

                                tgtStatus.DMR_TxPILC = dstPiLC;
                                Log.Logger.Debug($"({SystemName}) TS {e.Slot + 1} [STREAM ID {e.StreamId}] TX_PI_LC {FneUtils.HexDump(tgtStatus.DMR_TxPILC.GetBytes())}");
                                Log.Logger.Information($"({SystemName}) DMRD: Call PI parameters routed to SYSTEM {target.Network} TS {target.Slot} TGID {target.Tgid}");
                            }

                            byte[] frame = new byte[e.Data.Length];
                            Buffer.BlockCopy(e.Data, 0, frame, 0, e.Data.Length);

                            // re-write destination TGID in the frame
                            frame[8] = (byte)((target.Tgid >> 16) & 0xFF);
                            frame[9] = (byte)((target.Tgid >> 8) & 0xFF);
                            frame[10] = (byte)((target.Tgid >> 0) & 0xFF);

                            // set or clear the e.Slot flag (if 0x80 is set Slot 2 otherwise Slot 1)
                            if (e.Slot == 1 && (frame[15] & 0x80) == 0x00)
                                frame[15] |= 0x80;
                            if (e.Slot == 0 && (frame[15] & 0x80) == 0x80)
                                frame[15] = (byte)(frame[15] & ~(0x80));

                            if (e.FrameType == FrameType.DATA_SYNC)
                            {
                                frame[15] |= (byte)(0x20 | (byte)e.DataType);

                                byte[] fullLC = null;
                                switch (e.DataType)
                                {
                                    case DMRDataType.VOICE_LC_HEADER:
                                        FullLC.Encode(tgtStatus.DMR_TxHLC, ref fullLC, DMRDataType.VOICE_LC_HEADER);
                                        break;
                                    case DMRDataType.VOICE_PI_HEADER:
                                        FullLC.EncodePI(tgtStatus.DMR_TxPILC, ref fullLC);
                                        break;
                                    case DMRDataType.TERMINATOR_WITH_LC:
                                        FullLC.Encode(tgtStatus.DMR_TxTLC, ref fullLC, DMRDataType.TERMINATOR_WITH_LC);
                                        break;
                                }

                                Buffer.BlockCopy(fullLC, 0, frame, 20, fullLC.Length);
                            }

                            // what type of FNE are we?
                            if (fne.FneType == FneType.MASTER)
                            {
                                FneMaster master = (FneMaster)fne;
                                master.SendPeer(tgtSystem.PeerId, FneBase.CreateOpcode(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_DMR), frame, e.PacketSequence);
                            }
                            else if (fne.FneType == FneType.PEER)
                            {
                                FnePeer peer = (FnePeer)fne;
                                peer.SendMaster(FneBase.CreateOpcode(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_DMR), frame, e.PacketSequence);
                            }

                            Log.Logger.Debug($"({SystemName}) DMR Packet routed by rule {groupVoice.Name} to SYSTEM {tgtSystem.SystemName}");
                        }
                    }
                }

                // final actions - is this a voice terminator?
                if ((e.FrameType == FrameType.DATA_SYNC) && (e.DataType == DMRDataType.TERMINATOR_WITH_LC) && (status.RxType != FrameType.TERMINATOR))
                {
                    TimeSpan callDuration = pktTime - status.RxStart;
                    Log.Logger.Information($"({SystemName}) DMRD: Traffic *CALL END        * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} TS {e.Slot + 1} DUR {callDuration.TotalSeconds} [STREAM ID: {e.StreamId}]");
                    // send report to monitor server
                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"TGID",e.DstId.ToString()},{"TS",(e.Slot+1).ToString()},{"DUR",callDuration.TotalSeconds.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","CALL_END"}});                

                    if (dmrCalls.ContainsKey(e.DstId)) 
                        if (dmrCalls.ContainsKey(e.Slot))
                            dmrCalls.Remove(e.DstId);
                }

                status.RxPeerId = e.PeerId;
                status.RxRFS = e.SrcId;
                status.RxType = e.FrameType;
                status.RxTGId = e.DstId;
                status.RxTime = pktTime;
                status.RxStreamId = e.StreamId;

                if (dmrCalls.ContainsKey(e.DstId)) 
                {
                    if (dmrCalls.ContainsKey(e.Slot))
                        dmrCalls[e.DstId][e.Slot] = status;
                }
            }
            else if (e.CallType == CallType.PRIVATE)
            {
                // is this a new call stream?
                if (e.StreamId != status.RxStreamId)
                {
                    if ((status.RxType != FrameType.TERMINATOR) && (pktTime < status.RxTime.AddSeconds(Constants.STREAM_TO)) &&
                        (status.RxRFS != e.SrcId))
                    {
                        Log.Logger.Warning($"({SystemName}) DMRD: Traffic *CALL COLLISION  * PEER {e.PeerId} SRC_ID {e.SrcId} DST_ID {e.DstId} TS {e.Slot + 1} [STREAM ID {e.StreamId}] (Collided with existing call)");
                        // send report to monitor server
                        FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"DST_ID",e.DstId.ToString()},{"TS",(e.Slot+1).ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","COLLISION"}});
                        return;
                    }

                    // this is a new call stream
                    status.RxStart = pktTime;
                    Log.Logger.Information($"({SystemName}) DMRD: Traffic *PRV CALL START  * PEER {e.PeerId} SRC_ID {e.SrcId} DST_ID {e.DstId} TS {e.Slot + 1} [STREAM ID {e.StreamId}]");
                    // send report to monitor server
                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"DST_ID",e.DstId.ToString()},{"TS",(e.Slot+1).ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","PRIVATE_CALL_START"}});

                    status.RxCallType = e.CallType;

                    // if we can, use the LC from the voice header as to keep all options intact
                    if ((e.FrameType == FrameType.DATA_SYNC) && (e.DataType == DMRDataType.VOICE_LC_HEADER))
                    {
                        LC lc = FullLC.Decode(dmrpkt, DMRDataType.VOICE_LC_HEADER);
                        status.DMR_RxLC = lc;
                    }
                    else // if we don't have a voice header; don't wait to decode it, just make a dummy header
                        status.DMR_RxLC = new LC()
                        {
                            SrcId = e.SrcId,
                            DstId = e.DstId
                        };

                    status.DMR_RxPILC = new PrivacyLC();
                    Log.Logger.Debug($"({SystemName}) TS {e.Slot + 1} [STREAM ID {e.StreamId}] RX_LC {FneUtils.HexDump(status.DMR_RxLC.GetBytes())}");
                }

                // if we can, use the PI LC from the PI voice header as to keep all options intact
                if ((e.FrameType == FrameType.DATA_SYNC) && (e.DataType == DMRDataType.VOICE_PI_HEADER))
                {
                    PrivacyLC lc = FullLC.DecodePI(dmrpkt);
                    status.DMR_RxPILC = lc;
                    Log.Logger.Information($"({SystemName}) DMRD: Traffic *CALL PI PARAMS  * PEER {e.PeerId} DST_ID {e.DstId} TS {e.Slot + 1} ALGID {lc.AlgId} KID {lc.KId} [STREAM ID {e.StreamId}]");
                    Log.Logger.Debug($"({SystemName}) TS {e.Slot + 1} [STREAM ID {e.StreamId}] RX_PI_LC {FneUtils.HexDump(status.DMR_RxPILC.GetBytes())}");
                }

                // final actions - is this a voice terminator?
                if ((e.FrameType == FrameType.DATA_SYNC) && (e.DataType == DMRDataType.TERMINATOR_WITH_LC) && (status.RxType != FrameType.TERMINATOR))
                {
                    TimeSpan callDuration = pktTime - status.RxStart;
                    Log.Logger.Information($"({SystemName}) DMRD: Traffic *PRV CALL END    * PEER {e.PeerId} SRC_ID {e.SrcId} DST_ID {e.DstId} TS {e.Slot + 1} DUR {callDuration.TotalSeconds} [STREAM ID: {e.StreamId}]");
                    // send report to monitor server
                    FneReporter.sendReport(new Dictionary<string,string> { {"SystemName",SystemName},{"PEER",e.PeerId.ToString()},{"SRC_ID",e.SrcId.ToString()},{"DST_ID",e.DstId.ToString()},{"TS",(e.Slot+1).ToString()},{"DUR",callDuration.TotalSeconds.ToString()},{"STREAM ID",e.StreamId.ToString()},{"Value","PRIVATE_CALL_END"}});

                    if (dmrCalls.ContainsKey(e.DstId)) 
                        if (dmrCalls.ContainsKey(e.Slot))
                            dmrCalls.Remove(e.DstId);
                }

                status.RxPeerId = e.PeerId;
                status.RxRFS = e.SrcId;
                status.RxType = e.FrameType;
                status.RxTGId = e.DstId;
                status.RxTime = pktTime;
                status.RxStreamId = e.StreamId;

                if (dmrCalls.ContainsKey(e.DstId)) 
                {
                    if (dmrCalls.ContainsKey(e.Slot))
                        dmrCalls[e.DstId][e.Slot] = status;
                }
            }
        }
    } // public abstract partial class FneSystemBase
} // namespace fnerouter
