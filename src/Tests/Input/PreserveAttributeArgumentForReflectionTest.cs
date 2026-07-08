
using System;
using System.Collections.Generic;
using System.Text;

namespace PreserveAttributeArgumentForReflectionTest
{	
	public class ClassA
	{
        [System.ComponentModel.DisplayName("Enable Other properties")]
        public bool FirstProperty => false;


        [System.ComponentModel.DisplayName("Second Property")]
        [VisibleByAttribute(nameof(FirstProperty))]
        public string SecondProperty { get; set; }
    }

    [System.Reflection.Obfuscation]
    [AttributeUsage(AttributeTargets.Property)]
    public class VisibleByAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisibleByAttribute" /> class.
        /// </summary>
        /// <param name="propertyName">Name of the property that determines the visibility of the attributed property.</param>
        public VisibleByAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; set; }
    }
}
