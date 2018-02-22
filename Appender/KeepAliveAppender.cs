#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using log4net.Core;
using log4net.Util;
using System.Threading;

namespace log4net.Appender
{
    /// <summary>
    /// KeepAliveAppender will produce alive logs in regular intervals from the log4net.Appender.KeepAliveAppender logger.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Without imparting any of the parent <see cref="ForwardingAppender"/>'s features,
    /// this appender get's itself <see cref="KeepAlive.Manage"/>d in <see cref="ActivateOptions"/>
    /// and <see cref="KeepAlive.Release"/>d in <see cref="OnClose"/>. Any attached appender should 
    /// then receive Alive messages.
    /// </para>
    /// <para>
    /// The appender must be referenced or else it will not be called. Add a additivity false logger log4net.Appender.KeepAliveAppender
    /// and hook this appender to it. Then add appenders that need to be kept alive to this appender.
    /// </para>
    /// </remarks>
    /// <author>Robert Sevcik</author>
    public class KeepAliveAppender : ForwardingAppender
    {
        private static ILog log = LogManager.GetLogger(typeof(KeepAliveAppender));

        /// <summary>
        /// It's the default level used to log 'alive' events, based on log4net's Alert level. 
        /// It's high so that it bounces through INFO/ERROR filters, even though it is not a reason to be alert.
        /// Lack of 'alive' might be a reason to be alert though.
        /// </summary>
        public static Level Alive = new Level(Level.Alert.Value, "ALIVE");

        /// <summary>
        /// Log alive message at this level
        /// </summary>
        public Level KeepAliveLevel { get; set; }

        /// <summary>
        /// Keep alive message
        /// </summary>
        public string KeepAliveMessage { get; set; }

        /// <summary>
        /// Keep alive interval
        /// </summary>
        public int KeepAliveInterval { get; set; }

        /// <summary>
        /// Initialize the appender based on the options set
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is part of the <see cref="IOptionHandler"/> delayed object
        /// activation scheme. The <see cref="ActivateOptions"/> method must 
        /// be called on this object after the configuration properties have
        /// been set. Until <see cref="ActivateOptions"/> is called this
        /// object is in an undefined state and must not be used. 
        /// </para>
        /// <para>
        /// If any of the configuration properties are modified then 
        /// <see cref="ActivateOptions"/> must be called again.
        /// </para>
        /// </remarks>
        public override void ActivateOptions()
        {
            base.ActivateOptions();
            KeepAlive.Instance.Manage(AliveCall, KeepAliveInterval);
        }

        /// <summary>
        /// Make an "I'm alive!" call
        /// </summary>
        protected void AliveCall()
        {
            log.Logger.Log(log.GetType(), KeepAliveLevel ?? Alive, KeepAliveMessage ?? Alive.DisplayName, null);
        }

        /// <summary>
        /// Closes the appender and releases resources.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Releases any resources allocated within the appender such as file handles, 
        /// network connections, etc.
        /// </para>
        /// <para>
        /// It is a programming error to append to a closed appender.
        /// </para>
        /// </remarks>
        protected override void OnClose()
        {
            KeepAlive.Instance.Release(AliveCall);

            base.OnClose();
        }
    }
}
