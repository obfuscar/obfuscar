using System;
#if (!SILVERLIGHT)
using System.Runtime.Serialization;

#endif

namespace Obfuscar
{
    [Serializable]
    public class ObfuscarException : Exception
    {
        /// <summary>
        /// Creates a <see cref="ObfuscarException"/>.
        /// </summary>
        public ObfuscarException()
        {
        }

        /// <summary>
        /// Creates a <see cref="ObfuscarException"/> instance with a specific <see cref="string"/>.
        /// </summary>
        /// <param name="message">Message</param>
        public ObfuscarException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a <see cref="ObfuscarException"/> instance with a specific <see cref="string"/> and an <see cref="Exception"/>.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="inner">Inner exception</param>
        public ObfuscarException(string message, Exception inner)
            : base(message, inner)
        {
        }

#if !CF
        /// <summary>
        /// Creates a <see cref="ObfuscarException"/> instance.
        /// </summary>
        /// <param name="info">Info</param>
        /// <param name="context">Context</param>
        protected ObfuscarException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
