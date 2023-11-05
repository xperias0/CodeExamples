/*
 * Document:#ModelSerializaioner.cs#
 * Author: Yuyang Qiu
 * Function:Serialize and deserialize static model or model with skeleton.
 */

using Antlr4.Runtime.Misc;
using LibTessDotNet;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using SystemHalf;
using TriLibCore.Extensions;
using TriLibCore.Samples;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public static class ModelSerializer
{
  
    /// <summary>
    /// Type of model,static or avatar model.
    /// </summary>
    enum ModelType { 
        
        StaticModel=0,
        AvatarModel = 1,
        max,
    }
    
    /// <summary>
    /// Type of component that attach on current gameobject.
    /// </summary>
    enum ComponentType
    {
        None = 0,
        MeshFilter = 1,
        MeshRender = 2,
        ColliderHelper = 3,
        Animator = 4,
        SkinnedMeshRenderer = 5,
        Max,
    }
    
    /// <summary>
    /// Material Type enum.
    /// </summary>
    public enum MaterialType
    {
        None = 0,
        Lit = 1,       
        VC = 2,         // Vertex Color
        Glass = 3,      
        Metal = 4,      
        Emission = 5,   
        Max,
    }

    /// <summary>
    /// Collider type enum.
    /// </summary>
    public enum ColliderType
    {
        None = 0,       // Ã»ÓÐÅö×²
        Box = 1,        // ºÐ×Ó
        Sphere = 2,     // ÇòÌå
        Capsule = 3,    // ½ºÄÒ
        Mesh = 4,       // Ä£ÐÍ

        Max,
    }

    public enum TextureType
    {
        NoAlpha = 0,
        SplitAlpha = 1,
        Max,
    }

    private static Dictionary<string, MaterialType> ShaderNameMaterialDic = new Dictionary<string, MaterialType>
    {
        {"HDRP/Lit", MaterialType.Lit},
        {"Shader Graphs/VC", MaterialType.VC},
        {"Shader Graphs/Metal", MaterialType.Metal},
        {"Shader Graphs/Glass", MaterialType.Glass},
        {"Shader Graphs/Emission", MaterialType.Emission},
    };


    enum MaterialAttribute
    {
        None = 0,
        BaseColorMap = 1,
        BaseColor = 2,
        AlphaClipping = 3,
        Metallic = 4,
        Smoothness = 5,
        NormalMap = 6,
        EmissiveColorLDR = 7,

        Max,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="root">The root transform</param>
    /// <param name="outputStream">The stream write to.</param>
    /// <param name="icon">The image of the game object</param>
    public static void Serialize(Transform root, Stream outputStream, Texture2D icon)
    {
        using (GZipStream zipStream = new GZipStream(outputStream, CompressionMode.Compress))
        {
            using (BinaryWriter writer = new BinaryWriter(zipStream, Encoding.UTF8))
            {
               
                byte childCount = (byte)root.childCount;
                if (childCount == 0)
                {
                    return;
                }
                // Ô¤Áô²ÎÊý
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)TextureType.SplitAlpha); //Çø·ÖÍ¸Ã÷Í¨µÀ´æ´¢

               
                Dictionary<int, Texture2D> textureDic = new Dictionary<int, Texture2D>();
                if (icon != null)
                {
                    textureDic.Add(0, icon);
                }
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    Texture2D tex = (Texture2D)renderers[i].material.GetTexture("_BaseColorMap");
                    if (tex && !textureDic.ContainsKey(tex.GetInstanceID()))
                    {
                        textureDic.Add(tex.GetInstanceID(), tex);
                    }

                    tex = (Texture2D)renderers[i].material.GetTexture("_NormalMap");
                    if (tex && !textureDic.ContainsKey(tex.GetInstanceID()))
                    {
                        textureDic.Add(tex.GetInstanceID(), tex);
                    }

                }

                writer.Write((byte)textureDic.Count);
                foreach (KeyValuePair<int, Texture2D> pair in textureDic)
                {
                    writer.Write(pair.Key);
                    byte[] texture = pair.Value.EncodeToJPG(60);
                    writer.Write((uint)texture.Length);
                    writer.Write(texture);

                    Color32[] color32s = pair.Value.GetPixels32();
                    for(int i = 0; i<color32s.Length;i++)
                    {
                        color32s[i].r = color32s[i].a;
                        color32s[i].g = color32s[i].a;
                        color32s[i].b = color32s[i].a;
                    }
                    Texture2D alphaChannel = new Texture2D(pair.Value.width, pair.Value.height);
                    alphaChannel.SetPixels32(color32s);
                    alphaChannel.Apply();
                    byte[] alphaChannelData = alphaChannel.EncodeToJPG(95);
                    writer.Write((uint)alphaChannelData.Length);
                    writer.Write(alphaChannelData);

                }



                writer.Write(childCount);

                for (int i = 0; i < childCount; i++)
                {
                    SerializeRecursion(root.GetChild(i), writer);
                }
            }
        }

    }

    
    /// <summary>
    /// Serialize game object and its child object.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="writer"></param>
    private static void SerializeRecursion(Transform obj, BinaryWriter writer)
    {
        writer.Write(obj.name);
        SerializeVector3(obj.localPosition, writer);
        SerializeVector3(obj.localEulerAngles, writer);
        SerializeVector3(obj.localScale, writer);

        SerializeComponents(obj, writer);

        byte childCount = (byte)obj.childCount;
        writer.Write(childCount);

        for(int i = 0; i < childCount; i++)
        {
            SerializeRecursion(obj.GetChild(i), writer);
        }
    }
    
    
    /// <summary>
    /// Serialize all component.
    /// </summary>
    /// <param name="obj">object to serialize</param>
    /// <param name="writer"></param>
    private static void SerializeComponents(Transform obj, BinaryWriter writer)
    {
        List<Component> ComponentList = new List<Component>();

        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if(meshFilter != null)
        {
            ComponentList.Add(meshFilter);
        }
        MeshRenderer meshRender = obj.GetComponent<MeshRenderer>();
        if(meshRender != null)
        {
            ComponentList.Add(meshRender);
        }

        ColliderHelper colliderHelper = obj.GetComponent<ColliderHelper>();
        if(colliderHelper != null)
        {
            ComponentList.Add(colliderHelper);
        }


        byte componentCount = (byte)ComponentList.Count;
        writer.Write(componentCount);
        if (meshFilter != null)
        {
            bool success = SerializeMeshFilter(meshFilter, writer);
        }

        if (meshRender != null)
        {
            SerializeMeshRender(meshRender, writer);
        }

        if(colliderHelper != null)
        {
            SerializeColliderHelper(colliderHelper, writer);
        }

    }

    private static void SerializeAnimator(Transform obj,BinaryWriter writer) {
        Animator animator = obj.GetComponent<Animator>();
        SerializeAnimatorController(animator,writer);
        
    }

    private static void SerializeAnimatorController(Animator ani,BinaryWriter writer) {
        UnityEditor.Animations.AnimatorController ac = ani.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        
        var root = ac.layers[0].stateMachine;
        
        List<string> states = new List<string>();
        List<string> clipPath = new List<string>();
        writer.Write((byte)root.states.Length);

        for (int i=0;i<root.states.Length;i++) {
            states.Add(root.states[i].state.name);
            clipPath.Add(AssetDatabase.GetAssetPath(root.states[i].state.motion)) ;
        }

        for (int i = 0; i < states.Count; i++) {
            writer.Write(states[i]);
          //  AssetDatabase.GetAssetPath();
            writer.Write(clipPath[i]);
        }
    }

    private static bool SerializeMeshFilter(MeshFilter meshFilter, BinaryWriter writer)
    {
        byte componentType = (byte)ComponentType.MeshFilter;
        writer.Write(componentType);

        Vector3[] vertices = meshFilter.mesh.vertices;
        int[] triangles = meshFilter.mesh.triangles;
        Vector3[] normals = meshFilter.mesh.normals;
        Vector2[] uvs = meshFilter.mesh.uv;
        Color32[] color32s = meshFilter.mesh.colors32;

        //Vector2[] uvs = meshFilter.mesh.uv;
        Bounds bounds = meshFilter.mesh.bounds;

        uint length = 0;
        if (vertices.Length > ushort.MaxValue)
        {
            writer.Write(length);
            return false;
        }

        //TODO:³¤¶È
        writer.Write((uint)0);

        writer.Write((ushort)vertices.Length);
        for(int i = 0; i < vertices.Length;i++)
        {
            SerializeVector3(vertices[i], writer);
        }

        for(int i = 0; i < vertices.Length; i++)
        {
            SerializeVector3(normals[i], writer);
        }

        writer.Write((ushort)color32s.Length);
        for (int i = 0; i < color32s.Length; i++)
        {
            Color32 color = color32s[i];
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }


        if (uvs != null)
        {
            writer.Write((ushort)uvs.Length);
            for (int i = 0; i < uvs.Length; i++)
            {
                SerializeVector2(uvs[i], writer);
            }
        }
        else
        {
            writer.Write((ushort)0);
        }



        writer.Write((uint)triangles.Length);
        for(int i = 0; i < triangles.Length; i++)
        {
            writer.Write((ushort)triangles[i]);
        }

        SerializeVector3(bounds.center, writer);
        SerializeVector3(bounds.size, writer);

        return true;
    }

    private static bool SerializeMeshRender(MeshRenderer meshRender, BinaryWriter writer)
    {
        byte componentType = (byte)ComponentType.MeshRender;
        writer.Write(componentType);

        using (var stream = new MemoryStream())
        {
            using (var subWriter = new BinaryWriter(stream, Encoding.UTF8, false))
            {
                byte matCount = (byte)meshRender.materials.Length;
                subWriter.Write(matCount);
                for (int i = 0; i < matCount; i++)
                {
                    Material mat = meshRender.materials[i];
                   
                    string shaderName = mat.shader.name;
                    MaterialType materialType = MaterialType.Lit;
                    ShaderNameMaterialDic.TryGetValue(shaderName, out materialType);

                    subWriter.Write((byte)materialType);

                    Texture2D tex = (Texture2D)mat.GetTexture("_BaseColorMap");
                    subWriter.Write((int)(tex != null ? tex.GetInstanceID(): 0));
                    
                    tex = (Texture2D)mat.GetTexture("_NormalMap");
                    subWriter.Write((int)(tex != null ? tex.GetInstanceID() : 0));
                    
                    subWriter.Write((byte)3);

                    byte instensity = FloatToByte(mat.GetFloat("_Intensity"), 2.0f);
                    subWriter.Write(instensity);
                    byte wind = FloatToByte(mat.GetFloat("_Wind"),1.0f);
                    subWriter.Write(wind);
                    byte pulse = FloatToByte(mat.GetFloat("_Pulse"),1.0f);
                    subWriter.Write(pulse);
                }

                writer.Write((uint)stream.Length);
                writer.Write(stream.ToArray());
            }
        }     

        return true;
    }

    private static bool SerializeSkinnedMeshRender(Transform obj, BinaryWriter writer) {

        Mesh mesh = obj.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        SkinnedMeshRenderer sr = obj.GetComponent<SkinnedMeshRenderer>();
     
        Transform[] bones = sr.bones;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        Color32[] color32s = mesh.colors32;
        BoneWeight[] boneWeight = mesh.boneWeights;
        Matrix4x4[] bindPos = mesh.bindposes;
        //Vector2[] uvs = meshFilter.mesh.uv;
        Bounds bounds =mesh.bounds;

      
        uint length = 0;
        if (vertices.Length > ushort.MaxValue)
        {
            writer.Write(length);
            return false;
        }
        

        writer.Write((ushort)vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            SerializeVector3(vertices[i], writer);
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            SerializeVector3(normals[i], writer);
        }

        writer.Write((ushort)color32s.Length);
        for (int i = 0; i < color32s.Length; i++)
        {
            Color32 color = color32s[i];
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }

        writer.Write((ushort)boneWeight.Length);
        for (int i=0;i<boneWeight.Length;i++) {
         
            writer.Write((ushort)boneWeight[i].boneIndex0);
            writer.Write((ushort)boneWeight[i].boneIndex1);
            writer.Write((ushort)boneWeight[i].boneIndex2);
            writer.Write((ushort)boneWeight[i].boneIndex3);
            SerializeFloat(boneWeight[i].weight0,writer);
            SerializeFloat(boneWeight[i].weight1, writer);
            SerializeFloat(boneWeight[i].weight2, writer);
            SerializeFloat(boneWeight[i].weight3, writer);
            
        }

        writer.Write((ushort)bindPos.Length);
        for (int i = 0; i < bindPos.Length; i++)
        {
            SerializeFloat(bindPos[i].m00,writer);
            SerializeFloat(bindPos[i].m01, writer);
            SerializeFloat(bindPos[i].m02, writer);
            SerializeFloat(bindPos[i].m03, writer);
            SerializeFloat(bindPos[i].m10, writer);
            SerializeFloat(bindPos[i].m11, writer);
            SerializeFloat(bindPos[i].m12, writer);
            SerializeFloat(bindPos[i].m13, writer);
            SerializeFloat(bindPos[i].m20, writer);
            SerializeFloat(bindPos[i].m21, writer);
            SerializeFloat(bindPos[i].m22, writer);
            SerializeFloat(bindPos[i].m23, writer);
            SerializeFloat(bindPos[i].m30, writer);
            SerializeFloat(bindPos[i].m31, writer);
            SerializeFloat(bindPos[i].m32, writer);
            SerializeFloat(bindPos[i].m33, writer);
        }

        writer.Write((ushort)bones.Length);
        
        for (int i = 0; i < bones.Length; i++)
        {
            writer.Write(bones[i].name);
            writer.Write(bones[i].parent.name);
            serializeTransform(bones[i], writer);
   
        }

        writer.Write((ushort)mesh.blendShapeCount);
        writer.Write((ushort)mesh.vertices.Length);
        for (int i=0;i<mesh.blendShapeCount;i++) {
            Vector3[] deltaVertices = new Vector3[mesh.vertices.Length];
            Vector3[] deltaNormals = new Vector3[mesh.vertices.Length];
            Vector3[] deltaTangents = new Vector3[mesh.vertices.Length];

            mesh.GetBlendShapeFrameVertices(i, 100, deltaVertices, deltaNormals, deltaTangents);

            writer.Write(mesh.GetBlendShapeName(i));
            SerializeVector3Array(deltaVertices,writer);
            SerializeVector3Array(deltaNormals, writer);
            SerializeVector3Array(deltaTangents, writer);

        }


        if (uvs != null)
        {
            writer.Write((ushort)uvs.Length);
            for (int i = 0; i < uvs.Length; i++)
            {
                SerializeVector2(uvs[i], writer);
            }
        }
        else
        {
            writer.Write((ushort)0);
        }



        writer.Write((uint)triangles.Length);
        for (int i = 0; i < triangles.Length; i++)
        {
            writer.Write((ushort)triangles[i]);
        }

        SerializeVector3(bounds.center, writer);
        SerializeVector3(bounds.size, writer);

        Transform rootBone =obj.GetComponent<SkinnedMeshRenderer>().rootBone;
        writer.Write(obj.name);
        writer.Write(rootBone.name);
        writer.Write(mesh.name);
        return true;


        
    }

    private static void SerializeVector3Array(Vector3[] array,BinaryWriter writer) {
        for (int i=0;i<array.Length;i++) {
            SerializeVector3(array[i],writer);        
        }
    
    }
    public static void SerializeColliderHelper(ColliderHelper colliderHelper, BinaryWriter writer)
    {
        byte componentType = (byte)ComponentType.ColliderHelper;
        writer.Write(componentType);
        writer.Write((uint)13);
        writer.Write((byte)colliderHelper.ColliderType);
        SerializeVector3(colliderHelper.Param1, writer);
        SerializeVector3(colliderHelper.Param2, writer);
    }
    
    
    /// <summary>
    /// convert float to byte
    /// </summary>
    /// <param name="value"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    private static byte FloatToByte(float value, float max)
    {
        return (byte)(value / max * 255);
    }

    private static float ByteToFloat(byte value, float max)
    {
        return (float)value / 255.0f * max;
    }

    private static void SerializeFloat(float value, BinaryWriter writer) {
    Half x = new Half(value);
    writer.Write(Half.GetBytes(x));
    }

    private static void SerializeVector3(Vector3 vec3, BinaryWriter writer)
    {
        Half x = new Half(vec3.x);
        Half y = new Half(vec3.y);
        Half z = new Half(vec3.z);

        writer.Write(Half.GetBytes(x));
        writer.Write(Half.GetBytes(y));
        writer.Write(Half.GetBytes(z));
        
    }


    private static void SerializeVector2(Vector2 vec2, BinaryWriter writer)
    {
        Half x = new Half(vec2.x);
        Half y = new Half(vec2.y);

        writer.Write(Half.GetBytes(x));
        writer.Write(Half.GetBytes(y));
    }

    
    /// <summary>
    /// Deserialize file.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="inputStream"></param>
    /// <returns></returns>
    public static Texture2D Deserialize(GameObject root, Stream inputStream)
    {
        Texture2D icon = null;
        using (GZipStream zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            icon = ReadZipStream(root, zipStream);
        }

        return icon;
    }

    static Texture2D ReadZipStream(GameObject root, GZipStream zipStream)
    {
        Texture2D icon = null;
        using (BinaryReader reader = new BinaryReader(zipStream, Encoding.UTF8))
        {
            
            
            byte param2 = reader.ReadByte();
            byte param3 = reader.ReadByte();
            TextureType textureType = (TextureType)reader.ReadByte();

            Dictionary<int, Texture2D> texDic = new Dictionary<int, Texture2D>();
            byte textureCount = reader.ReadByte();
            for (int i = 0; i < textureCount; i++)
            {
                int id = reader.ReadInt32();
                uint length = reader.ReadUInt32();
                byte[] data = reader.ReadBytes((int)length);

                Texture2D tex = new Texture2D(1, 1);
                tex.LoadImage(data);
                

                if (textureType == TextureType.SplitAlpha)
                {
                    uint alphaLength = reader.ReadUInt32();
                    byte[] alphaData = reader.ReadBytes((int)alphaLength);
                    Texture2D alphaTex = new Texture2D(1, 1);
                    alphaTex.LoadImage(alphaData);

                    Color32[] colors = tex.GetPixels32();
                    Color32[] alphas = alphaTex.GetPixels32();
                    for(int j = 0; j<colors.Length; j++)
                    {
                        colors[j] =new Color32(colors[j].r, colors[j].g, colors[j].b, alphas[j].r);
                    }

                    tex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
                    tex.SetPixels32(colors);
                    tex.Apply();

                }

                texDic.Add(id, tex);

            }

            if (texDic.ContainsKey(0))
            {
                icon = texDic[0];
            }

            byte nodeCount = reader.ReadByte();
            Debug.Log("Count: "+nodeCount);
            for (int i = 0; i < nodeCount; i++)
            {
                DeserializeRecursion(root, reader, texDic);
            }
        }
        GameObject.Find("IconPic").GetComponent<RawImage>().texture = icon;
        return icon;
    }

    private static void DeserializeRecursion(GameObject root, BinaryReader reader, Dictionary<int, Texture2D> texDic)
    {
        string name = reader.ReadString();
        Debug.Log("DeseriaL: "+name);
        GameObject obj = new GameObject(name);
        obj.transform.parent = root.transform;

        obj.transform.localPosition = DeserializeVector3(reader);
        obj.transform.localEulerAngles = DeserializeVector3(reader);
        obj.transform.localScale = DeserializeVector3(reader);

        DeserializeComponents(obj, reader,texDic);

        byte childCount = reader.ReadByte();
        for(int i = 0; i < childCount; i++)
        {
            DeserializeRecursion(obj, reader, texDic);
        }

    }

    private static void DeserializeComponents(GameObject obj, BinaryReader reader, Dictionary<int, Texture2D> texDic)
    {
        byte componentsCount = reader.ReadByte();
        for(int i = 0; i < componentsCount; i++)
        {
            ComponentType componentType = (ComponentType)reader.ReadByte();
            uint length = reader.ReadUInt32();

            switch (componentType)
            {
                case ComponentType.MeshFilter:
                    DeserializeMeshFilter(obj, reader, length);
                    break;
                case ComponentType.MeshRender:
                    DeserializeMeshRender(obj, reader, length, texDic);
                    break;
                case ComponentType.ColliderHelper:
                    DeserializeColliderHelper(obj, reader, length);
                    break;
                default:
                    reader.ReadBytes((int)length);
                    break;
            }
        }
    }

    private static void DeserializeMeshFilter(GameObject obj, BinaryReader reader, uint dataLength)
    {
        ushort verticesCount = reader.ReadUInt16();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color32> color32s = new List<Color32>();

        for (int i = 0; i < verticesCount; i++)
        {
            vertices.Add(DeserializeVector3(reader));
        }
        for (int i = 0; i < verticesCount; i++)
        {
            normals.Add(DeserializeVector3(reader));
        }

        ushort colorCount = reader.ReadUInt16();
        for (int i = 0; i < colorCount; i++)
        {
            Color32 color = new Color32();
            color.r = reader.ReadByte();
            color.g = reader.ReadByte();
            color.b = reader.ReadByte();
            color.a = reader.ReadByte();
            color32s.Add(color);
        }


        ushort uvCount = reader.ReadUInt16();
        for (int i = 0; i < uvCount; i++)
        {
            uvs.Add(DeserializeVector2(reader));
        }

        uint trianglesCount = reader.ReadUInt32();
        for (int i = 0; i<trianglesCount; i++)
        {
            triangles.Add(reader.ReadUInt16());
        }

        Vector3 boundsCenter = DeserializeVector3(reader);
        Vector3 boundsSize = DeserializeVector3(reader);

        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors32 = color32s.ToArray();
        if(uvs.Count != 0)
        {
            mesh.uv = uvs.ToArray();
        }

        Bounds bounds = new Bounds();
        bounds.center = boundsCenter;
        bounds.size = boundsSize;

        mesh.bounds = bounds;
        //mesh.RecalculateTangents();
        meshFilter.mesh = mesh;

    }

    private static void DeserializeSkinnedMeshRenderer(GameObject obj,BinaryReader reader,Dictionary<string,string> dic, Dictionary<string, string>BoneParent,string tagName) {
        SkinnedMeshRenderer smr = obj.AddComponent<SkinnedMeshRenderer>();
      

        ushort verticesCount = reader.ReadUInt16();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color32> color32s = new List<Color32>();
        BoneWeight[] boneW;
        Matrix4x4[] bindPos;
        List<Transform> bones = new List<Transform>();
        Mesh mesh = new Mesh();
      
        

        for (int i = 0; i < verticesCount; i++)
        {
            vertices.Add(DeserializeVector3(reader));
        }
        for (int i = 0; i < verticesCount; i++)
        {
            normals.Add(DeserializeVector3(reader));
        }

        ushort colorCount = reader.ReadUInt16();
        for (int i = 0; i < colorCount; i++)
        {
            Color32 color = new Color32();
            color.r = reader.ReadByte();
            color.g = reader.ReadByte();
            color.b = reader.ReadByte();
            color.a = reader.ReadByte();
            color32s.Add(color);
        }

        ushort boneWeightCount = reader.ReadUInt16();
        boneW = new BoneWeight[boneWeightCount];
        for (int i = 0; i < boneWeightCount; i++)
        { 
            boneW[i] = new BoneWeight();
            boneW[i].boneIndex0 = reader.ReadUInt16();
            boneW[i].boneIndex1 = reader.ReadUInt16();
            boneW[i].boneIndex2 = reader.ReadUInt16();
            boneW[i].boneIndex3 = reader.ReadUInt16();
            boneW[i].weight0 = DeserializeFloat(reader);
            boneW[i].weight1 = DeserializeFloat(reader);
            boneW[i].weight2 = DeserializeFloat(reader);
            boneW[i].weight3 = DeserializeFloat(reader);

      //      Debug.Log("BoneWeright: " + boneW[i].boneIndex0 + " " + boneW[i].boneIndex1 + " " + boneW[i].boneIndex2 + " " + boneW[i].boneIndex3);
        }

        ushort bindPosCount = reader.ReadUInt16();
        bindPos = new Matrix4x4[bindPosCount];
        for (int i = 0; i < bindPosCount; i++)
        {
            Matrix4x4 n = new Matrix4x4();
            bindPos[i] = n;
            
            bindPos[i].m00 = DeserializeFloat(reader);
            bindPos[i].m01 = DeserializeFloat(reader);
            bindPos[i].m02 = DeserializeFloat(reader);
            bindPos[i].m03 = DeserializeFloat(reader);
            bindPos[i].m10 = DeserializeFloat(reader);
            bindPos[i].m11 = DeserializeFloat(reader);
            bindPos[i].m12 = DeserializeFloat(reader);
            bindPos[i].m13 = DeserializeFloat(reader);
            bindPos[i].m20 = DeserializeFloat(reader);
            bindPos[i].m21 = DeserializeFloat(reader);
            bindPos[i].m22 = DeserializeFloat(reader);
            bindPos[i].m23 = DeserializeFloat(reader);
            bindPos[i].m30 = DeserializeFloat(reader);
            bindPos[i].m31 = DeserializeFloat(reader);
            bindPos[i].m32 = DeserializeFloat(reader);
            bindPos[i].m33 = DeserializeFloat(reader);
           
        }

        ushort boneCount = reader.ReadUInt16();
        for (int i = 0; i < boneCount; i++)
        {
            string name = reader.ReadString();
            string parName = reader.ReadString();
           GameObject temp = new GameObject("temp");
            deserializeTransform(temp.transform,reader);
            GameObject go = null;
            GameObject par = null;
            GameObject[] tagObjs = GameObject.FindGameObjectsWithTag(tagName);

            for (int j=0;j<tagObjs.Length;j++) {
              GameObject cur=  tagObjs[j];
                if (cur.name.Equals(name)) {
                    go = cur;          
                }            
            }

            if (go==null) {
                go = new GameObject(name);
                go.tag = tagName;
                go.transform.position = temp.transform.position;
                go.transform.localEulerAngles = temp.transform.localEulerAngles;
                go.transform.localScale = temp.transform.localScale;
            }

            tagObjs = GameObject.FindGameObjectsWithTag(tagName);
            for (int j = 0; j < tagObjs.Length; j++)
            {
              GameObject cur= tagObjs[j];
                if (cur.name.Equals(parName)) {
                    par = cur;
                    go.transform.parent = par.transform;
                }
                if (j==tagObjs.Length-1&&!cur.name.Equals(parName)&&!BoneParent.ContainsKey(name)) {
                    BoneParent.Add(name,parName);           
                }
            }

       
            GameObject.Destroy(temp);
            bones.Add(go.transform);
                //    GameObject par = null;
                //    GameObject go = new GameObject(name);
                //    deserializeTransform(go.transform, reader);
                //GameObject[] tagObjs = GameObject.FindGameObjectsWithTag(tagName);

                //go.tag = tagName;
                //for (int j = 0; j < tagObjs.Length; j++)
                //{ 
                //    GameObject cur = tagObjs[j];
                //    if (cur.name.Equals(name))
                //    {
                //        if (cur.transform.childCount == 0)
                //        {
                //            GameObject.Destroy(cur);
                //        }
                //        else {
                //            go.name = "aa";
                //            break;
                //        }


                //    }

                //}
                //tagObjs = GameObject.FindGameObjectsWithTag(tagName);

                //for (int j = 0; j < tagObjs.Length; j++)
                //    {
                //        if (tagObjs[j].name.Equals(parName))
                //        {
                //            par = tagObjs[j];
                //            go.transform.parent = tagObjs[j].transform;
                //        }

                //    }
                //    if (par == null && !BoneParent.ContainsKey(name))
                //    {
                //        BoneParent.Add(name, parName);
                //    }


                //    bones.Add(go.transform);

            }

        ushort blendShapeCount = reader.ReadUInt16();
        ushort verticesLength = reader.ReadUInt16();
        for (int i=0;i<blendShapeCount;i++) {
            Vector3[] deltaVertices = new Vector3[verticesLength];
            Vector3[] deltaNormals = new Vector3[verticesLength];
            Vector3[] deltaTangents = new Vector3[verticesLength];

            string blendShapeName = reader.ReadString();
            deltaVertices = DeserialzeVector3Array(verticesLength,reader);
            deltaNormals = DeserialzeVector3Array(verticesLength, reader); 
            deltaTangents = DeserialzeVector3Array(verticesLength, reader);
            mesh.AddBlendShapeFrame(blendShapeName, 100, deltaVertices, deltaNormals, deltaTangents);
        }

        ushort uvCount = reader.ReadUInt16();
        for (int i = 0; i < uvCount; i++)
        {
            uvs.Add(DeserializeVector2(reader));
        }

        uint trianglesCount = reader.ReadUInt32();
        for (int i = 0; i < trianglesCount; i++)
        {
            triangles.Add(reader.ReadUInt16());
        }

        Vector3 boundsCenter = DeserializeVector3(reader);
        Vector3 boundsSize = DeserializeVector3(reader);


        
        
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors32 = color32s.ToArray();
        mesh.boneWeights = boneW;
        mesh.bindposes = bindPos;
        smr.bones = bones.ToArray();

        if (uvs.Count != 0)
        {
            mesh.uv = uvs.ToArray();
        }

        Bounds bounds = new Bounds();
        bounds.center = boundsCenter;
        bounds.size = boundsSize;

        mesh.bounds = bounds;
        smr.sharedMesh = mesh;
        
        string parentName =reader.ReadString();
        string rotBone = reader.ReadString();
        dic.Add(parentName,rotBone);
        mesh.name = reader.ReadString();

        Material mat = MaterialsHelper.Instance.VertexColor;
        smr.material = mat;
      
       
    }
    private static void DeserializeMeshRender(GameObject obj, BinaryReader reader, uint dataLength, Dictionary<int, Texture2D> texDic)
    {
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

        List<Material> matList = new List<Material>();
        byte matCount = reader.ReadByte();
        for (int i = 0; i < matCount; i++)
        {
            MaterialType matType = (MaterialType)reader.ReadByte();
            int baseTexId = reader.ReadInt32();
            int normalTexId = reader.ReadInt32();

            byte paramCount = reader.ReadByte();
            byte[] paramsArray = reader.ReadBytes(paramCount);

            Material mat = null;
            switch (matType)
            {
                case MaterialType.Lit:
                    mat = new Material(Shader.Find("HDRP/Lit"));

                    if (texDic.ContainsKey(baseTexId))
                    {
                        mat.SetTexture("_BaseColorMap", texDic[baseTexId]);
                    }

                    if (texDic.ContainsKey(normalTexId))
                    {
                        mat.SetTexture("_NormalMap", texDic[normalTexId]);
                    }
                    break;
                case MaterialType.VC:
                    mat = MaterialsHelper.Instance.VertexColor;
                    if (paramsArray[1] != 0 || paramsArray[2] != 0)
                    {
                        mat = MaterialsHelper.Instance.VertexColorInstance;
                    }
                    break;
                case MaterialType.Glass:
                    mat = MaterialsHelper.Instance.Glass;
                    if (paramsArray[1] != 0 || paramsArray[2] != 0)
                    {
                        mat = MaterialsHelper.Instance.GlassInstance;
                    }
                    break;
                case MaterialType.Metal:
                    mat = MaterialsHelper.Instance.Metal;
                    if (paramsArray[1] != 0 || paramsArray[2] != 0)
                    {
                        mat = MaterialsHelper.Instance.MetalInstance;
                    }
                    break;
                case MaterialType.Emission:
                    mat = MaterialsHelper.Instance.Emission;
                    if (paramsArray[1] != 0 || paramsArray[2] != 0)
                    {
                        mat = MaterialsHelper.Instance.EmissionInstance;
                    }
                    break;
                default:
                    break;
            }

            mat.SetFloat("_Intensity", ByteToFloat(paramsArray[0], 2.0f));
            mat.SetFloat("_Wind", ByteToFloat(paramsArray[1], 1.0f));
            mat.SetFloat("_Pulse", ByteToFloat(paramsArray[2], 1.0f));
            matList.Add(mat);
        }

        renderer.materials = matList.ToArray();
        
    }

    private static void DeserializeColliderHelper(GameObject obj, BinaryReader reader, uint dataLength)
    {
        ColliderType colliderType = (ColliderType)reader.ReadByte();
        Vector3 param1 = DeserializeVector3(reader);
        Vector3 param2 = DeserializeVector3(reader);

        ColliderHelper colliderHelper = obj.GetComponent<ColliderHelper>();
        if (colliderHelper == null)
        {
            colliderHelper = obj.AddComponent<ColliderHelper>();
        }

        colliderHelper.ColliderType = colliderType;
        colliderHelper.Param1 = param1;
        colliderHelper.Param2 = param2;

        colliderHelper.Apply(false);
    }

    private static void DeserializeAnimator(GameObject obj,BinaryReader reader) {
        Animator ani =  obj.AddComponent<Animator>();
       UnityEditor.Animations.AnimatorController ac = new UnityEditor.Animations.AnimatorController();
        ani.runtimeAnimatorController = ac;
        ani.avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/TriLib/TriLibSamples/AvatarLoader/Models/FinalRig.fbx");
        
       // avatar.name = GameObject.Find("AvatarController").transform.GetChild(0).transform.name;

        ac.AddLayer("DefaultLayer");
        ac.name = "NewAvatarController";
        int length = reader.ReadByte();
        List<string> states = new List<string>();
        List<string> clipPath = new List<string>();

        for (int i=0;i<length;i++) {
        states.Add(reader.ReadString());
        clipPath.Add(reader.ReadString());
        }
        
        var root = ac.layers[0].stateMachine;
        var rootState = root.AddState(states[0]);
       ac.AddParameter(states[0], UnityEngine.AnimatorControllerParameterType.Bool);
        rootState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath[0]);

        for (int i=1;i<length;i++) {
            var NewState = root.AddState(states[i]);
            NewState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath[i]);
            string newParameter = NewState.motion.name;

            ac.AddParameter(NewState.motion.name,UnityEngine.AnimatorControllerParameterType.Bool);
            root.AddAnyStateTransition(root.states[0].state);
            var transition = root.states[0].state.AddTransition(NewState);
            transition.AddCondition(AnimatorConditionMode.If, 1, newParameter);
            var backTransition = NewState.AddTransition(root.states[0].state);
            backTransition.AddCondition(AnimatorConditionMode.If, 1, root.states[0].state.name);

        }

    }

    private static Vector3 DeserializeVector3(BinaryReader reader)
    {
        Vector3 vect = Vector3.zero;
        byte[] xByte = reader.ReadBytes(2);
        byte[] yByte = reader.ReadBytes(2);
        byte[] zByte = reader.ReadBytes(2);
        

        vect.x = (float)Half.ToHalf(xByte, 0);
        vect.y = (float)Half.ToHalf(yByte, 0);
        vect.z = (float)Half.ToHalf(zByte, 0);

        return vect;
    }


    private static float DeserializeFloat(BinaryReader reader) {
        byte[] xByte = reader.ReadBytes(2);
        float x = (float)Half.ToHalf(xByte,0);
        return x;
    }
    
    private static Vector2 DeserializeVector2(BinaryReader reader)
    {
        Vector2 vect = Vector2.zero;
        byte[] xByte = reader.ReadBytes(2);
        byte[] yByte = reader.ReadBytes(2);


        vect.x = (float)Half.ToHalf(xByte, 0);
        vect.y = (float)Half.ToHalf(yByte, 0);

        return vect;
    }

   
    private static Vector3[] DeserialzeVector3Array(int length,BinaryReader reader) {
        Vector3[] array = new Vector3[length];
        for (int i=0;i<length;i++) {
            array[i] = DeserializeVector3(reader);
        }
        return array;
    }
    //root=GameObject.Find("AvatarController").transform.GetChild(0).gameObject.transform;
    public static void SerializeAvatarAndAnimator(Transform root, Stream outputStream, Texture2D icon) {
        using (GZipStream zipStream = new GZipStream(outputStream, CompressionMode.Compress))
        {
            using (BinaryWriter biWriter = new BinaryWriter(zipStream, Encoding.UTF8))
            {
               
                if (root.childCount == 0)
                {
                    return;
                }

                biWriter.Write((byte)ModelType.AvatarModel);
                biWriter.Write((byte)0);
                biWriter.Write((byte)0);

                biWriter.Write(root.name);
                serializeTransform(root,biWriter);
                SerializeAnimator(root,biWriter);
                biWriter.Write((ushort)root.childCount);
                Debug.Log("childcout"+root.childCount);
                for (int i=0;i<root.childCount;i++) {
                    Transform parent  = root.GetChild(i);
                    biWriter.Write(parent.name);
                    serializeTransform(parent,biWriter);
                    SkinnedMeshRenderer smr = parent.GetComponent<SkinnedMeshRenderer>();
                    Animator animator = parent.GetComponent<Animator>();

                    if (smr != null)
                    {
                        biWriter.Write((byte)1);
                        SerializeSkinnedMeshRender(parent, biWriter);
                    }
                    else { 
                    biWriter.Write((byte)0);
                    }
                }


                byte[] texture = icon.EncodeToJPG(60);
                biWriter.Write((uint)texture.Length);
                biWriter.Write(texture);

                Color32[] color32s =icon.GetPixels32();
                for (int i = 0; i < color32s.Length; i++)
                {
                    color32s[i].r = color32s[i].a;
                    color32s[i].g = color32s[i].a;
                    color32s[i].b = color32s[i].a;
                }
                Texture2D alphaChannel = new Texture2D(icon.width, icon.height);
                alphaChannel.SetPixels32(color32s);
                alphaChannel.Apply();
                byte[] alphaChannelData = alphaChannel.EncodeToJPG(95);
                biWriter.Write((uint)alphaChannelData.Length);
                biWriter.Write(alphaChannelData);


            }
        }
     }

   static void serializeTransform(Transform objTransform,BinaryWriter biWriter) {
        SerializeVector3(objTransform.localPosition, biWriter);
        SerializeVector3(objTransform.localEulerAngles,biWriter);
        SerializeVector3(objTransform.localScale, biWriter);
    }

   

    static void deserializeTransform(Transform objTransform,BinaryReader reader) {
        objTransform.position = DeserializeVector3(reader);
        objTransform.localEulerAngles = DeserializeVector3(reader);
        objTransform.localScale = DeserializeVector3(reader);
    }


    public static Texture2D DeserializeAllModel(Transform root, Stream inputStream)
    {
        Texture2D icon = null;
        using (GZipStream zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            using (BinaryReader reader = new BinaryReader(zipStream, Encoding.UTF8))
            {
                byte modelType = reader.ReadByte();
                switch (modelType)
                {
                    case 0:
                        Debug.Log("StaticModelReaded");

                        icon = ReadZipStream(root.gameObject,zipStream);
                        break;
                    case 1:
                        Debug.Log("AvtarModelReaded");

                        icon = ReadAnimateZipStream(root,zipStream);
                        break;
                }

            }

        }
        return icon;

    }
    public static Texture2D DeserializeAnimate(Transform root, Stream inputStream)
    {
        Texture2D icon = null;
        using (GZipStream zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            icon = ReadAnimateZipStream(root, zipStream);
        }
        
        return icon;
    }
    // root = GameObject.Find("AvatarController").transform;
    static Texture2D ReadAnimateZipStream(Transform root, GZipStream zipStream)
    {
        Texture2D icon = null;
        
        string objName = "";
       
        Dictionary<string, string> dic = new Dictionary<string, string>();
        Dictionary<string, string> BoneParent = new Dictionary<string, string>();

        using (BinaryReader reader = new BinaryReader(zipStream, Encoding.UTF8))
        {

            /// Ô¤Áô²ÎÊý
            byte param2 = reader.ReadByte();
            byte param3 = reader.ReadByte();

            GameObject p = new GameObject(reader.ReadString());
            objName = p.name;
            p.transform.parent = GameObject.Find("AvatarController").transform;
            InternalEditorUtility.AddTag(objName);
            p.tag = objName;
            deserializeTransform(p.transform,reader);
            DeserializeAnimator(p,reader);
            int chidCount = reader.ReadUInt16();

            for (int i=0;i<chidCount;i++) {
                GameObject b = new GameObject(reader.ReadString());
                GameObject[] tagOBj = GameObject.FindGameObjectsWithTag(objName);
                for (int j = 0; j < tagOBj.Length; j++)
                {
                    GameObject cur = tagOBj[j];
                    if (cur.name.Equals(b.name))
                    {
                        if (cur.transform.childCount != 0)
                        {
                            b.name = "aa";
                        }

                    }
                }
                b.tag = objName;
                deserializeTransform(b.transform,reader);
                b.transform.parent = p.transform;
                
                byte boolSmr = reader.ReadByte();
                if (boolSmr==1) {
                    DeserializeSkinnedMeshRenderer(b,reader,dic, BoneParent,objName);
                }

                if (GameObject.Find("aa")!=null) {
                    GameObject.Destroy(GameObject.Find("aa"));
                }
                
            }

        //    int id = reader.ReadInt32();
            uint length = reader.ReadUInt32();
            byte[] data = reader.ReadBytes((int)length);

            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(data);
       
                uint alphaLength = reader.ReadUInt32();
                byte[] alphaData = reader.ReadBytes((int)alphaLength);
                Texture2D alphaTex = new Texture2D(1, 1);
                alphaTex.LoadImage(alphaData);

                Color32[] colors = tex.GetPixels32();
                Color32[] alphas = alphaTex.GetPixels32();
                for (int j = 0; j < colors.Length; j++)
                {
                    colors[j] = new Color32(colors[j].r, colors[j].g, colors[j].b, alphas[j].r);
                }

                tex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
                tex.SetPixels32(colors);
                tex.Apply();

            icon = tex;

            GameObject[] TagObjs = GameObject.FindGameObjectsWithTag(objName);
            
            foreach (string child in BoneParent.Keys) {
                GameObject cur = null;
                GameObject par = null;
                for (int i=0;i<TagObjs.Length;i++) {
                    if (TagObjs[i].name.Equals(child)) { 
                    cur = TagObjs[i];
                    }
                    if (TagObjs[i].name.Equals(BoneParent[child])) {
                        par = TagObjs[i];
                    }
                    if (cur!=null&&par!=null) {
                        cur.transform.parent = par.transform;
                    }
                }
         
            }
            TagObjs = GameObject.FindGameObjectsWithTag(objName);
           
            foreach (string a in dic.Keys)
            {
                GameObject smrObj = null;
                GameObject rootBoneObj = null;

                for (int i = 0; i < TagObjs.Length; i++)
                {
                    if (TagObjs[i].name.Equals(a))
                    {
                        smrObj = TagObjs[i];
                    }
                    if (TagObjs[i].name.Equals(dic[a]))
                    {
                        rootBoneObj = TagObjs[i];
                    }
                    if (smrObj != null && rootBoneObj != null)
                    {
                        smrObj.GetComponent<SkinnedMeshRenderer>().rootBone = rootBoneObj.transform;
                    }
                }

            }

            //    root.RotateAround(root.transform.position, Vector3.up, 180);
            GameObject.Find("AvatarController").GetComponent<AvatarController>().Animator = GameObject.Find(objName).GetComponent<Animator>();
            GameObject.Find("AvatarController").GetComponent<AvatarController>().InnerAvatar = GameObject.Find(objName);
            GameObject.Find("AvatarController").GetComponent<AnimatorGenerater>().RefreshAnimatorController();
            GameObject.Destroy(GameObject.Find("AvatarController").transform.GetChild(0).gameObject);
            
           // GameObject.Find("aaa").GetComponent<RawImage>().texture = tex;

        }


        return icon;

    }
}
