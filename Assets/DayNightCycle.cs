using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    public float timeOfDay = 0;
    public float timeSpeed = 2000;
    public Material[] dayMaterial;
    public Material[] dawnSetMaterial;
    public Material[] nightMaterial;
    public Text hourText;
    public Light mainLight;
    public GameObject Horizen;
    
    private Renderer horizenRenderer;
    const int secondsInDay = 86400;
    private bool nightTime;

    // Start is called before the first frame update
    void Start()
    {
        horizenRenderer = Horizen.GetComponent<Renderer>();
        nightTime = false;
    }

    // Update is called once per frame
    void Update()
    {
        timeOfDay = (Time.time * timeSpeed) % secondsInDay;

        if (!nightTime && timeOfDay >= secondsInDay / 2)
        {
            nightTime = true;
            GameObject.FindObjectOfType<MeshGenerator>().SpawnZombies();
        }
        if (nightTime && timeOfDay < secondsInDay / 2)
        {
            nightTime = false;
            GameObject.FindObjectOfType<MeshGenerator>().DespawnZombies();
        }
        RotateLight();
        ChangeSkyColor();
        UpdateTime();
    }

    void UpdateTime()
    {
        System.TimeSpan time = System.TimeSpan.FromSeconds(timeOfDay + secondsInDay / 3);
        hourText.text = string.Format("{0:D2}:{1:D2}", time.Hours, time.Minutes);
    }

    void RotateLight()
    {
        float x = timeOfDay / secondsInDay * 360;
        mainLight.transform.rotation = Quaternion.Euler(x, 0, 0);
    }

    void ChangeSkyColor()
    {
        if (timeOfDay >= secondsInDay * 23 / 24)
        {
            Material[] mats = GetMaterialIndicesTransition(timeOfDay, secondsInDay * 23 / 24, secondsInDay, dawnSetMaterial, dayMaterial);
            horizenRenderer.materials = mats;
        }

        else if (timeOfDay >= secondsInDay * 22 / 24)
        {
            Material[] mats = GetMaterialIndicesTransition(timeOfDay, secondsInDay * 22 / 24, secondsInDay * 23 / 24, nightMaterial, dawnSetMaterial);
            horizenRenderer.materials = mats;
        }

        else if (timeOfDay >= secondsInDay * 14 / 24)
        {
            Material[] mats = new Material[2];
            mats[0] = nightMaterial[0];
            mats[1] = nightMaterial[0];
            horizenRenderer.materials = mats;
        }

        else if (timeOfDay >= secondsInDay * 13 / 24)
        {
            Material[] mats = GetMaterialIndicesTransition(timeOfDay, secondsInDay * 13 / 24, secondsInDay * 14 / 24, dawnSetMaterial, nightMaterial);
            horizenRenderer.materials = mats;
        }

        else if (timeOfDay >= secondsInDay * 12 / 24)
        {
            Material[] mats = GetMaterialIndicesTransition(timeOfDay, secondsInDay * 12 / 24, secondsInDay * 13 / 24, dayMaterial, dawnSetMaterial);
            horizenRenderer.materials = mats;
        }

        else
        {
            Material[] mats = new Material[2];
            mats[0] = dayMaterial[0];
            mats[1] = dayMaterial[0];
            horizenRenderer.materials = mats;
        }
    }

    private Material[] GetMaterialIndicesTransition(float time, int start, int end, Material[] firstMat, Material[] secondMat)
    {
        int i = ((int)time - start) / ((end - start) / 3);
        Material[] mats = new Material[2];
        mats[0] = firstMat[i];
        mats[1] = secondMat[2 - i];
        return mats;
    }
}
