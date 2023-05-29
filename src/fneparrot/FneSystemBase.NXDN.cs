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
using System.Threading.Tasks;

using Serilog;

using fnecore;
using fnecore.NXDN;

namespace fneparrot
{
    /// <summary>
    /// Implements a FNE system base.
    /// </summary>
    public abstract partial class FneSystemBase
    {
        private List<byte[]> nxdnCallData = new List<byte[]>();

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
            return true;
        }

        /// <summary>
        /// Event handler used to process incoming NXDN data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void NXDNDataReceived(object sender, NXDNDataReceivedEvent e)
        {
            if (fne.FneType == FneType.PEER)
            {
                Log.Logger.Error($"({SystemName}) NXDD: How did this happen? Parrot doesn't have peers...");
                return;
            }

            DateTime pktTime = DateTime.Now;
            if (e.CallType == CallType.GROUP)
            {
                if (e.SrcId == 0)
                {
                    Log.Logger.Warning($"({SystemName}) NXDD: Received call from SRC_ID {e.SrcId}? Dropping call e.Data.");
                    p25CallData.Clear();
                    return;
                }

                // is this a new call stream?
                if (e.StreamId != status[NXDN_FIXED_SLOT].RxStreamId && (e.MessageType != NXDNMessageType.MESSAGE_TYPE_TX_REL))
                {
                    status[NXDN_FIXED_SLOT].RxStart = pktTime;
                    Log.Logger.Information($"({SystemName}) NXDD: Traffic *CALL START     * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} [STREAM ID {e.StreamId}]");
                }

                if ((e.MessageType == NXDNMessageType.MESSAGE_TYPE_TX_REL) && (status[NXDN_FIXED_SLOT].RxType != FrameType.TERMINATOR))
                {
                    TimeSpan callDuration = pktTime - status[NXDN_FIXED_SLOT].RxStart;
                    Log.Logger.Information($"({SystemName}) NXDD: Traffic *CALL END       * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} DUR {callDuration} [STREAM ID {e.StreamId}]");
                    nxdnCallData.Add(e.Data);
                    Task.Delay(2000).GetAwaiter().GetResult();
                    Log.Logger.Information($"({SystemName}) NXDD: Playing back transmission from SRC_ID {e.SrcId}");

                    FneMaster master = (FneMaster)fne;
                    foreach (byte[] pkt in p25CallData)
                    {
                        master.SendPeers(FneBase.CreateOpcode(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_NXDN), pkt);
                        Task.Delay(60).GetAwaiter().GetResult();
                    }
                    nxdnCallData.Clear();
                }
                else
                    nxdnCallData.Add(e.Data);

                status[NXDN_FIXED_SLOT].RxRFS = e.SrcId;
                status[NXDN_FIXED_SLOT].RxType = e.FrameType;
                status[NXDN_FIXED_SLOT].RxTGId = e.DstId;
                status[NXDN_FIXED_SLOT].RxTime = pktTime;
                status[NXDN_FIXED_SLOT].RxStreamId = e.StreamId;
            }
            else
                Log.Logger.Warning($"({SystemName}) NXDD: Parrot does not support private calls.");

            return;
        }
    } // public abstract partial class FneSystemBase
} // namespace fneparrot
