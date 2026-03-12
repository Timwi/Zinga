using System;

namespace Zinga
{
    public class ConstraintKindInfoAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
    }
}