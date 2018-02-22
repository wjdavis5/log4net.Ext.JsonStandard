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
using log4net.Appender;
using log4net.Layout;
using log4net.Layout.Pattern;
using log4net.ObjectRenderer;
using log4net.Repository;
using log4net.Util;
using log4net.Util.TypeConverters;
using log4net.Repository.Hierarchy;
using log4net.Util.Stamps;

namespace log4net.Plugin
{
    /// <summary>
    /// Set <see cref="Hierarchy.LoggerFactory"/> to <see cref="StampingLoggerFactory"/>
    /// </summary>
    /// <author>Robert Sevcik</author>
    public class StampPlugin : IPlugin
    {
        /// <summary>
        /// Plugin name is the Type's AssemblyQualifiedName
        /// </summary>
        public virtual string Name { get { return this.GetType().AssemblyQualifiedName; } }

        /// <summary>
        /// Attached repository
        /// </summary>
        public ILoggerRepository Repo { get; set; }

        /// <summary>
        /// Interfere with a repository
        /// </summary>
        /// <param name="repository"></param>
        public void Attach(ILoggerRepository repository)
        {
            Repo = repository;
            Repo.ConfigurationChanged += Repo_ConfigurationChanged;
            if (Repo.Configured) Repo_ConfigurationChanged(Repo, new EventArgs());
        }

        /// <summary>
        /// Stop interferring
        /// </summary>
        public void Shutdown()
        {
            Repo.ConfigurationChanged -= Repo_ConfigurationChanged;
            Repo = null;
        }

        /// <summary>
        /// Do the interferring
        /// </summary>
        /// <param name="sender">a repo, only <see cref="Hierarchy"/> is handled</param>
        /// <param name="e">ignored</param>
        void Repo_ConfigurationChanged(object sender, EventArgs e)
        {
            var hierarchy = sender as Hierarchy;

            if (hierarchy != null)
                hierarchy.LoggerFactory = new StampingLoggerFactory(hierarchy.LoggerFactory);
        }
    }
}
