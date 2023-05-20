#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class MaskGeneratorWindow : EditorWindow
{
    private static ComputeShader shader;
    private bool useComputeShader = true;

    private Texture2D metallicTexture;
    private Texture2D aoTexture;
    private Texture2D detailMaskTexture;
    private Texture2D smoothnessTexture;

    private float metallicValue = 0.5f;
    private float aoValue = 0.5f;
    private float smoothnessValue = 0.5f;
    private bool isRoughnessMap = false;

    private Vector2 scrollPosition = Vector2.zero;

    [MenuItem("Tools/Mask Map Generator")]
    public static void ShowWindow(){
        InitializeWindow();
        var window = GetWindow<MaskGeneratorWindow>("Mask Map Generator");
        window.minSize = new Vector2(300f,300f);
    }

    private static bool InitializeWindow(){
        string[] script = System.IO.Directory.GetFiles(Application.dataPath, "MaskGenerator.cs", SearchOption.AllDirectories);

        if (script.Length == 0){
            Debug.LogError("MaskGenerator script path not found.");
            return false;
        }

        string shaderPath = Path.Combine(Path.GetDirectoryName(script[0]), "MaskGenerator.compute");
        string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string relativeShaderPath = shaderPath.Replace(projectPath, "").TrimStart(Path.DirectorySeparatorChar);
        shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(relativeShaderPath);
        return true;
    }

    private void OnGUI(){
        useComputeShader = EditorGUILayout.Toggle("Use compute shader", useComputeShader);
        float minHeight = position.height;
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(minHeight));

        GUILayout.Label("Input Textures", EditorStyles.boldLabel);

        GUILayoutOption[] textureOptions = new GUILayoutOption[4];
        textureOptions[0] = GUILayout.Width(120f);
        textureOptions[1] = GUILayout.Height(120f);
        textureOptions[2] = GUILayout.ExpandWidth(false);
        textureOptions[3] = GUILayout.ExpandHeight(false);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("(R) Metallic");
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        metallicTexture = (Texture2D)EditorGUILayout.ObjectField(metallicTexture, typeof(Texture2D), false, textureOptions);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        if (metallicTexture == null) metallicValue = EditorGUILayout.Slider(metallicValue, 0f, 1f);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("(G) Ambient Occlusion");
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        aoTexture = (Texture2D)EditorGUILayout.ObjectField(aoTexture, typeof(Texture2D), false, textureOptions);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        if (aoTexture == null) aoValue = EditorGUILayout.Slider(aoValue, 0f, 1f);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("(B) Detail Mask");
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        detailMaskTexture = (Texture2D)EditorGUILayout.ObjectField(detailMaskTexture, typeof(Texture2D), false, textureOptions);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("(A) Smoothness");
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        smoothnessTexture = (Texture2D)EditorGUILayout.ObjectField(smoothnessTexture, typeof(Texture2D), false, textureOptions);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (smoothnessTexture == null) {
            isRoughnessMap = false;
            smoothnessValue = EditorGUILayout.Slider(smoothnessValue, 0f, 1f);
        }
        else isRoughnessMap = EditorGUILayout.Toggle("Roughness Map", isRoughnessMap);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Generate Mask Map")) GenerateMaskMap();

        if (GUILayout.Button("Clear")) ClearWindow();

        GUILayout.Space(25f);
        EditorGUILayout.EndScrollView();
    }

    private void ClearWindow(){
        metallicTexture = null;
        metallicValue = 0.5f;

        aoTexture = null;
        aoValue = 0.5f;

        detailMaskTexture = null;

        smoothnessTexture = null;
        smoothnessValue = 0.5f;
        isRoughnessMap = false;
    }

    private void GenerateMaskMap(){
        InitializeWindow();
        if (metallicTexture == null && aoTexture == null && detailMaskTexture == null && smoothnessTexture == null){
            Debug.LogError("Please assign at least one input texture before generating the mask map.");
            return;
        }

        if (useComputeShader) Debug.Log("Generating mask on gpu.");
        else Debug.Log("Generating mask on cpu");

        Texture2D maskMap;

        int width = 0;
        int height = 0;

        var texArray = new Texture2D[]{metallicTexture, aoTexture, detailMaskTexture, smoothnessTexture};
        foreach (Texture2D tex in texArray){
            if (tex == null) continue;
            width = tex.width;
            height = tex.height;
            break;
        }

        PrepTexture(texArray, true);

        if (useComputeShader){
            if (metallicTexture != null) shader.SetTexture(0, "metallic", metallicTexture);
            else shader.SetTexture(0, "metallic", CreateBlank(metallicValue, width, height));

            if (aoTexture != null) shader.SetTexture(0, "ao", aoTexture);
            else shader.SetTexture(0, "ao", CreateBlank(aoValue, width, height));

            if (detailMaskTexture != null) shader.SetTexture(0, "detail", detailMaskTexture);
            else shader.SetTexture(0, "detail", CreateBlank(0f, width, height)); 

            if (smoothnessTexture != null) shader.SetTexture(0, "smoothness", smoothnessTexture);
            else shader.SetTexture(0, "smoothness", CreateBlank(smoothnessValue, width, height));

            shader.SetBool("isRoughness", isRoughnessMap);

            RenderTexture render = new RenderTexture(width, height, 0);
            render.enableRandomWrite = true;
            render.Create();
            shader.SetTexture(0, "Result", render);

            int threadGroupsX = Mathf.CeilToInt(width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(height / 8.0f);
            shader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

            maskMap = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture.active = render;
            maskMap.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            maskMap.Apply();
        } else {
            MaskGenerator generator = new MaskGenerator{
                width = width,
                height = height,
                texArray = texArray,
                metallicTexture = metallicTexture,
                aoTexture = aoTexture,
                detailMaskTexture = detailMaskTexture,
                smoothnessTexture = smoothnessTexture,
                metallicValue = metallicValue,
                aoValue = aoValue,
                smoothnessValue = smoothnessValue,
                isRoughnessMap = isRoughnessMap
            };
            maskMap = generator.GenerateMaskMap();
        }
        PrepTexture(texArray, false);

        string path = SaveMaskMap(maskMap);
        SetImportSettings(path);
    }

    private void PrepTexture(Texture2D[] texArray, bool value){
        foreach (Texture2D texture in texArray){
            if (texture == null) continue;
                
            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null){
                importer.isReadable = value;
                importer.sRGBTexture = !value;
                importer.SaveAndReimport();
            }
        }
    }

    private Texture2D CreateBlank(float val, int width, int height){
        var color = new Color(val, val, val, 1);
        var tmpTex = new Texture2D(width, height);
        var colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++){
            colors[i] = color;
        }
        tmpTex.SetPixels(colors);
        tmpTex.Apply();
        return tmpTex;
    }

    private string SaveMaskMap(Texture2D maskMap){
        string path = EditorUtility.SaveFilePanelInProject("Save Mask Map", "mask_map", "png", "Please enter a file name to save the mask map to");
        if (path.Length > 0){
            byte[] pngData = maskMap.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
            AssetDatabase.Refresh();
        }
        return path;
    }

    private void SetImportSettings(string path){
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null){
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }
}

