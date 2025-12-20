using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentLogic : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Step Settings")]
    public float stepDelay = 0.5f;

    private List<AnimationClip> clips = new List<AnimationClip>();
    private int currentIndex = -1;
    private Coroutine stepCoroutine;
    private bool isPlaying;

    // ================= INITIALIZATION =================
    void Awake()
    {
        CacheClips();
    }

    private void CacheClips()
    {
        clips.Clear();

        var controller = animator.runtimeAnimatorController;
        if (controller == null) return;

        foreach (var clip in controller.animationClips)
        {
            if (clip.name != "Idle") // keep Idle excluded
                clips.Add(clip);
        }
    }

    // ================= STEP =================
    public void Step()
    {
        StopAllCoroutines();
        stepCoroutine = StartCoroutine(PlayAllSteps());
    }

    private IEnumerator PlayAllSteps()
    {
        isPlaying = true;

        for (int i = currentIndex + 1; i < clips.Count; i++)
        {
            currentIndex = i;
            PlayClip(clips[i]);

            yield return new WaitForSeconds(clips[i].length);
            yield return new WaitForSeconds(stepDelay);
        }

        isPlaying = false;
    }

    // ================= NEXT =================
    public void Next()
    {
        StopAllCoroutines();

        if (isPlaying)
            CompleteCurrent();

        if (currentIndex < clips.Count - 1)
        {
            currentIndex++;
            PlayClip(clips[currentIndex]);
        }
    }

    // ================= PREVIOUS =================
    public void Previous()
    {
        StopAllCoroutines();

        if (isPlaying)
            CompleteCurrent();

        // If we are at first animation -> go back to Idle
        if (currentIndex == 0)
        {
            currentIndex = -1;
            animator.Play("Idle", 0, 0f);
            return;
        }

        // Normal previous behavior
        if (currentIndex > 0)
        {
            currentIndex--;
            PlayClip(clips[currentIndex]);
        }
    }


    // ================= CORE =================
    private void PlayClip(AnimationClip clip)
    {
        animator.speed = 1f;
        animator.Play(clip.name, 0, 0f);
        isPlaying = true;
    }

    private void CompleteCurrent()
    {
        if (currentIndex < 0 || currentIndex >= clips.Count) return;

        animator.Play(clips[currentIndex].name, 0, 1f);
        animator.Update(0f);
        isPlaying = false;
    }

    // ================= RESET =================
    public void ResetExperiment()
    {
        StopAllCoroutines();
        currentIndex = -1;
        isPlaying = false;

        animator.Play("Idle", 0, 0f);
    }
}
