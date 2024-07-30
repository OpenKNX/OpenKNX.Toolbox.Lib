using System;
using System.Runtime.Serialization;

namespace OpenKNX.Toolbox.Lib.Exceptions;

[Serializable]
public class AttributeNotFoundException : Exception
{
    public AttributeNotFoundException (string message) 
        : base($"Could not find attribute '{message}' on Element.")
    {}

    public AttributeNotFoundException (string message, Exception innerException)
        : base ($"Could not find attribute '{message}' on Element.", innerException)
    {}    
}