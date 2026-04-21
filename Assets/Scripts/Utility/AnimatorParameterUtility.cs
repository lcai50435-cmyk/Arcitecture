using UnityEngine;

public static class AnimatorParameterUtility
{
    public static bool CanDrive(Animator animator)
    {
        return animator != null
            && animator.isActiveAndEnabled
            && animator.isInitialized
            && animator.gameObject.activeInHierarchy
            && animator.runtimeAnimatorController != null;
    }

    public static void SetBoolIfPresent(Animator animator, string parameterName, bool value)
    {
        if (!HasParameter(animator, parameterName, AnimatorControllerParameterType.Bool))
        {
            return;
        }

        animator.SetBool(parameterName, value);
    }

    public static void SetFloatIfPresent(Animator animator, string parameterName, float value)
    {
        if (!HasParameter(animator, parameterName, AnimatorControllerParameterType.Float))
        {
            return;
        }

        animator.SetFloat(parameterName, value);
    }

    private static bool HasParameter(Animator animator, string parameterName, AnimatorControllerParameterType expectedType)
    {
        if (!CanDrive(animator))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type == expectedType && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }
}
