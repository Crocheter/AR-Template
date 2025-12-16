using System.Collections;
using UnityEngine;
using Vuforia;

public class DelayedPermissionAndVuforia : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.2f); // wait for Unity to finish init

        #if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
        #endif

        yield return new WaitForSeconds(0.1f); // small extra delay

        VuforiaBehaviour.Instance.enabled = true; // start Vuforia
    }
}
