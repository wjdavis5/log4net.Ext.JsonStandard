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

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Plugin
{
    /// <summary>
    /// Not sure if this should stay. 
    /// This log4net plugin will register <see cref="JsonObjectRenderer"/>
    /// and add a "json" Conversion pattern to all <see cref="ILayout"/>s.
    /// </summary>
    /// <remarks>
    /// If you put the following in your assembly, the log4net logging will 
    /// be magically JSONified without much need for xml configuration:
    /// [assembly: log4net.Config.Plugin(typeof(JsonPlugin))]
    /// Then you can just use conversion pattern %json or %json{with further options} in your PatternLayout.
    /// Though using SerializedLayout still gives much more flexibility
    /// </remarks>
    /// <author>Robert Sevcik</author>
    public class JsonPlugin : IPlugin
    {
        static bool s_initted = false;

        /// <summary>
        /// Call <see cref="ArrangementConverter.Init"/> and <see cref="LayoutConverter.Init"/> once.
        /// </summary>
        public static void Init()
        {
            if (s_initted) return;
            s_initted = true;
            ArrangementConverter.Init();
            LayoutConverter.Init(serialized: true);
        }

        /// <summary>
        /// Plugin name is the Type's AssemblyQualifiedName
        /// </summary>
        public virtual string Name { get { return this.GetType().AssemblyQualifiedName; } }

        /// <summary>
        /// Attached repository
        /// </summary>
        public ILoggerRepository Repo { get; set; }

        /// <summary>
        /// Create an instance and call <see cref="Init"/>
        /// </summary>
        public JsonPlugin()
        {
            Init();
        }

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
        /// <param name="sender">a repo</param>
        /// <param name="e">ignored</param>
        void Repo_ConfigurationChanged(object sender, EventArgs e)
        {
            var repo = sender as ILoggerRepository;

            repo.RendererMap.Put(typeof(object), JsonObjectRenderer.Default);
            repo.RendererMap.Put(typeof(Exception), new DefaultRenderer());
            
            foreach (var appender in repo.GetAppenders())
            {
                PatternLayout layout = null;

                if (appender is AppenderSkeleton)
                {
                    var anylayout = ((AppenderSkeleton)appender).Layout;
                    if (anylayout == null)
                    {
                        ((AppenderSkeleton)appender).Layout = layout = new SerializedLayout();
                    }
                    else
                    {
                        layout = ((AppenderSkeleton)appender).Layout as PatternLayout;
                    }
                }
                else
                {
                    var prop = appender.GetType().GetProperty("Layout");
                    if (prop != null
                        && typeof(ILayout).IsAssignableFrom(prop.PropertyType)
                        && prop.GetIndexParameters().Length == 0)
                    {
                        var anylayout = prop.GetValue(appender, null);
                        if (anylayout == null)
                        {
                            ((AppenderSkeleton)appender).Layout = layout = new SerializedLayout();
                        }
                        else if (prop.CanWrite)
                        {
                            prop.SetValue(appender, layout = new SerializedLayout(), null);
                        }
                    }
                    else
                    {
                        layout = null;
                    }
                }

                if (layout == null) continue;

                layout.AddConverter(new ConverterInfo() { Name = "json", Type = typeof(JsonPatternConverter) });
                layout.ActivateOptions();
            }
        }
    }
}
