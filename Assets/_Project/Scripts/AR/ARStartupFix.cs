using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARStartupFix : MonoBehaviour
{
    private ARSession session;

    void Start()
    {
        session = FindObjectOfType<ARSession>();
        StartCoroutine(RestartAR());
    }

    IEnumerator RestartAR()
    {
        yield return new WaitForSeconds(0.5f);

        if (session != null)
        {
            session.Reset();
            Debug.Log("AR Session Reset → FIX APPLIED");
        }
    }
}