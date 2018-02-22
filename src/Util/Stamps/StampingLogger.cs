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
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace log4net.Util.Stamps
{

    /// <summary>
    /// Stamping logger stamps the  <see cref="LoggingEvent"/> using the <see cref="StampDelegate"/> <see cref="Call"/> 
    /// and passes it to the wrapped <see cref="InnerLogger"/> in the <see cref="CallAppenders"/> call.
    /// </summary>
    /// <remarks>
    /// This would be a much nicer a job if ILogger could be decorated.
    /// Or! If ILogger was actually used in the framework :[ properly, particularly by the <see cref="Hierarchy" />
    /// </remarks>
    /// <remarks>
    /// 
    /// * All Log() and ForcedLog() methods are left as in <see cref="Logger"/> (not overriden).
    /// * We rely on the CallAppenders() method which should be called by either of the above.
    /// * Any other property or method should simply wrap the <see cref="InnerLogger"/>
    /// 
    /// </remarks>
    /// <author>Robert Sevcik</author>
    public class StampingLogger : Logger
    {
        /// <summary>
        /// The wrapped logger
        /// </summary>
        protected Logger InnerLogger { get; private set; }

        /// <summary>
        /// The stamping call to execute before passing event to <see cref="InnerLogger"/>
        /// </summary>
        protected StampDelegate Call { get; private set; }

        /// <summary>
        /// Wrap a logger and remember to call a delegate for each <see cref="LoggingEvent"/>
        /// </summary>
        /// <param name="innerLogger"></param>
        /// <param name="call"></param>
        /// <remarks>
        /// Callers shoul ensure that params are not null!
        /// </remarks>
        public StampingLogger(Logger innerLogger, StampDelegate call)
            : base(innerLogger.Name)
        {
            InnerLogger = innerLogger;
            Call = call;
        }

        /// <summary>
        /// The point of stamping the event and hand-over to <see cref="InnerLogger"/>
        /// </summary>
        /// <param name="loggingEvent">event to stamp and pass</param>
        protected override void CallAppenders(LoggingEvent loggingEvent)
        {
            Call(loggingEvent);
            InnerLogger.Log(loggingEvent);
        }

        #region Wrapped Properties

        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        public override bool Additivity
        {
            get { return InnerLogger.Additivity; }
            set { InnerLogger.Additivity = value; }
        }
        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        public override Appender.AppenderCollection Appenders
        {
            get { return InnerLogger.Appenders; }
        }
        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        public override Level EffectiveLevel
        {
            get { return InnerLogger.EffectiveLevel; }
        }
        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        public override Hierarchy Hierarchy
        {
            get { return InnerLogger.Hierarchy; }
            set { base.Hierarchy = InnerLogger.Hierarchy = value; }
        }
        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        public override Level Level
        {
            get { return InnerLogger.Level; }
            set { base.Level = InnerLogger.Level = value; }
        }
        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        public override string Name
        {
            get { return InnerLogger.Name; }
        }
        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        public override Logger Parent
        {
            get { return InnerLogger.Parent; }
            set { base.Parent = InnerLogger; InnerLogger.Parent = value; }
        }

        #endregion

        #region Wrapped Methods

        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        /// <param name="newAppender"></param>
        public override void AddAppender(Appender.IAppender newAppender)
        {
            InnerLogger.AddAppender(newAppender);
        }

        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        public override void CloseNestedAppenders()
        {
            InnerLogger.CloseNestedAppenders();
        }

        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Appender.IAppender GetAppender(string name)
        {
            return InnerLogger.GetAppender(name);
        }

        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public override bool IsEnabledFor(Level level)
        {
            return InnerLogger.IsEnabledFor(level);
        }

        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        public override void RemoveAllAppenders()
        {
            InnerLogger.RemoveAllAppenders();
        }

        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        /// <param name="appender"></param>
        /// <returns></returns>
        public override Appender.IAppender RemoveAppender(Appender.IAppender appender)
        {
            return InnerLogger.RemoveAppender(appender);
        }

        /// <summary>
        /// Wrap of <see cref="InnerLogger"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Appender.IAppender RemoveAppender(string name)
        {
            return InnerLogger.RemoveAppender(name);
        }

        #endregion

        /// <summary>
        /// Give some useful debugging description.
        /// </summary>
        /// <returns>"this wrapping that"</returns>
        public override string ToString()
        {
            return String.Format("{0} wrapping {1}", GetType(), InnerLogger);
        }
    }
}
