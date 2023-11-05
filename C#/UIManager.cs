/*
 * Document:#UIManager.cs#
 * Author: Yuyang Qiu
 * Function:A single-instance script for the current mouse
 * interaction with objects and its correlation with UI functions.
 * 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TriLibCore;
using TriLibCore.SFB;
using UnityEngine;
using UnityEngine.UI;
using Battlehub.RTCommon;
using Unity.VisualScripting;
using Battlehub.RTHandles;
using UnityEditor.Animations;
using static System.Net.Mime.MediaTypeNames;
using UnityEditor;
using TriLibCore.Samples;
using System.Runtime.InteropServices.ComTypes;

public class UIManager : MonoBehaviour, ILoadFile
{
    private static UIManager m_instance = null;
    public static UIManager Instance
    {
        get
        {
            return m_instance;
        }
    }
    
    public GameObject Root;

    GameObject AvatarRoot;
    
    public Transform Target;

    public Transform Sun;

    public Slider SunSlider;

    public Toggle AutoBake;

    
    public Camera PhotoCamera;

   public Transform lastObj;

    modelInformText mt;

    
    [HideInInspector]
    public Transform PickObj = null;

    AssetLoaderOptions AssetLoaderOptions;


  
    // Start is called before the first frame update
    void Start()
    {
        m_instance = this;
        AssetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
        AssetLoaderOptions.Timeout = 180;
        AssetLoaderOptions.ImportCameras = false;
        AssetLoaderOptions.ImportLights = false;

        AvatarRoot = GameObject.Find("AvatarController");

       
    }

    // Update is called once per frame
    void Update()
    {
        MousePick();

        if (Input.GetKeyDown(KeyCode.Delete)) {
            if (PickObj.parent != null&&PickObj.parent.name!= "Root")
            {
                Destroy(PickObj.parent.gameObject);
            }
            else { 
                Destroy(PickObj.gameObject);
            }
        }

    }
   
    public void OnStaticModelBtnClick()
    {
        var filePickerAssetLoader = AssetLoaderFilePicker.Create();
        filePickerAssetLoader.LoadModelFromFilePickerAsync("Select a File", OnLoad, OnMaterialsLoad, OnProgress, OnBeginLoadModel, OnError, null, AssetLoaderOptions);
    }

  

    public void OnLoad(AssetLoaderContext assetLoaderContext)
    {
        if (assetLoaderContext.RootGameObject == null)
        {
            return;
        }
        MeshRenderer mr = assetLoaderContext.RootGameObject.transform.GetComponentInChildren<MeshRenderer>();
        SkinnedMeshRenderer smr = assetLoaderContext.RootGameObject.transform.GetComponentInChildren<SkinnedMeshRenderer>();

        if (mr!=null) {
            assetLoaderContext.RootGameObject.transform.parent = this.Root.transform;

            GameObject.Find("AnimationPanel").GetComponent<PanelAnimation>().inAnimaiton();
            GameObject.Find("RightPanel").GetComponent<PanelAnimation>().outAnimation();
        }
        if (smr!=null) {
            
            assetLoaderContext.RootGameObject.transform.parent = AvatarRoot.transform;
            Animator ani= assetLoaderContext.RootGameObject.AddComponent<Animator>();
            ani.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/TriLib/TriLibSamples/AvatarLoader/AnimatorControllers/Mannequin.controller") as AnimatorController;
            ani.avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/TriLib/TriLibSamples/AvatarLoader/Models/FinalRig.fbx");

            GameObject.Find("AvatarController").GetComponent<AvatarController>().Animator = assetLoaderContext.RootGameObject.GetComponent<Animator>();
           GameObject.Find("AvatarController").GetComponent<AvatarController>().InnerAvatar = assetLoaderContext.RootGameObject;

            assetLoaderContext.RootGameObject.transform.RotateAround(assetLoaderContext.RootGameObject.transform.position,Vector3.up,180f);
           // GameObject.Find("RightPanel").GetComponent<PanelAnimation>().inAnimaiton();
           // GameObject.Find("AnimationPanel").GetComponent<PanelAnimation>().outAnimation() ;

            Destroy(AvatarRoot.transform.GetChild(0).gameObject);

        }
        assetLoaderContext.RootGameObject.transform.localPosition = Vector3.zero;
        
        Target.position = GetCenter(Root.transform);

        CreateCollider(assetLoaderContext.RootGameObject.transform);
        PickObj = assetLoaderContext.RootGameObject.transform;



    }



    public void OnMaterialsLoad(AssetLoaderContext assetLoaderContext)
    {
        Debug.Log("MaterialsLoaded");

        if (AutoBake.isOn)
        {
            GeometryPanel.BakeVerticesColor(assetLoaderContext.RootGameObject.transform);
        }

        UpdatePhotoCamera();

    }

    public void OnProgress(AssetLoaderContext assetLoaderContext, float value)
    {

    }

    public void OnBeginLoadModel(bool hasFiles)
    {

    }

    public void OnError(IContextualizedError contextualizedError)
    {

    }

