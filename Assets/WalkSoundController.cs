using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkSoundController : MonoBehaviour
{
    // Start is called before the first frame update
    public List<AudioClip> walkSounds;
    public AudioSource walkSource;

    int currentSound = 0;
    void Start()
    {
        InvokeRepeating("PlaySound", 0.0f, 0.5f);
    }

    void PlaySound()
    {
        if (Input.GetButton("Vertical") || Input.GetButton("Horizontal"))
        {
            walkSource.PlayOneShot(walkSounds[Random.Range(0,walkSounds.Count)]);
            if (currentSound >= walkSounds.Count)
            {
                currentSound = 0;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
