using System;

namespace Zinga
{
    public class ConstraintKindInfoAttribute : Attribute
    {
        public ConstraintKindInfoAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}