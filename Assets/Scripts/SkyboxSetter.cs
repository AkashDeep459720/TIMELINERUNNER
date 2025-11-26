using UnityEngine;

public class SkyboxSetter : MonoBehaviour
{
    [SerializeField] private Material skyboxMaterial;

    void Start()
    {
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
            DynamicGI.UpdateEnvironment(); // Optional: updates lighting
        }
    }
}
