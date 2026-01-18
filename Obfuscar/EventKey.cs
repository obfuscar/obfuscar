#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using MethodAttributes = System.Reflection.MethodAttributes;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar
{
    class EventKey
    {
        public EventKey(IEvent evt)
            : this(new TypeKey(evt.DeclaringTypeFullName), evt.EventTypeFullName, evt.Name, null, evt)
        {
        }

        public EventKey(MutableEventDefinition evt)
            : this(new TypeKey(evt?.DeclaringType), evt)
        {
        }

        public EventKey(TypeKey typeKey, MutableEventDefinition evt)
            : this(typeKey, evt?.EventType?.GetFullName() ?? string.Empty, evt?.Name ?? string.Empty, evt, evt)
        {
        }

        public EventKey(TypeKey typeKey, string type, string name, MutableEventDefinition eventDefinition)
            : this(typeKey, type, name, eventDefinition, eventDefinition)
        {
        }

        public EventKey(TypeKey typeKey, string type, string name, MutableEventDefinition eventDefinition, IEvent eventAdapter)
        {
            this.TypeKey = typeKey;
            this.Type = type;
            this.Name = name;
            this.Event = eventDefinition;
            this.EventAdapter = eventAdapter;
        }

        public TypeKey TypeKey { get; }

        public string Type { get; }

        public string Name { get; }

        public MethodAttributes AddMethodAttributes
        {
            get
            {
                if (EventAdapter != null)
                    return EventAdapter.AddMethodAttributes;

                return Event.AddMethod != null ? Event.AddMethod.Attributes : 0;
            }
        }

        public MutableTypeDefinition DeclaringType
        {
            get { return Event?.DeclaringType; }
        }

        public MutableEventDefinition Event { get; }

        public IEvent EventAdapter { get; }

        public virtual bool Matches(MutableEventDefinition member)
        {
            if (member != null)
            {
                if (TypeKey.Matches(member.DeclaringType))
                    return Type == member.EventType?.GetFullName() && Name == member.Name;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            EventKey key = obj as EventKey;
            if (key == null)
                return false;

            return this == key;
        }

        public static bool operator ==(EventKey a, EventKey b)
        {
            if ((object) a == null)
                return (object) b == null;
            else if ((object) b == null)
                return false;
            else
                return a.TypeKey == b.TypeKey && a.Type == b.Type && a.Name == b.Name;
        }

        public static bool operator !=(EventKey a, EventKey b)
        {
            if ((object) a == null)
                return (object) b != null;
            else if ((object) b == null)
                return true;
            else
                return a.TypeKey != b.TypeKey || a.Type != b.Type || a.Name != b.Name;
        }

        public override int GetHashCode()
        {
            return TypeKey.GetHashCode() ^ Type.GetHashCode() ^ Name.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1} {2}::{3}", TypeKey.Scope, Type, TypeKey.Fullname, Name);
        }
    }
}
