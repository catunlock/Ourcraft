using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RandomWords : MonoBehaviour
{
    public AnimationClip[] animations;
    public AudioClip[] audios;
    public AudioClip[] specialAudios;
    public AudioClip[] firstGreetingAudios;
    public AudioClip[] greetingAudios;
    public AudioClip[] goodbyeAudios;
    public AudioClip deathAudio;
    AudioSource audioSource;
    Animator anim;
    public float delayWeight;
    public float maxSpeakTimer;
    public float specialAudioChance;
    float current = 0;
    float speakTimer;
    bool firstGreeting;


    void Awake()
    {
        firstGreeting = true;
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        speakTimer = maxSpeakTimer;
    }

    // Update is called once per frame
    void Update()
    {
        if (audioSource.isPlaying)
            speakTimer -= Time.deltaTime;

        if (speakTimer <= 0)
        {
            speakTimer = maxSpeakTimer;
            ChangeFace(animations[Random.Range(0, animations.Length)].name);
        }
        else
            current = Mathf.Lerp(current, 0, delayWeight);
         
        anim.SetLayerWeight(1, current);
    }

    public void RestoreDefault()
    {
        anim.SetLayerWeight(1, 0);
    }

    public void PlayRandomSalutationAudio()
    {
        int index;

        if (audioSource.isPlaying)
            audioSource.Stop();

        if (firstGreeting)
        {
            firstGreeting = false;
            index = Random.Range(0, firstGreetingAudios.Length);
            audioSource.PlayOneShot(firstGreetingAudios[index]);
            // play one from greetings
        }
        else
        {
            index = Random.Range(0, greetingAudios.Length);
            audioSource.PlayOneShot(greetingAudios[index]);
            // play one from first greetings audios
        }
    }

    public void PlayRandomTalkAudio()
    {
        int index;

        if (audioSource.isPlaying)
            audioSource.Stop();

        if (Random.value > specialAudioChance)
        {
            index = Random.Range(0, audios.Length);
            audioSource.PlayOneShot(audios[index]);
            // play one from greetings
        }
        else
        {
            index = Random.Range(0, specialAudios.Length);
            audioSource.PlayOneShot(specialAudios[index]);
            // play one from first greetings audios
        }
    }

    public void PlayRandomFarewellAudio()
    {
        int index = Random.Range(0, goodbyeAudios.Length);

        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.PlayOneShot(goodbyeAudios[index]);
        // plays a random "good bye" audio
    }

    public void PlayDeathAudio()
    {
        audioSource.PlayOneShot(deathAudio);
    }

    void ChangeFace(string str)
    {
        current = 1;
        anim.CrossFade(str, 0.1f);
    }
}