public class MaskGenerator
{
    public int width = 0;
    public int height = 0;
    public Texture2D[] texArray;

    public Texture2D metallicTexture;
    public float metallicValue;

    public Texture2D aoTexture;
    public float aoValue;

    public Texture2D detailMaskTexture;

    public Texture2D smoothnessTexture;
    public float smoothnessValue;
    public bool isRoughnessMap;

    public Texture2D GenerateMaskMap(){

        Color[] maskMapPixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++){
            int i = y * width + x;
            float metallicPixel = metallicTexture != null ? GetPixel(metallicTexture, new Vector2Int(x, y), false) : metallicValue;
            float aoPixel = aoTexture != null ? GetPixel(aoTexture, new Vector2Int(x, y), false) : aoValue;
            float detailMaskPixel = detailMaskTexture != null ? GetPixel(detailMaskTexture, new Vector2Int(x, y), false) : 0f;
            float smoothnessPixel = smoothnessTexture != null ? GetPixel(smoothnessTexture, new Vector2Int(x, y), isRoughnessMap) : smoothnessValue;
            maskMapPixels[i] = new Color(metallicPixel, aoPixel, detailMaskPixel, smoothnessPixel);
        }

        Texture2D maskMap = new Texture2D(width, height, TextureFormat.RGBA32, false);
        maskMap.SetPixels(maskMapPixels);
        maskMap.Apply();

        return maskMap;
    }

    private float GetPixel(Texture2D tex, Vector2Int pixel, bool invert){
        float greyscale = tex.GetPixel(pixel.x, pixel.y).r * 0.299f + tex.GetPixel(pixel.x, pixel.y).g * 0.587f + tex.GetPixel(pixel.x, pixel.y).b *0.114f;
        return invert ? 1 - greyscale : greyscale;
    }
}
#endif