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

namespace fnerouter
{
    /// <summary>
    /// 
    /// </summary>
    public class RoutingRuleGroupVoiceDestination
    {
        /// <summary>
        /// 
        /// </summary>
        public string Network;

        /// <summary>
        /// 
        /// </summary>
        public uint Tgid;

        /// <summary>
        /// 
        /// </summary>
        public byte Slot;
    } // public class RoutingRuleGroupVoiceDestination

    /// <summary>
    /// 
    /// </summary>
    public class RoutingRuleGroupVoiceSource
    {
        /// <summary>
        /// 
        /// </summary>
        public uint Tgid;

        /// <summary>
        /// 
        /// </summary>
        public byte Slot;
    } // public class RoutingRuleGroupVoiceSource

    /// <summary>
    /// 
    /// </summary>
    public class RoutingRuleGroupVoiceConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public bool Active;
        
        /// <summary>
        /// 
        /// </summary>
        public bool Affiliated;
        
        /// <summary>
        /// 
        /// </summary>
        public bool Routable;

        /// <summary>
        /// 
        /// </summary>
        public List<int> Ignored;
    } // public class RoutingRuleGroupVoiceConfig

    /// <summary>
    /// 
    /// </summary>
    public class RoutingRuleGroupVoice
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name;

        /// <summary>
        /// 
        /// </summary>
        public RoutingRuleGroupVoiceConfig Config;

        /// <summary>
        /// 
        /// </summary>
        public RoutingRuleGroupVoiceSource Source;

        /// <summary>
        /// 
        /// </summary>
        public List<RoutingRuleGroupVoiceDestination> Destination;
    } // public class RoutingRuleGroupVoice

    /// <summary>
    /// 
    /// </summary>
    public class RoutingRule
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 
        /// </summary>
        public int GroupHangTime;
        
        /// <summary>
        /// 
        /// </summary>
        public bool Master;
        
        /// <summary>
        /// 
        /// </summary>
        public bool SendTgid;

        /// <summary>
        /// 
        /// </summary>
        public List<RoutingRuleGroupVoice> GroupVoice;
    } // public class RoutingRule
} // namespace fnerouter
