using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextButton : MonoBehaviour
{
    [SerializeField]
    GameObject tutorial;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Next()
    {
        if (tutorial.activeSelf)
            SceneManager.LoadScene(1);
        else
            tutorial.SetActive(true);
    }
}
