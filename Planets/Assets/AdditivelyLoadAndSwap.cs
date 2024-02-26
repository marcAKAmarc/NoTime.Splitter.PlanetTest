using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class LoadSwapInput
{
    public Transform Replacable;
    public string NewTransformName;
}
[Serializable]
public class LoadSwapData
{
    public Transform Replacable;
    public Transform ReplacedBy;
}

public class AdditivelyLoadAndSwap : MonoBehaviour
{
    public string SceneName;
    public List<LoadSwapInput> LoadSwapInput;
    private List<LoadSwapData> LoadSwapDatas;

    private Transform newt;

    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive).completed += OnSceneLoaded;
    }

    private void OnSceneLoaded(AsyncOperation op)
    {
        var modelScene = SceneManager.GetSceneByName(SceneName);
        LoadSwapDatas = new List<LoadSwapData>();
        foreach (var input in LoadSwapInput)
        {
            var root = modelScene.GetRootGameObjects()[0].transform;

            Transform found = null;
            if (root.name == input.NewTransformName)
                found = root;
            else
                found.Find(input.NewTransformName);

            if (found != null)
            {
                LoadSwapDatas.Add(new LoadSwapData { Replacable = input.Replacable, ReplacedBy = found.transform });

                found.gameObject.isStatic = false;
                foreach (var subFound in found.gameObject.GetComponentsInChildren<Transform>())
                {
                    subFound.gameObject.isStatic = false;
                }
                foreach (var collider in found.gameObject.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }

                input.Replacable.gameObject.SetActive(false);


            }
            else
            {
                Debug.Log("Failed to find transform \"" + input.NewTransformName + "\" in Scene \"" + SceneName + "\".  Root[0] name is '" + root.transform.name + "'.");
            }

        }
    }

    // Update is called once per frame
    void Update()
    {
        if (LoadSwapDatas != null)
        {
            foreach (var data in LoadSwapDatas)
            {
                data.ReplacedBy.position = data.Replacable.position;
                data.ReplacedBy.localScale = data.Replacable.lossyScale;
                data.ReplacedBy.rotation = data.Replacable.rotation;
            }
        }
    }
}
