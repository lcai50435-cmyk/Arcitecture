public static class PlayerLoadoutRuntime
{
    public static InkType CurrentInkType { get; set; } = InkType.DirectInk;

    public static WeaponType CurrentWeaponType
    {
        get => CurrentInkType.ToWeaponType();
        set => CurrentInkType = value.ToInkType();
    }

    public static bool AllowBaseAttack { get; set; } = false;
}
