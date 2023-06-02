﻿/**
* Digital Voice Modem - Fixed Network Equipment
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment
*
*/
/*
*   Copyright (C) 2022-2023 by Bryan Biedenkapp N2PLL
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
using System.Security.Cryptography;

using fnecore.DMR;
using fnecore.P25;
using fnecore.NXDN;
using fnecore.EDAC;

namespace fnecore
{
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
    public delegate bool DMRDataValidate(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId);
    /// <summary>
    /// Event used to process incoming DMR data.
    /// </summary>
    public class DMRDataReceivedEvent : EventArgs
    {
        /// <summary>
        /// Peer ID
        /// </summary>
        public uint PeerId { get; }
        /// <summary>
        /// Source Address
        /// </summary>
        public uint SrcId { get; }
        /// <summary>
        /// Destination Address
        /// </summary>
        public uint DstId { get; }
        /// <summary>
        /// Slot Number
        /// </summary>
        public byte Slot { get; }
        /// <summary>
        /// Call Type (Group or Private)
        /// </summary>
        public CallType CallType { get; }
        /// <summary>
        /// Frame Type
        /// </summary>
        public FrameType FrameType { get; }
        /// <summary>
        /// DMR Data Type
        /// </summary>
        public DMRDataType DataType { get; }
        /// <summary>
        /// 
        /// </summary>
        public byte n { get; }
        /// <summary>
        /// Stream ID
        /// </summary>
        public uint StreamId { get; }
        /// <summary>
        /// Raw message data
        /// </summary>
        public byte[] Data { get; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="DMRDataReceivedEvent"/> class.
        /// </summary>
        private DMRDataReceivedEvent()
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DMRDataReceivedEvent"/> class.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="slot">Slot Number</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="dataType">DMR Data Type</param>
        /// <param name="n"></param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="data">Raw message data</param>
        public DMRDataReceivedEvent(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, byte n, uint streamId, byte[] data) : base()
        {
            this.PeerId = peerId;
            this.SrcId = srcId;
            this.DstId = dstId;
            this.Slot = slot;
            this.CallType = callType;
            this.FrameType = frameType;
            this.DataType = dataType;
            this.n = n;
            this.StreamId = streamId;

            byte[] Data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, Data, 0, data.Length);
        }
    } // public class DMRDataReceivedEvent : EventArgs

    /// <summary>
    /// Callback used to validate incoming P25 data.
    /// </summary>
    /// <param name="peerId">Peer ID</param>
    /// <param name="srcId">Source Address</param>
    /// <param name="dstId">Destination Address</param>
    /// <param name="callType">Call Type (Group or Private)</param>
    /// <param name="duid">P25 DUID</param>
    /// <param name="frameType">Frame Type</param>
    /// <param name="streamId">Stream ID</param>
    /// <returns>True, if data stream is valid, otherwise false.</returns>
    public delegate bool P25DataValidate(uint peerId, uint srcId, uint dstId, CallType callType, P25DUID duid, FrameType frameType, uint streamId);
    /// <summary>
    /// Event used to process incoming P25 data.
    /// </summary>
    public class P25DataReceivedEvent : EventArgs
    {
        /// <summary>
        /// Peer ID
        /// </summary>
        public uint PeerId { get; }
        /// <summary>
        /// Source Address
        /// </summary>
        public uint SrcId { get; }
        /// <summary>
        /// Destination Address
        /// </summary>
        public uint DstId { get; }
        /// <summary>
        /// Call Type (Group or Private)
        /// </summary>
        public CallType CallType { get; }
        /// <summary>
        /// P25 DUID
        /// </summary>
        public P25DUID DUID { get; }
        /// <summary>
        /// Frame Type
        /// </summary>
        public FrameType FrameType { get; }
        /// <summary>
        /// Stream ID
        /// </summary>
        public uint StreamId { get; }
        /// <summary>
        /// Raw message data
        /// </summary>
        public byte[] Data { get; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="P25DataReceivedEvent"/> class.
        /// </summary>
        private P25DataReceivedEvent()
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="P25DataPreprocessEvent"/> class.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="duid">P25 DUID</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="data">Raw message data</param>
        public P25DataReceivedEvent(uint peerId, uint srcId, uint dstId, CallType callType, P25DUID duid, FrameType frameType, uint streamId, byte[] data) : base()
        {
            this.PeerId = peerId;
            this.SrcId = srcId;
            this.DstId = dstId;
            this.CallType = callType;
            this.DUID = duid;
            this.FrameType = frameType;
            this.StreamId = streamId;

            Data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, Data, 0, data.Length);
        }
    } // public class P25DataReceivedEvent : EventArgs

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
    public delegate bool NXDNDataValidate(uint peerId, uint srcId, uint dstId, CallType callType, NXDNMessageType messageType, FrameType frameType, uint streamId);
    /// <summary>
    /// Event used to process incoming NXDN data.
    /// </summary>
    public class NXDNDataReceivedEvent : EventArgs
    {
        /// <summary>
        /// Peer ID
        /// </summary>
        public uint PeerId { get; }
        /// <summary>
        /// Source Address
        /// </summary>
        public uint SrcId { get; }
        /// <summary>
        /// Destination Address
        /// </summary>
        public uint DstId { get; }
        /// <summary>
        /// Call Type (Group or Private)
        /// </summary>
        public CallType CallType { get; }
        /// <summary>
        /// NXDN Message Type
        /// </summary>
        public NXDNMessageType MessageType { get; }
        /// <summary>
        /// Frame Type
        /// </summary>
        public FrameType FrameType { get; }
        /// <summary>
        /// Stream ID
        /// </summary>
        public uint StreamId { get; }
        /// <summary>
        /// Raw message data
        /// </summary>
        public byte[] Data { get; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="NXDNDataReceivedEvent"/> class.
        /// </summary>
        private NXDNDataReceivedEvent()
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NXDNDataReceivedEvent"/> class.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="messageType">NXDN Message Type</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="data">Raw message data</param>
        public NXDNDataReceivedEvent(uint peerId, uint srcId, uint dstId, CallType callType, NXDNMessageType messageType, FrameType frameType, uint streamId, byte[] data) : base()
        {
            this.PeerId = peerId;
            this.SrcId = srcId;
            this.DstId = dstId;
            this.CallType = callType;
            this.MessageType = messageType;
            this.FrameType = frameType;
            this.StreamId = streamId;

            byte[] Data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, Data, 0, data.Length);
        }
    } // public class NXDNDataReceivedEvent : EventArgs

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
    public delegate bool PeerIgnored(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId);
    /// <summary>
    /// Event when a peer connects.
    /// </summary>
    public class PeerConnectedEvent : EventArgs
    {
        /// <summary>
        /// Peer ID
        /// </summary>
        public uint PeerId { get; }
        /// <summary>
        /// Peer Information
        /// </summary>
        public PeerInformation Information { get; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="PeerConnectedEvent"/> class.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="peer">Peer Information</param>
        public PeerConnectedEvent(uint peerId, PeerInformation peer) : base()
        {
            this.PeerId = peerId;
            this.Information = peer;
        }
    } // public class PeerConnectedEvent : EventArgs

    /// <summary>
    /// Type of FNE instance.
    /// </summary>
    public enum FneType : byte
    {
        /// <summary>
        /// Master
        /// </summary>
        MASTER,
        /// <summary>
        /// Peer
        /// </summary>
        PEER,
        /// <summary>
        /// Unknown (should never happen)
        /// </summary>
        UNKNOWN = 0xFF
    } // public enum FneType : byte


    /// <summary>
    /// This class implements some base functionality for all other FNE network classes.
    /// </summary>
    public abstract class FneBase
    {
        protected readonly string systemName = string.Empty;
        protected readonly uint peerId = 0;

        protected static Random rand = null;

        protected bool isStarted = false;
        protected FneType fneType;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the system name for this <see cref="FneBase"/>.
        /// </summary>
        public string SystemName => systemName;

        /// <summary>
        /// Gets the peer ID for this <see cref="FneBase"/>.
        /// </summary>
        public uint PeerId => peerId;

        /// <summary>
        /// Flag indicating whether this <see cref="FneBase"/> is running.
        /// </summary>
        public bool IsStarted => isStarted;

        /// <summary>
        /// Gets the <see cref="FneType"/> this <see cref="FneBase"/> is.
        /// </summary>
        public FneType FneType => fneType;

        /// <summary>
        /// Gets/sets the interval that peers will need to ping the master.
        /// </summary>
        public int PingTime
        {
            get;
            set;
        }

        /// <summary>
        /// Get/sets the current logging level of the <see cref="FneBase"/> instance.
        /// </summary>
        public LogLevel LogLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Get/sets a flag that enables dumping the raw recieved packets to the log.
        /// </summary>
        /// <remarks>This will also require the <see cref="FneBase.LogLevel"/> be set to DEBUG.</remarks>
        public bool RawPacketTrace
        {
            get;
            set;
        }

        /*
        ** Events/Callbacks
        */

        /// <summary>
        /// Callback action that handles validating a DMR call stream.
        /// </summary>
        public DMRDataValidate DMRDataValidate = null;
        /// <summary>
        /// Event action that handles processing a DMR call stream.
        /// </summary>
        public event EventHandler<DMRDataReceivedEvent> DMRDataReceived;

        /// <summary>
        /// Callback action that handles validating a P25 call stream.
        /// </summary>
        public P25DataValidate P25DataValidate = null;
        /// <summary>
        /// Event action that handles preprocessing a P25 call stream.
        /// </summary>
        public event EventHandler<P25DataReceivedEvent> P25DataPreprocess;
        /// <summary>
        /// Event action that handles processing a P25 call stream.
        /// </summary>
        public event EventHandler<P25DataReceivedEvent> P25DataReceived;

        /// <summary>
        /// Callback action that handles validating a NXDN call stream.
        /// </summary>
        public NXDNDataValidate NXDNDataValidate = null;
        /// <summary>
        /// Event action that handles processing a NXDN call stream.
        /// </summary>
        public event EventHandler<NXDNDataReceivedEvent> NXDNDataReceived;

        /// <summary>
        /// Callback action that handles verifying if a peer is ignored for a call stream.
        /// </summary>
        public PeerIgnored PeerIgnored = null;
        /// <summary>
        /// Event action that handles when a peer connects.
        /// </summary>
        public event EventHandler<PeerConnectedEvent> PeerConnected;
        /// <summary>
        /// Callback action that handles when a peer disconnects.
        /// </summary>
        public Action<uint> PeerDisconnected = null;

        /// <summary>
        /// Callback action that handles internal logging.
        /// </summary>
        public Action<LogLevel, string> Logger;

        /*
        ** Methods
        */

        /// <summary>
        /// Static initializer for the <see cref="FneMaster"/> class.
        /// </summary>
        static FneBase()
        {
            int seed = 0;
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] intBytes = new byte[4];
                rng.GetBytes(intBytes);
                seed = BitConverter.ToInt32(intBytes, 0);
            }

            rand = new Random(seed);
            rand.Next();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FneBase"/> class.
        /// </summary>
        /// <param name="systemName"></param>
        /// <param name="peerId"></param>
        protected FneBase(string systemName, uint peerId)
        {
            this.systemName = systemName;
            this.peerId = peerId;

            this.fneType = FneType.UNKNOWN;

            // set a default "noop" logger
            Logger = (LogLevel level, string message) => { };
        }

        /// <summary>
        /// Starts the main execution loop for this <see cref="FneBase"/>.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Stops the main execution loop for this <see cref="FneBase"/>.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Helper to generate a new stream ID.
        /// </summary>
        /// <returns></returns>
        public static uint CreateStreamID()
        {
            return (uint)rand.Next(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// Helper to just quickly generate opcode tuples (mainly for brevity).
        /// </summary>
        /// <param name="func">Function</param>
        /// <param name="subFunc">Sub-Function</param>
        /// <returns></returns>
        public static Tuple<byte, byte> CreateOpcode(byte func, byte subFunc = Constants.NET_SUBFUNC_NOP)
        {
            return new Tuple<byte, byte>(func, subFunc);
        }

        /// <summary>
        /// Helper to pack a peer ID into a byte array.
        /// </summary>
        /// <param name="peerId">Peer ID.</param>
        /// <returns></returns>
        protected byte[] PackPeerId(uint peerId)
        {
            byte[] bytes = new byte[4];
            FneUtils.WriteBytes(peerId, ref bytes, 0);
            return bytes;
        }

        /// <summary>
        /// Helper to generate a tagged response.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected byte[] Response(string tag, byte[] data)
        {
            byte[] res = new byte[tag.Length + data.Length];
            FneUtils.StringToBytes(tag, res, 0, tag.Length);
            Buffer.BlockCopy(data, 0, res, tag.Length, data.Length);
            return res;
        }

        /// <summary>
        /// Helper to fire the logging action.
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        protected void Log(LogLevel logLevel, string message)
        {
            byte level = (byte)logLevel;
            if (level <= (byte)LogLevel)
                Logger(logLevel, message);
        }

        /// <summary>
        /// Helper to read and process a FNE RTP frame.
        /// </summary>
        /// <param name="frame">Raw UDP socket frame.</param>
        /// <param name="messageLength">Length of payload message.</param>
        /// <param name="rtpHeader">RTP Header.</param>
        /// <param name="fneHeader">RTP FNE Header.</param>
        protected byte[] ReadFrame(UdpFrame frame, out int messageLength, out RtpHeader rtpHeader, out RtpFNEHeader fneHeader)
        {
            int length = frame.Message.Length;
            messageLength = -1;
            rtpHeader = null;
            fneHeader = null;

            // read message from socket
            if (length > 0)
            {
                if (length < Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes)
                {
                    Log(LogLevel.ERROR, $"Message received from network is malformed! " +
                        $"{Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes} bytes != {frame.Message.Length} bytes");
                    return null;
                }

                // decode RTP header
                rtpHeader = new RtpHeader();
                if (!rtpHeader.Decode(frame.Message))
                {
                    Log(LogLevel.ERROR, $"Invalid RTP packet received from network");
                    return null;
                }

                // ensure the RTP header has extension header (otherwise abort)
                if (!rtpHeader.Extension)
                {
                    Log(LogLevel.ERROR, "Invalid RTP header received from network");
                    return null;
                }

                // ensure payload type is correct
                if ((rtpHeader.PayloadType != Constants.DVMRtpPayloadType) &&
                    (rtpHeader.PayloadType != Constants.DVMRtpControlPayloadType))
                {
                    Log(LogLevel.ERROR, "Invalid RTP payload type received from network");
                    return null;
                }

                // decode FNE RTP header
                fneHeader = new RtpFNEHeader();
                if (!fneHeader.Decode(frame.Message))
                {
                    Log(LogLevel.ERROR, "Invalid RTP packet received from network");
                    return null;
                }

                // copy message
                messageLength = (int)fneHeader.MessageLength;
                byte[] message = new byte[messageLength];
                Buffer.BlockCopy(frame.Message, (int)(Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes + Constants.RtpFNEHeaderLengthBytes), 
                    message, 0, messageLength);

                ushort calc = CRC.CreateCRC16(message, (uint)(messageLength * 8));
                if (calc != fneHeader.CRC)
                {
                    Log(LogLevel.ERROR, "Failed CRC CCITT-162 check");
                    messageLength = -1;
                    return null;
                }

                return message;
            }

            return null;
        }

        /// <summary>
        /// Helper to generate and write a FNE RTP frame.
        /// </summary>
        /// <param name="message">Payload message.</param>
        /// <param name="peerId">Peer ID.</param>
        /// <param name="opcode">FNE Network Opcode.</param>
        /// <param name="pktSeq">RTP Packet Sequence.</param>
        /// <param name="streamId">Stream ID.</param>
        /// <returns></returns>
        protected byte[] WriteFrame(byte[] message, uint peerId, Tuple<byte, byte> opcode, ushort pktSeq, uint streamId)
        {
            byte[] buffer = new byte[message.Length + Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes + Constants.RtpFNEHeaderLengthBytes];
            FneUtils.Memset(buffer, 0, buffer.Length);

            RtpHeader header = new RtpHeader();
            header.Extension = true;
            header.PayloadType = Constants.DVMRtpPayloadType;
            header.Sequence = pktSeq;
            header.SSRC = streamId;

            header.Encode(ref buffer);

            RtpFNEHeader fneHeader = new RtpFNEHeader();
            fneHeader.CRC = CRC.CreateCRC16(message, (uint)(message.Length * 8));
            fneHeader.StreamID = streamId;
            fneHeader.PeerID = peerId;
            fneHeader.MessageLength = (uint)message.Length;

            fneHeader.Function = opcode.Item1;
            fneHeader.SubFunction = opcode.Item2;

            fneHeader.Encode(ref buffer);

            Buffer.BlockCopy(message, 0, buffer, (int)(Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes + Constants.RtpFNEHeaderLengthBytes),
                    message.Length);
            return buffer;
        }

        /// <summary>
        /// Helper to fire the DMR data received event.
        /// </summary>
        /// <param name="e"><see cref="DMRDataReceivedEvent"/> instance</param>
        protected void FireDMRDataReceived(DMRDataReceivedEvent e)
        {
            if (DMRDataReceived != null)
                DMRDataReceived.Invoke(this, e);
        }

        /// <summary>
        /// Helper to fire the P25 data pre-process event.
        /// </summary>
        /// <param name="e"><see cref="P25DataReceivedEvent"/> instance</param>
        protected void FireP25DataPreprocess(P25DataReceivedEvent e)
        {
            if (P25DataPreprocess != null)
                P25DataPreprocess.Invoke(this, e);
        }

        /// <summary>
        /// Helper to fire the P25 data received event.
        /// </summary>
        /// <param name="e"><see cref="P25DataReceivedEvent"/> instance</param>
        protected void FireP25DataReceived(P25DataReceivedEvent e)
        {
            if (P25DataReceived != null)
                P25DataReceived.Invoke(this, e);
        }

        /// <summary>
        /// Helper to fire the NXDN data received event.
        /// </summary>
        /// <param name="e"><see cref="NXDNDataReceivedEvent"/> instance</param>
        protected void FireNXDNDataReceived(NXDNDataReceivedEvent e)
        {
            if (NXDNDataReceived != null)
                NXDNDataReceived.Invoke(this, e);
        }

        /// <summary>
        /// Helper to fire the peer connected event.
        /// </summary>
        /// <param name="e"><see cref="PeerConnectedEvent"/> instance</param>
        protected void FirePeerConnected(PeerConnectedEvent e)
        {
            if (PeerConnected != null)
                PeerConnected.Invoke(this, e);
        }
    } // public abstract class FneBase
} // namespace fnecore
