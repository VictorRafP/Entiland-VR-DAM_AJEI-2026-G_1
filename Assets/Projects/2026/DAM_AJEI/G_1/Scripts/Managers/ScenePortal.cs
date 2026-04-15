using UnityEngine;
using UnityEngine.SceneManagement;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    public class ScenePortal : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            SceneManager.LoadScene("G_1");
        }
    }
}