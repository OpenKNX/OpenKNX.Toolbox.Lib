using System;
using System.Runtime.Serialization;

namespace OpenKNX.Toolbox.Lib.Exceptions;

[Serializable]
public class ElementNotFoundException : Exception
{
    public ElementNotFoundException (string message) 
        : base($"Could not find element '{message}'.")
    {}

    public ElementNotFoundException (string message, Exception innerException)
        : base ($"Could not find element '{message}'.", innerException)
    {}    
}