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
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Util.Stamps
{
    /// <summary>
    /// Wrap created loggers with a stamping logger to modify <see cref="LoggingEvent"/>s as they pass through.
    /// </summary>
    /// <remarks>
    /// Purpose: to have a uniquely identifying stamps on each event regardless of user code and regardless of appenders.
    /// Caller unique identity can mostly be established by checking the machine, processid and time 
    /// with the assumption that only one app instance can run on a certain machine with a certain PID at a certain time.
    /// If this assumption is broken, then other/further stamps are needed. Sequential stamp is thrown in to address
    /// a problem with time measurement granularity within the application between consecutive or parallel events.
    /// With uniquely identifying stamps, events can be processed, managed and analyzed more easily in respective tools.
    /// </remarks>
    /// <author>Robert Sevcik</author>
    public class StampingLoggerFactory : ILoggerFactory
    {
        /// <summary>
        /// Defines the default stamping policy
        /// </summary>
        public static IStamp[] DefaultStamps
                            = new IStamp[]{
                                new Stamp()
                            };

        /// <summary>
        /// List of stamps to be used on events. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// XML configuration will likely use the <see cref="AddStamp"/> method to populate this.
        /// </para>
        /// <para>
        /// If empty, the <see cref="DefaultStamps"/> set will be used.
        /// </para>
        /// </remarks>
        protected readonly IList<IStamp> Stamps = new List<IStamp>();

        /// <summary>
        /// The wrapped logger factory. 
        /// </summary>
        /// <remarks>
        /// If empty, <see cref="DefaultLoggerFactory" /> fills the space on first use.
        /// </remarks>
        public ILoggerFactory InnerFactory { get; set; }

        /// <summary>
        /// Create instance without an <see cref="InnerFactory"/>
        /// </summary>
        public StampingLoggerFactory()
            : this(null)
        {
        }

        /// <summary>
        /// Create instance with a <see cref="InnerFactory"/>
        /// </summary>
        /// <param name="innerFactory">can be null</param>
        public StampingLoggerFactory(ILoggerFactory innerFactory)
        {
            InnerFactory = innerFactory;
        }

        #region ILoggerFactory implementation
#if LOG4NET_1_2_10_COMPATIBLE
        /// <summary>
        /// Wraps the <see cref="Logger" /> made by the wrapped inner factory in a stamping logger.
        /// </summary>
        /// <param name="name">The name of the <see cref="Logger" />.</param>
        /// <returns>The <see cref="Logger" /> instance for the specified name wrapped with a stamper.</returns>
        /// <remarks>
        /// <para>
        /// Create a new <see cref="Logger" /> instance with the 
        /// specified name.
        /// </para>
        /// <para>
        /// Called by the <see cref="Hierarchy"/> to create
        /// new named <see cref="Logger"/> instances.
        /// </para>
        /// <para>
        /// If the <paramref name="name"/> is <c>null</c> then the root logger
        /// must be returned.
        /// </para>
        /// </remarks>
        public virtual Logger CreateLogger(string name)
        {
            var innerFactory = GetFactory();

            if (innerFactory == null)
                throw new InvalidOperationException(String.Format("No factory was created by this {0}", this));

            var innerLogger = innerFactory.CreateLogger(name);

            if (innerLogger == null)
                throw new InvalidOperationException(String.Format("Factory {0} created no logger.", innerFactory));

            return CreateStampingLogger(innerLogger, StampEvent);
        }
#else
        /// <summary>
        /// Wraps the <see cref="Logger" /> made by the wrapped inner factory in a stamping logger.
        /// </summary>
        /// <param name="repository">The <see cref="ILoggerRepository" /> that will own the <see cref="Logger" />.</param>
        /// <param name="name">The name of the <see cref="Logger" />.</param>
        /// <returns>The <see cref="Logger" /> instance for the specified name wrapped with a stamper.</returns>
        /// <remarks>
        /// <para>
        /// Create a new <see cref="Logger" /> instance with the 
        /// specified name.
        /// </para>
        /// <para>
        /// Called by the <see cref="Hierarchy"/> to create
        /// new named <see cref="Logger"/> instances.
        /// </para>
        /// <para>
        /// If the <paramref name="name"/> is <c>null</c> then the root logger
        /// must be returned.
        /// </para>
        /// </remarks>
        public virtual Logger CreateLogger(ILoggerRepository repository, string name)
        {
            if (repository == null)
                throw new ArgumentNullException("repository");

            var innerFactory = GetFactory();

            if (innerFactory == null)
                throw new InvalidOperationException(String.Format("No factory was created by this {0}", this));

            var innerLogger = innerFactory.CreateLogger(repository, name);

            if (innerLogger == null)
                throw new InvalidOperationException(String.Format("Factory {0} created no logger.", innerFactory));

            return CreateStampingLogger(innerLogger, StampEvent);
        }
#endif
        #endregion

        /// <summary>
        /// Get the wrapped factory, most likely <see cref="InnerFactory"/>. Set a default if empty (<see cref="DefaultLoggerFactory" />)
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// Gives an inherited class a chance to override default behavior.
        /// </para>
        /// <para>
        /// FIXME: Could we reach the <see cref="DefaultLoggerFactory" /> more easily/obviously, rather than instantiating a fake <see cref="Hierarchy"/> and fetching <see cref="Hierarchy.LoggerFactory"/>?
        /// </para>
        /// </remarks>
        protected virtual ILoggerFactory GetFactory()
        {
            var innerFactory = InnerFactory;

            if (innerFactory == null)
            {
                var fakeRepo = new Hierarchy();
                innerFactory = fakeRepo.LoggerFactory;
                InnerFactory = innerFactory;
            }

            return innerFactory;
        }

        /// <summary>
        /// Create the wrapping stamper logger. By default, root logger is not wrapped.
        /// </summary>
        /// <param name="logger">Implementation logger to wrap.</param>
        /// <param name="call">delegate to call to stamp the event before passing it to <paramref name="logger"/> </param>
        /// <returns></returns>
        protected virtual Logger CreateStampingLogger(Logger logger, StampDelegate call)
        {
            if (logger.Name == "root") return logger;
            else return new StampingLogger(logger, call);
        }

        /// <summary>
        /// The default delegate for <see cref="CreateStampingLogger"/> applies all the stamps in regular order.
        /// </summary>
        /// <param name="loggingEvent">event to stamp</param>
        protected virtual void StampEvent(LoggingEvent loggingEvent)
        {
            foreach (var stamp in GetStamps())
            {
                if (stamp != null)
                    stamp.StampEvent(loggingEvent);
            }
        }

        /// <summary>
        /// Get stamps defined or default stamps or empty.
        /// </summary>
        /// <returns>stamps to apply</returns>
        /// <remarks>
        /// Called by <see cref="StampEvent"/>, gives a child class a chance to override default behavior.
        /// </remarks>
        protected virtual IEnumerable<IStamp> GetStamps()
        {
            return Stamps.Count == 0
                ? DefaultStamps
                : Stamps;
        }

        /// <summary>
        /// Add a stamp to the set
        /// </summary>
        /// <param name="stamp"></param>
        /// <remarks>
        /// This will likely be called by the XML config.
        /// </remarks>
        /// <remarks>
        /// The default behavior is that if no stamps are explicitly added, the <see cref="DefaultStamps"/> are used instead.
        /// </remarks>
        public void AddStamp(IStamp stamp)
        {
            Stamps.Add(stamp);
        }
    }
}
