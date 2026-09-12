public class AutoBuildTemplate
{
    public static string UIClass =
 @"using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
public class #类名# : MonoBehaviour
{
//auto
   public void Start()
	{
		#查找#
	}
	#成员#
}
";///用来生成UI统一模板
}

