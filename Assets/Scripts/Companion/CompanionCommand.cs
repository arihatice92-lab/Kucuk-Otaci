using UnityEngine;

public enum CompanionCommandType { Follow, Stay, GoTo, Interact }

public readonly struct CompanionCommand
{
    public readonly CompanionCommandType Type;
    public readonly Vector3 TargetPosition;
    public readonly IInteractable Target;

    private CompanionCommand(CompanionCommandType type, Vector3 pos, IInteractable target)
    {
        Type = type; TargetPosition = pos; Target = target;
    }

    public static CompanionCommand Follow() => new(CompanionCommandType.Follow, default, null);
    public static CompanionCommand Stay() => new(CompanionCommandType.Stay, default, null);
    public static CompanionCommand GoTo(Vector3 pos) => new(CompanionCommandType.GoTo, pos, null);
    public static CompanionCommand Interact(Vector3 pos, IInteractable t) => new(CompanionCommandType.Interact, pos, t);
}