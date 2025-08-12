using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Error
{
    public static readonly Exception WidgetBoundErrorException = new Exception("Widget bound error!");
    public static readonly Exception ComponentNotFoundException = new Exception("Component not found!");
}
