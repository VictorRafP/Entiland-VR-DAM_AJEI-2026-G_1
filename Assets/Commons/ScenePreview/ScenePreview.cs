using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePreview : MonoBehaviour
{
    [SerializeField] private string sceneName;


    public void OnPull()
    {
        Debug.Log("OnPull");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void OnStartTarget()
    {
        Debug.Log("OnStartTarget");
    }

    public void OnStopTarget()
    {
        Debug.Log("OnStopTarget");
    }

    public void OnStartSelect()
    {
        Debug.Log("OnStartSelect");
    }

    public void OnStopSelect()
    {
        Debug.Log("OnStopSelect");
    }
}
