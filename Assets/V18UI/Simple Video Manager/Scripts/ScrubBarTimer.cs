using UnityEngine;
using UnityEngine.UI;

public class ScrubBarTimer : MonoBehaviour
{
    [SerializeField] Text m_ScrubTime;
    [SerializeField] Slider m_ScrubBarSlider;

    private float m_CooldownTime = .5f;
    private float m_CurrentTimer = 0;

    private void Start()
    {
        m_ScrubTime.text = "";
    }

    public void OnScrubBarSliderMove()
    {
        float timeInSeconds = m_ScrubBarSlider.value;

        string minutes = Mathf.Floor(timeInSeconds / 60).ToString("00");
        string seconds = (timeInSeconds % 60).ToString("00");


        m_ScrubTime.text = minutes + " : " + seconds;

        m_CurrentTimer = m_CooldownTime;
    }

    private void Update()
    {
        if(m_CurrentTimer > 0)
        {
            m_CurrentTimer -= Time.deltaTime;

            if(m_CurrentTimer <= 0)
            {
                m_ScrubTime.text = "";
            }
        }
    }
}
