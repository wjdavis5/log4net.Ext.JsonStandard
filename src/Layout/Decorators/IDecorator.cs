using System;
using System.Collections.Generic;
using System.Text;

namespace log4net.Layout.Decorators
{
    /// <summary>
    /// Decorator modifies logged objects
    /// </summary>
    public interface IDecorator
    {
        /// <summary>
        /// decorate logged object
        /// </summary>
        /// <param name="obj">object to decorate</param>
        /// <returns>decorated object</returns>
        object Decorate(object obj);
    }
}
