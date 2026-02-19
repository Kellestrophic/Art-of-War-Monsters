using System.Collections;
using UnityEngine;

public class FallenController : MonoBehaviour
{
    public enum FallenPhase
    {
        Phase1_Reaver,
        Phase2_Binder,
        Phase3_Warper,
        Final_Trinity
    }

    [Header("Phase")]
    public FallenPhase CurrentPhase { get; private set; }

    [Header("Skeleton Mages")]
    [SerializeField] private BaseSkeletonMage mageReaver;
    [SerializeField] private BaseSkeletonMage mageBinder;
    [SerializeField] private BaseSkeletonMage mageWarper;

    [Header("Final Phase Rotation")]
    [Tooltip("How long each mage is allowed to perform major attacks")]
    [SerializeField] private float rotationWindow = 2f;

    [Tooltip("Speed multiplier applied each full rotation")]
    [SerializeField] private float rotationAcceleration = 0.9f;

    private Coroutine finalPhaseRoutine;

    // =====================================================
    // UNITY
    // =====================================================

    private void Start()
    {
        EnterPhase(FallenPhase.Phase1_Reaver);
    }

    // =====================================================
    // PHASE CONTROL
    // =====================================================

   public void EnterPhase(FallenPhase phase)
{
    if (finalPhaseRoutine != null)
    {
        StopCoroutine(finalPhaseRoutine);
        finalPhaseRoutine = null;
    }

    CurrentPhase = phase;

    DisableAllMages();

    switch (phase)
    {
        case FallenPhase.Phase1_Reaver:
            mageReaver.SetEnabled(true);
            mageReaver.AllowMajorAttack(true);   // 🔥 ENABLE ATTACKS
            break;

      case FallenPhase.Phase2_Binder:
    mageBinder.SetEnabled(true);
    mageBinder.AllowMajorAttack(true);   // 🔥 REQUIRED
    break;


        case FallenPhase.Phase3_Warper:
            mageWarper.SetEnabled(true);
            mageWarper.AllowMajorAttack(true);   // 🔥 ENABLE ATTACKS
            break;

        case FallenPhase.Final_Trinity:
            mageReaver.SetEnabled(true);
            mageBinder.SetEnabled(true);
            mageWarper.SetEnabled(true);

            mageReaver.AllowMajorAttack(false);
            mageBinder.AllowMajorAttack(false);
            mageWarper.AllowMajorAttack(false);

            mageReaver.OnFinalPhaseStart();
            mageBinder.OnFinalPhaseStart();
            mageWarper.OnFinalPhaseStart();

            finalPhaseRoutine = StartCoroutine(FinalPhaseRotation());
            break;
    }
}


    private void DisableAllMages()
    {
        mageReaver.SetEnabled(false);
        mageBinder.SetEnabled(false);
        mageWarper.SetEnabled(false);
    }

    // =====================================================
    // FINAL PHASE ROTATION
    // =====================================================

    private IEnumerator FinalPhaseRotation()
    {
        float window = rotationWindow;

        while (CurrentPhase == FallenPhase.Final_Trinity)
        {
            // Bone Reaver
            yield return RunRotationWindow(mageReaver, window);

            // Grave Binder
            yield return RunRotationWindow(mageBinder, window);

            // Void Warper
            yield return RunRotationWindow(mageWarper, window);

            // Each full loop speeds up the fight
            window *= rotationAcceleration;
            window = Mathf.Max(0.8f, window); // safety clamp
        }
    }

    private IEnumerator RunRotationWindow(BaseSkeletonMage mage, float duration)
    {
        if (mage == null || !mage.IsAlive)
            yield break;

        mage.AllowMajorAttack(true);
        yield return new WaitForSeconds(duration);
        mage.AllowMajorAttack(false);
    }

    // =====================================================
    // CALLED BY MAGES WHEN THEY DIE
    // =====================================================

    public void NotifyMageDeath(BaseSkeletonMage mage)
    {
        // In final phase: if only one mage left, remove rotation
        int alive =
            (mageReaver.IsAlive ? 1 : 0) +
            (mageBinder.IsAlive ? 1 : 0) +
            (mageWarper.IsAlive ? 1 : 0);

        if (alive <= 1)
        {
            if (finalPhaseRoutine != null)
            {
                StopCoroutine(finalPhaseRoutine);
                finalPhaseRoutine = null;
            }

            // Let remaining mage go wild
            mageReaver.AllowMajorAttack(true);
            mageBinder.AllowMajorAttack(true);
            mageWarper.AllowMajorAttack(true);
        }
    }
    public void NotifyMageLowHealth(BaseSkeletonMage mage)
{
    if (CurrentPhase == FallenPhase.Phase1_Reaver && mage == mageReaver)
        EnterPhase(FallenPhase.Phase2_Binder);

    else if (CurrentPhase == FallenPhase.Phase2_Binder && mage == mageBinder)
        EnterPhase(FallenPhase.Phase3_Warper);

    else if (CurrentPhase == FallenPhase.Phase3_Warper && mage == mageWarper)
        EnterPhase(FallenPhase.Final_Trinity);
}

}
