using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class AutoBuild
{

    [MenuItem("生成/创建或刷新界面")]
    public static void BuildUIScript()
    {

        var dicUIType = new Dictionary<string, string>();

        dicUIType.Add("Img", "Image");
        dicUIType.Add("Btn", "Button");
        dicUIType.Add("Txt", "Text");
        dicUIType.Add("Tran", "Transform");


        GameObject[] selectobjs = Selection.gameObjects;

        foreach (GameObject go in selectobjs)
        {
            //选择的物体
            GameObject selectobj = go.transform.root.gameObject;

            //物体的子物体
            Transform[] _transforms = selectobj.GetComponentsInChildren<Transform>(true);

            List<Transform> childList = new List<Transform>(_transforms);

            //UI需要查询的物体
            var mainNode = from trans in childList where trans.name.Contains('_') && dicUIType.Keys.Contains(trans.name.Split('_')[0]) select trans;

            var nodePathList = new Dictionary<string, string>();

            //循环得到物体路径
            foreach (Transform node in mainNode)
            {
                Transform tempNode = node;
                string nodePath = "/" + tempNode.name;

                while (tempNode != tempNode.root)
                {
                    tempNode = tempNode.parent;

                    int index = nodePath.IndexOf('/');

                    nodePath = nodePath.Insert(index, "/" + tempNode.name);
                }

                nodePathList.Add(node.name, nodePath);
            }

            //成员变量字符串
            string memberstring = "";
            //查询代码字符串
            string loadedcontant = "";

            foreach (Transform itemtran in mainNode)
            {
                string typeStr = dicUIType[itemtran.name.Split('_')[0]];

                memberstring += "public " + typeStr + " " + itemtran.name + " = null;\r\n\t";

                loadedcontant += itemtran.name + " = " + "gameObject.transform.Find(\"" + nodePathList[itemtran.name] + "\").GetComponent<" + typeStr + ">();\r\n\t\t";
            }


            string scriptPath = Application.dataPath + "/Scripts/" + selectobj.name + ".cs";


            string classStr = "";

            //如果已经存在了脚本，则只替换//auto下方的字符串
            if (File.Exists(scriptPath))
            {
                FileStream classfile = new FileStream(scriptPath, FileMode.Open);
                StreamReader read = new StreamReader(classfile);
                classStr = read.ReadToEnd();
                read.Close();
                classfile.Close();
                File.Delete(scriptPath);

                string splitStr = "//auto";
                string unchangeStr = Regex.Split(classStr, splitStr, RegexOptions.IgnoreCase)[0];
                string changeStr = Regex.Split(AutoBuildTemplate.UIClass, splitStr, RegexOptions.IgnoreCase)[1];

                StringBuilder build = new StringBuilder();
                build.Append(unchangeStr);
                build.Append(splitStr);
                build.Append(changeStr);
                classStr = build.ToString();
            }
            else
            {
                classStr = AutoBuildTemplate.UIClass;
            }

            classStr = classStr.Replace("#类名#", selectobj.name);
            classStr = classStr.Replace("#查找#", loadedcontant);
            classStr = classStr.Replace("#成员#", memberstring);


            FileStream file = new FileStream(scriptPath, FileMode.CreateNew);
            StreamWriter fileW = new StreamWriter(file, System.Text.Encoding.UTF8);
            fileW.Write(classStr);
            fileW.Flush();
            fileW.Close();
            file.Close();


            Debug.Log("创建脚本 " + Application.dataPath + "/Scripts/" + selectobj.name + ".cs 成功!");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

}