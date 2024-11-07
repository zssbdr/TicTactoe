using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public AudioClip[] ses;
    [SerializeField] private AudioSource audioSource;

    public enum AUDIO_TYPE {
        Circle = 0,
        Cross1 = 1,
        Cross2 = 2,
    }

    //private static GameAudioManager instance;
    //public static GameAudioManager Instance { get { return instance; } }

    //private void Awake()
    //{
    //    if (instance != null)
    //    {
    //        Destroy(gameObject);
    //        return;
    //    }

    //    instance = this;
    //}

    // Start is called before the first frame update
    private void Start()
    {
        UnityEngine.Debug.Log("GameAudio Active");
        audioSource = GetComponent<AudioSource>();
    }

    public void InitManager()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySE(AUDIO_TYPE audioType)
    {
        audioSource.clip = ses[(int)audioType];
        audioSource.Play();
    }


    // Update is called once per frame
    private void Update()
    {
        
    }
}