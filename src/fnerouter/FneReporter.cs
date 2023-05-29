/**
* Digital Voice Modem - Fixed Network Equipment
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment
*
*/
/*
*   Copyright (C) 2023 by Charlie Bricker
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
using System.Net.Http;

namespace fnerouter
{
    /// <summary>
    /// 
    /// </summary>
    public class FneReporter
    {
        private static readonly HttpClient client = new HttpClient();
        public static bool reporterEnabled = true;

        //TODO TODO TODO: Get reporterEnabled value from config.
        public static int port = 5555;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dict"></param>
        internal static async void sendReport(Dictionary<string, string> dict)
        {
            if (reporterEnabled)
            {
                var content = new FormUrlEncodedContent(dict);
                //TODO: Get port value from config.

                try
                {
                    var response = await client.PostAsync(("http://localhost:" + port + "/"), content);
                    var responseString = await response.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to send report. ", e);
                }
            }
        }
    } // class FneReporter
} // namespace fnerouter
