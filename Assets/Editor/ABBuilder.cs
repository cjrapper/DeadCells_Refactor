using UnityEditor;
using UnityEngine;
using System.IO;

public class ABBuilder
{
   //菜单按钮
   [MenuItem("AB包工具/一键打包所有AB包")]
   public static void BuildAllABs()
   {
    //定义AB包输出路径：先用本地云端（StreamingAssets）
    string outputPath = Application.streamingAssetsPath;
    if(!Directory.Exists(outputPath))
    {
        Directory.CreateDirectory(outputPath);
    }

    //**核心代码**：
    //打包AB包,参数：输出路径,压缩方式,平台类型
    BuildPipeline.BuildAssetBundles(
        outputPath, 
        BuildAssetBundleOptions.ChunkBasedCompression, 
        BuildTarget.StandaloneWindows64
        );

    //打包完成提示
    AssetDatabase.Refresh();
    Debug.Log("AB包打包完成!    路径：" + outputPath);
   }
}
