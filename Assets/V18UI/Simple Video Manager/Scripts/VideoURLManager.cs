using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

public class VideoURLManager : MonoBehaviour
{
    [SerializeField] VideoPlayer m_VideoPlayer;
    [SerializeField] List<string> m_VideoURLS;

    // IMAGE
    [SerializeField] RawImage m_RawImage;
    [SerializeField] RenderTexture m_RenderTexture;

    [SerializeField] Slider m_CurrentTimerSlider;
    [SerializeField] Slider m_InteractiveSlider;

    [SerializeField] Slider m_VolumeSlider;

    [SerializeField] Image m_PlayPause_IMG;

    [SerializeField] Text m_CurrentTime;
    [SerializeField] Text m_TotalTime;

    [SerializeField] Sprite m_PlaySprite;
    [SerializeField] Sprite m_PauseSprite;

    // MUTE //
    [SerializeField] Image m_SoundImage;
    [SerializeField] Sprite m_SoundOn;
    [SerializeField] Sprite m_SoundOff;

    // SHUFFLE //
    [SerializeField] Image m_ShuffleImage;
    [SerializeField] Color32 m_Shuffle_Enabled;
    [SerializeField] Color32 m_Shuffle_Disabled;

    // SPINNER
    [SerializeField] GameObject m_SpinnerObj;

    // Autoplay //
    /// <summary>
    /// Play the next video
    /// </summary>
    [SerializeField] bool m_AutoPlay = false;
    [SerializeField] bool m_LandscapeMode = false;
    private bool m_Loop = false;
    [SerializeField] Image m_LoopImage;
    [SerializeField] Color32 m_Loop_Enabled;
    [SerializeField] Color32 m_Loop_Disabled;
   


    /// <summary>
    /// Key URL, Value VideoClips
    /// </summary>
    private Dictionary<string, VideoClip> m_VideoClipsDictionary;


    private int m_VideoClipIndex = -1;

    [SerializeField] bool m_LoadOnAwake = true;

    // shuffling
    private bool m_Shuffle = false;



    private void Start()
    {
        m_VideoPlayer.prepareCompleted += PrepareComplete;
        m_VideoPlayer.loopPointReached += VideoComplete;

        m_VideoClipsDictionary = new Dictionary<string, VideoClip>();

        // setup dictionary, videos are only pooled at runtime
        for (int i = 0; i < m_VideoURLS.Count; i++)
        {
            m_VideoClipsDictionary.Add(m_VideoURLS[i], null);
        }

        if (m_LoadOnAwake)
        {
            LoadAndPlayVideo(GetNextVideoClipURL());
        }

        if(m_LandscapeMode)
        {
            OnClick_FlipOrientation();
        }

        
    }
    [SerializeField] bool _debug = false;
    private void Update()
    {
        if (m_VideoPlayer.isPlaying)
        {
            SetCurrentTimeUI();
            MovePlayHead();
        }

        if(_debug)
        {
            _debug = false;
        }
    }

        #region Increment/decrement index and retrieving URLS

    private string GetNextVideoClipURL()
    {
        m_VideoClipIndex++;

        if (m_VideoClipIndex >= m_VideoURLS.Count)
        {
            m_VideoClipIndex = m_VideoClipIndex % m_VideoURLS.Count;
        }

        //Debug.Log("next video clip url " + m_VideoURLS[m_VideoClipIndex]);
        //Debug.Log("videoClip index " + m_VideoClipIndex);
        return m_VideoURLS[m_VideoClipIndex];
    }

    private string GetPreviousVideoClipURL()
    {
        m_VideoClipIndex--;

        if (m_VideoClipIndex < 0)
        {
            m_VideoClipIndex = m_VideoURLS.Count - 1;
        }

        return m_VideoURLS[m_VideoClipIndex];
    }

    private string GetShuffledURL()
    {
        while (true)
        {
            int randomIndex = Random.Range(0, m_VideoURLS.Count);

            if (randomIndex != m_VideoClipIndex)
            {
                m_VideoClipIndex = randomIndex;

                return m_VideoURLS[m_VideoClipIndex];
            }
        }
    }

