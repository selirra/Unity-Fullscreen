using UnityEngine;
using UnityEditor;

public class MaskGeneratorWindow : EditorWindow
{
    private Texture2D metallicTexture;
    private Texture2D aoTexture;
    private Texture2D detailMaskTexture;
    private Texture2D smoothnessTexture;

    private float metallicValue = 0.5f;
    private float aoValue = 0.5f;
    private float smoothnessValue = 0.5f;
    private bool isRoughnessMap = false;


    [MenuItem("Tools/Mask Map Generator")]
    public static void ShowWindow(){
        var window = GetWindow<MaskGeneratorWindow>("Mask Map Generator");
        window.minSize = new Vector2(300f, 720f);
    }

    private void OnGUI(){
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
        if (smoothnessTexture == null) smoothnessValue = EditorGUILayout.Slider(smoothnessValue, 0f, 1f);
        else isRoughnessMap = EditorGUILayout.Toggle("Roughness Map", isRoughnessMap);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Generate Mask Map")){
            if (metallicTexture == null && aoTexture == null && detailMaskTexture == null && smoothnessTexture == null){
                Debug.LogError("Please assign at least one input texture before generating the mask map.");
                return;
            }

            MaskGenerator generator = new MaskGenerator{
                metallicTexture = metallicTexture,
                aoTexture = aoTexture,
                detailMaskTexture = detailMaskTexture,
                smoothnessTexture = smoothnessTexture,

                metallicValue = metallicValue,
                aoValue = aoValue,
                smoothnessValue = smoothnessValue,

                isRoughnessMap = isRoughnessMap

            };

            Texture2D maskMap = generator.GenerateMaskMap();
            string path = SaveMaskMap(maskMap);
            SetImportSettings(path);
        }

        if (GUILayout.Button("Clear")){
            metallicTexture = null;
            metallicValue = 0.5f;

            aoTexture = null;
            aoValue = 0.5f;

            detailMaskTexture = null;

            smoothnessTexture = null;
            smoothnessValue = 0.5f;
            isRoughnessMap = false;
        }

        GUILayout.Space(10f);
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
    public Texture2D[] texArray = new Texture2D[4]; 

    public Texture2D metallicTexture;
    public float metallicValue;

    public Texture2D aoTexture;
    public float aoValue;

    public Texture2D detailMaskTexture;

    public Texture2D smoothnessTexture;
    public float smoothnessValue;
    public bool isRoughnessMap;

    public Texture2D GenerateMaskMap(){
        texArray[0] = metallicTexture;
        texArray[1] = aoTexture;
        texArray[2] = detailMaskTexture;
        texArray[3] = smoothnessTexture;

        int width = 0;
        int height = 0;

        foreach (Texture2D texture in texArray){
            if (texture != null){
                width = texture.width;
                height = texture.height;
                break;
            }
        }

        PrepTexture(true);

        Color[] metallicPixels = GetPixels(metallicTexture, width, height, metallicValue, false);
        Color[] aoPixels = GetPixels(aoTexture, width, height, aoValue, false);
        Color[] detailMaskPixels = GetPixels(detailMaskTexture, width, height, 0f, false);
        Color[] smoothnessPixels = GetPixels(smoothnessTexture, width, height, smoothnessValue, isRoughnessMap);

        Color[] maskMapPixels = new Color[width * height];

        for (int i = 0; i < maskMapPixels.Length; i++){
            maskMapPixels[i] = new Color(metallicPixels[i].r, aoPixels[i].g, detailMaskPixels[i].b, smoothnessPixels[i].r);
        }

        Texture2D maskMap = new Texture2D(width, height, TextureFormat.RGBA32, false);
        maskMap.SetPixels(maskMapPixels);
        maskMap.Apply();

        PrepTexture(false);
        return maskMap;
    }

    private void PrepTexture(bool value){
        foreach (Texture2D texture in texArray){
            if (texture == null) continue;
                
            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null){
                importer.isReadable = value;
                importer.SaveAndReimport();
            }
        }
    }

    private Color[] GetPixels(Texture2D texture, int width, int height, float value, bool invert){
        if (texture != null){
            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++) {
                float grayscale = pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f;
                if (invert) grayscale = 1 - grayscale;
                pixels[i] = new Color(grayscale, grayscale, grayscale);
            }
            return pixels;
        }

        Color[] defaultPixels = new Color[width * height];
            
        for (int i = 0; i < defaultPixels.Length; i++) defaultPixels[i] = new Color(value, value, value);

        return defaultPixels;
    }
}
