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

namespace fneparrot
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
        public string FileRoot = "fneparrot";
    } // public class ConfigLogObject

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
        public List<ConfigMasterObject> Masters = new List<ConfigMasterObject>();
    } // public class ConfigurationObject
} // namespace fneparrot