    #endregion

    #region OnClick Methods

    public void OnClick_PlayPause()
    {
        if (m_VideoPlayer.isPlaying)
        {
            m_VideoPlayer.Pause();

            PlayingSpriteActive(true);
        }
        else
        {
            PlayingSpriteActive(false);

            // because it starts on -1
            if (m_VideoClipIndex < 0)
            {
                LoadAndPlayVideo(GetNextVideoClipURL());
              
            }
            else
            {
                m_VideoPlayer.Play();

                m_PlayPause_IMG.sprite = m_PauseSprite;

                SetTotalTimeUI();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="isActive"></param>
    private void PlayingSpriteActive(bool isActive)
    {
        if(isActive)
        {
            m_PlayPause_IMG.sprite = m_PlaySprite;
        }
        else
        {
            m_PlayPause_IMG.sprite = m_PauseSprite;
        }
    }

    public void OnClick_Next()
    {
        m_CurrentTimerSlider.value = 0;
        m_InteractiveSlider.value = 0;

        string url = (m_Shuffle) == true ? GetShuffledURL() : GetNextVideoClipURL();

        LoadAndPlayVideo(url);
    }

    public void OnClick_Previous()
    {
        m_CurrentTimerSlider.value = 0;
        m_InteractiveSlider.value = 0;

        string url = (m_Shuffle) == true ? GetShuffledURL() : GetPreviousVideoClipURL();
        LoadAndPlayVideo(url);
    }

    public void OnClick_ToggleMute()
    {
        bool isMute = m_VideoPlayer.GetDirectAudioMute(0);

        isMute = !isMute;   // flip/toggle mute

        switch (isMute)
        {
            case true:
                m_SoundImage.sprite = m_SoundOff;
                break;
            case false:
                m_SoundImage.sprite = m_SoundOn;
                break;
        }
        

        m_VideoPlayer.SetDirectAudioMute(0, isMute);
    }


    // enable shuffling
    public void OnClick_Shuffle()
    {
        m_Shuffle = !m_Shuffle;

        switch (m_Shuffle)
        {
            case true:
                m_ShuffleImage.color = m_Shuffle_Enabled;
                break;
            case false:
                m_ShuffleImage.color = m_Shuffle_Disabled;
                break;
        }
    }

    public void OnClick_Loop()
    {
        m_Loop = !m_Loop;

        switch (m_Loop)
        {
            case true:
                m_LoopImage.color = m_Loop_Enabled;
                break;
            case false:
                m_LoopImage.color = m_Loop_Disabled;
                break;
        }
    }

    public void OnClick_FlipOrientation()
    {
        switch (Screen.orientation)
        {
            case ScreenOrientation.LandscapeLeft:
                Screen.orientation = ScreenOrientation.Portrait;
                FlipRenderTexture();
                break;
            case ScreenOrientation.Portrait:
                Screen.orientation = ScreenOrientation.LandscapeLeft;
                FlipRenderTexture();
                break;

        }
    }

    private void FlipRenderTexture()
    {
        // ****** FLIPPING
        int width = m_RenderTexture.height;
        int height = m_RenderTexture.width;


        m_RenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        m_RawImage.texture = m_RenderTexture;
        m_VideoPlayer.targetTexture = m_RenderTexture;
    }

    #endregion

    #region Current time / scub bar progress bars / sliders

    private void SetCurrentTimeUI()
    {
        string minutes = Mathf.Floor((int)m_VideoPlayer.time / 60).ToString("00");
        string seconds = ((int)m_VideoPlayer.time % 60).ToString("00");

        m_CurrentTime.text = minutes + " : " + seconds;
    }

    private void SetTotalTimeUI()
    {
        Debug.Log("SetTotalTimeUI" + m_VideoPlayer.length);
        string minutes = Mathf.Floor((int)m_VideoPlayer.length / 60).ToString("00");
        string seconds = ((int)m_VideoPlayer.length % 60).ToString("00");

        m_TotalTime.text = minutes + " : " + seconds;
    }

    private void MovePlayHead()
    {
        m_CurrentTimerSlider.value = (float)m_VideoPlayer.time;
        m_CurrentTimerSlider.maxValue = (float)m_VideoPlayer.length;

        // interactive slider
        m_InteractiveSlider.maxValue = Mathf.Floor((int)m_VideoPlayer.length);
    }

    /// <summary>
    /// Connected to on value changed
    /// </summary>
    public void ScrubBarHeadMove()
    {
        Debug.Log("video current time" + m_VideoPlayer.time + " scrubar time " + m_InteractiveSlider.value + " clip length" + m_VideoPlayer.length);
        
        m_CurrentTimerSlider.value = m_InteractiveSlider.value;

        m_VideoPlayer.time = m_InteractiveSlider.value;

        if (!m_VideoPlayer.isPlaying)
        {
            Debug.Log("is videoplayer not playing");
            m_VideoPlayer.Play();
            Debug.Log("wtf");
            m_PlayPause_IMG.sprite = m_PauseSprite;
        }
    }

    /// <summary>
    /// Listening to slider update event - attached through inspector
    /// </summary>
    public void UpdateVolume()
    {
        m_VideoPlayer.SetDirectAudioVolume(0, m_VolumeSlider.value);
    }

    #endregion

    private void LoadAndPlayVideo(string url)
    {
        Debug.Log("Loadandplayervideo");
        // stop regardless

        // check if the current url has a record/entry
        if (!m_VideoClipsDictionary.ContainsKey(url))
        {
            Debug.LogError("m_VideoClipsDictionary does not contain key " + url);
            return;
        }

        // check if the current url has a videoClip stored in memory
        bool videoInMemory = m_VideoClipsDictionary[url] == null ? false : true;

        if(videoInMemory)
        {
            m_VideoPlayer.source = VideoSource.VideoClip;

            // load it
            m_VideoPlayer.clip = m_VideoClipsDictionary[url];
            m_VideoPlayer.url = url;
           
            m_VideoPlayer.Play();
            Debug.Log("Exists");
        }

        if(!videoInMemory)
        {
            // if it doesn't exist
            // show spinner
            IsLoadingSpinnerActive(true);
            // load video
            m_VideoPlayer.source = VideoSource.Url;
            m_VideoPlayer.Stop();
            m_VideoPlayer.url = url;
            m_VideoPlayer.Prepare();
        }
    }

    private void IsLoadingSpinnerActive(bool isActive)
    {
        m_SpinnerObj.SetActive(isActive);
    }

   
    private void PrepareComplete(VideoPlayer source)
    {
        Debug.Log("Prepare complete");
        m_PlayPause_IMG.sprite = m_PauseSprite;

        m_VideoClipsDictionary[source.url] = source.clip;
       
        m_VideoPlayer.Play();

        float length = (float)m_VideoPlayer.length;
        Debug.Log("video length " + m_VideoPlayer.length);
        m_InteractiveSlider.maxValue = length;
        m_CurrentTimerSlider.maxValue = length;

        SetTotalTimeUI();

        IsLoadingSpinnerActive(false);
    }

    private void VideoComplete(UnityEngine.Video.VideoPlayer vp)
    {
        PlayingSpriteActive(true);

        Debug.Log("Video complete");
        if (m_AutoPlay)
        {
            m_VideoPlayer.Stop();

          
            if (m_Shuffle)
            {
                LoadAndPlayVideo(GetShuffledURL());
            }

            if (!m_Shuffle)
            {
                if (m_VideoClipIndex + 1 >= m_VideoClipsDictionary.Count && !m_Loop)
                {
                    // if we reached the end of the line - i
                    Debug.Log("Reached the end");
                    return;
                }

                LoadAndPlayVideo(GetNextVideoClipURL());
            }

            m_CurrentTimerSlider.value = 0;

            m_InteractiveSlider.value = 0;
        }
    }
}