    void CreateCollider(Transform root)
    {
        MeshFilter meshFilter = root.GetComponent<MeshFilter>();
        ColliderHelper colliderHelper = root.GetComponent<ColliderHelper>();
        if (meshFilter != null && colliderHelper == null)
        {
            colliderHelper = root.gameObject.AddComponent<ColliderHelper>();
            colliderHelper.Apply(false);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform obj = root.GetChild(i);
            CreateCollider(obj);
        }

    }

    Vector3 GetCenter(Transform root)
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < root.childCount; i++)
        {
            Vector3 childCenter = GetCenter(root.GetChild(i));
            if (childCenter != Vector3.zero)
            {
                if (center == Vector3.zero)
                {
                    center = childCenter;
                }
                else
                {
                    center += childCenter;
                    center /= 2.0f;
                }
            }
        }

        MeshFilter mesh = root.GetComponent<MeshFilter>();
        if (mesh)
        {
            return mesh.mesh.bounds.center;
        }

        return center;
    }

    /// <summary>
    /// Serialize current model as a binary file.
    /// </summary>
    public void OnExportYIMTBtnClick()
    {
       
     

        string name = "model";
        if (Root.transform.childCount > 0)
        {
            name = Root.transform.GetChild(0).name;
        }
        if (!AvatarRoot.transform.GetChild(0).name.Equals("FinalRig")) {
            name = AvatarRoot.transform.GetChild(0).name;
        }
        StandaloneFileBrowser.SaveFilePanelAsync("Save yq file", null, name, "yimt", OnSaveYIMT);

    }


    /// <summary>
    /// Open a saved binary file and deserialize it to gameobject in unity.
    /// </summary>
    public void OnOpenYIMTBtnClick()
    {
        StandaloneFileBrowser.OpenFilePanelAsync("open a yq file", null, "yimt", false, OnOpenYIMT);
        
    }

    int m_photoSize = 128;
    
    void OnSaveYIMT(ItemWithStream item)
    {
        UpdatePhotoCamera();
     RenderTexture renTex = new RenderTexture(m_photoSize, m_photoSize, 16);

        PhotoCamera.targetTexture = renTex;
        PhotoCamera.Render();
        RenderTexture.active = renTex;

        //¶ÁÈ¡ÏñËØ
        Texture2D tex = new Texture2D(m_photoSize, m_photoSize);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

    //    GameObject.Find("aaa").GetComponent<RawImage>().texture = tex;

        tex.Apply();

       

        PhotoCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renTex);

      
        //for(int i = 0; i < tex.width; i++)
        //{
        //    for(int j = 0; j<tex.height;j++)
        //    {
        //        if(tex.GetPixel(i,j).a == 0)
        //        {
        //            tex.SetPixel(i, j, Color.black);
        //        }
        //    }
        //}


        using (var stream = File.Open(item.Name, FileMode.Create))
        {

            if (Root.transform.childCount == 0&& AvatarRoot.transform.GetChild(0).gameObject.name.Equals("FinalRig")) {
                return;
            }

            if (AvatarRoot.transform.childCount==1&&!AvatarRoot.transform.GetChild(0).name.Equals("FinalRig")) {
                ModelSerializer.SerializeAvatarAndAnimator(AvatarRoot.transform.GetChild(0), stream, tex);
            }

            if (Root.transform.childCount!=0) {
                ModelSerializer.Serialize(Root.transform, stream, tex);
            }
        }


        Debug.Log("Save file" + item.Name);
    }

    void OnOpenYIMT(IList<ItemWithStream> itemList)
    {
        if (itemList.Count == 0)
        {
            return;
        }



        if (!File.Exists(itemList[0].Name))
        {
            return;
        }


        using (var stream = File.Open(itemList[0].Name, FileMode.Open))
        {
            //ModelSerializer.Deserialize(Root, stream);
            ModelSerializer.DeserializeAllModel(Root.transform,stream);
        }

        Target.position = GetCenter(Root.transform);

        Debug.Log("Reading file" + itemList[0].Name);
    }

   

    public void OnSunRotateChange()
    {
        float value = SunSlider.value;

        float angle = Mathf.Lerp(-20, 200, value);

     //   Debug.Log(angle);
        Sun.localRotation = Quaternion.AngleAxis(angle, Vector3.right);
    }


    void MousePick()
    {

        if (Input.GetMouseButtonUp(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.name == "Ground")
                {
                 
                    return;
                }

                PickObj = hit.transform;
                //  Destroy(lastObj.GetComponent<ExposeToEditor>());

                if (PickObj.GetComponent<ExposeToEditor>() == null&&PickObj.GetComponent<MeshEditor>()==null)
                {
                    PickObj.AddComponent<ExposeToEditor>();
                    PickObj.GetComponent<ExposeToEditor>().ShowSelectionGizmo = true;
                  
                }

                if (PickObj.name!= "VertexEditBall(Clone)") {
                    lastObj = PickObj;
                }

                Debug.Log("Pick:" + hit.transform.name);
            }

        }
    }
    
    /// <summary>
    /// Take a picture shot of target camera.
    /// </summary>
    void UpdatePhotoCamera()
    {
        Bounds bounds = GetBounds();
        float distance = bounds.size.x * 0.866f * 2.2f;
        float a = 0f;
        if (AvatarRoot.transform.childCount == 1 && !AvatarRoot.transform.GetChild(0).name.Equals("FinalRig"))
        {
            distance *= 1.7f;
            a = 0.8f;
        }
        distance = MathF.Max(distance, (bounds.size.z / 1.732f + bounds.size.y) * 1.8f);
        Vector3 pos = bounds.center + new Vector3(0, distance * 0.5f+a, -distance * 0.866f);
      

        PhotoCamera.transform.position = pos;
    }

    /// <summary>
    /// get object bounds box.
    /// </summary>
    /// <returns></returns>
    Bounds GetBounds()
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        MeshFilter[] meshFilters = Root.GetComponentsInChildren<MeshFilter>();

        if (meshFilters.Length == 0)
        {
            return bounds;
        }

        if (meshFilters.Length == 1)
        {
            return meshFilters[0].mesh.bounds;
        }

        Mesh mesh = meshFilters[0].mesh;

        Vector3 min = mesh.bounds.min;
        Vector3 max = mesh.bounds.max;

        for (int i = 1; i < meshFilters.Length; i++)
        {
            min = Vector3.Min(min, meshFilters[i].mesh.bounds.min);
            max = Vector3.Max(max, meshFilters[i].mesh.bounds.max);
        }

        bounds.max = max;
        bounds.min = min;

        return bounds;
    }

   
    public void DeleteTriangle() {
      
            Debug.Log("last :"+ lastObj.name);
            if (lastObj!=null&& lastObj.GetComponent<MeshEditor>() != null)
            {
            lastObj.GetComponent<MeshEditor>().IsTriangleDeleted = true;
            }
      
    }

    public void RecoverModel() {
      //  lastObj.GetComponent<MeshEditor>().isRecovered = true;
        PickObj.GetComponent<MeshEditor>().isRecovered = true;
    }

    public void separateModel() {
        PickObj.GetComponent<MeshEditor>().isSeparate = true;
    }

    public void OnOpenYimtAnimator()
    {
        StandaloneFileBrowser.OpenFilePanelAsync("", null, "yimt", false, onOpenYimtAnimator);

    }

    public void OnExportYimtAnimate()
    {
        GameObject ModelRoot = GameObject.Find("AvatarController");
        if (ModelRoot.transform.childCount == 0)
        {
            
            return;
        }

        string name = "model";
        if (ModelRoot.transform.childCount > 0)
        {
            name = ModelRoot.transform.GetChild(0).name;
        }

        StandaloneFileBrowser.SaveFilePanelAsync("±£´æYIMTÎÄ¼þ", null, name, "yimt", OnSaveAnimator);

    }

    void onOpenYimtAnimator(IList<ItemWithStream> itemList)
    {
        Transform aniRoot = GameObject.Find("AvatarController").transform;
        if (itemList.Count == 0)
        {
            return;
        }



        if (!File.Exists(itemList[0].Name))
        {
            return;
        }


        using (var stream = File.Open(itemList[0].Name, FileMode.Open))
        {
            ModelSerializer.DeserializeAnimate(aniRoot, stream);
        }

        Target.position = GetCenter(aniRoot.transform);

        Debug.Log("¶ÁÈ¡ÎÄ¼þ" + itemList[0].Name);
    }
    void OnSaveAnimator(ItemWithStream item) {
        UpdatePhotoCamera();

        RenderTexture renTex = new RenderTexture(m_photoSize, m_photoSize, 16);
        PhotoCamera.targetTexture = renTex;
        PhotoCamera.Render();
        RenderTexture.active = renTex;

       
        Texture2D tex = new Texture2D(m_photoSize, m_photoSize);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();

        

        PhotoCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renTex);
        GameObject targetModel = GameObject.Find("AvatarController").transform.GetChild(0).gameObject;
        using (var stream = File.Open(item.Name, FileMode.Create))
        {
            ModelSerializer.SerializeAvatarAndAnimator(targetModel.transform, stream, tex);
        }   
    }

    public void EnableAvararContronller() {
        if (!GameObject.Find("AvatarController").GetComponent<AvatarController>().enabled)
        {
            GameObject.Find("AvatarController").GetComponent<AvatarController>().enabled = true;
            GameObject.Find("AvatarController").GetComponent<AnimatorGenerater>().enabled = true;
            GameObject.Find("AvatarController").GetComponent<CharacterController>().enabled = true;
           
            
        }
        else {
            GameObject.Find("AvatarController").GetComponent<AvatarController>().enabled = false;
            GameObject.Find("AvatarController").GetComponent<AnimatorGenerater>().enabled = false;
            GameObject.Find("AvatarController").GetComponent<CharacterController>().enabled = false;
           
        }


    }


}
