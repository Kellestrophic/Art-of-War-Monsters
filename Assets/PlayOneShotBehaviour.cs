using UnityEngine;

public class PlayOneShotBehaviour : StateMachineBehaviour
{
    public AudioClip soundToPlay;
    [Range(0f,1f)] public float volume = 1f;
    public bool playOnEnter = true, playOnExit = false, playAfterDelay = false;
    public float playDelay = 0.25f;

    private float timeSinceEntered = 0f;
    private bool hasDelayedSoundPlayed = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (playOnEnter) SFXBus.Instance?.PlayAt(soundToPlay, animator.transform.position, volume);
        timeSinceEntered = 0f;
        hasDelayedSoundPlayed = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (playAfterDelay && !hasDelayedSoundPlayed)
        {
            timeSinceEntered += Time.deltaTime;
            if (timeSinceEntered > playDelay)
            {
                SFXBus.Instance?.PlayAt(soundToPlay, animator.transform.position, volume);
                hasDelayedSoundPlayed = true;
            }
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (playOnExit) SFXBus.Instance?.PlayAt(soundToPlay, animator.transform.position, volume);
    }
}
